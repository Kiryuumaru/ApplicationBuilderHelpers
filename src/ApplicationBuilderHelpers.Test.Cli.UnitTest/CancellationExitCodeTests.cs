using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process exit-code coverage for the cancellation contract:
/// normal return is success (exit 0), <see cref="CommandException"/> carries
/// the exit code, and cancellation requested via the passed token maps to
/// 128 + SIGINT (130) while internal shutdown stays success (0).
/// Runs against the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point, following the <see cref="ConsoleDecouplingTests"/> pattern.
/// </summary>
/// <remarks>
/// Ctrl+C gap: the CancelKeyPress handler lives inside the internal executor and
/// <c>ApplicationBuilder.RunAsync</c> always constructs a real console output, so there
/// is no seam to simulate Ctrl+C in-process. Real-signal coverage would need an
/// out-of-process test or an internal-visible seam (not added here; tests only).
/// </remarks>
[Collection("ConsoleDecoupling")]
public sealed class CancellationExitCodeTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private const int CanceledExitCode = 130;

    [Command("Cancellation exit-code probe.")]
    private sealed class SuccessCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
            => ValueTask.CompletedTask;
    }

    [Command("Cancellation exit-code probe.")]
    private sealed class FailureCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
            => throw new CommandException("Probe failure.", 7);
    }

    [Command("Cancellation exit-code probe.")]
    private sealed class CancelObservingCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
            => await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Command("Cancellation exit-code probe.")]
    private sealed class CallbackCountingCommand : Command
    {
        public static int ExitingCount;
        public static int ExitedCount;
        public static TaskCompletionSource<bool> CallbacksRegistered = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly int _delayMs;

        public CallbackCountingCommand()
            : this(0)
        {
        }

        public CallbackCountingCommand(int delayMs) => _delayMs = delayMs;

        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var lifetime = applicationHost.Services.GetRequiredService<LifetimeService>();
            lifetime.ApplicationExitingCallback(() => Interlocked.Increment(ref ExitingCount));
            lifetime.ApplicationExitedCallback(() => Interlocked.Increment(ref ExitedCount));
            CallbacksRegistered.TrySetResult(true);
            if (_delayMs > 0)
            {
                await Task.Delay(_delayMs, cancellationToken);
            }
        }
    }

    [Command("Cancellation exit-code probe.")]
    private sealed class LegacyShimCommand : Command
    {
        public static bool ShimInvoked;

        [Obsolete("Intentional legacy-path probe.")]
#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member (intentional: legacy path probe).
#pragma warning disable CS0618 // Legacy overload is intentionally exercised here.
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            ShimInvoked = true;
            return ValueTask.CompletedTask;
        }
#pragma warning restore CS0618
#pragma warning restore CS0809
    }

    [Command("Cancellation exit-code probe.")]
    private sealed class BothOverloadsCommand : Command
    {
        public static bool TokenOverloadInvoked;
        public static bool ShimOverloadInvoked;

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            TokenOverloadInvoked = true;
            return ValueTask.CompletedTask;
        }

#pragma warning disable CS0809 // Obsolete member overrides non-obsolete member (intentional: precedence probe).
#pragma warning disable CS0618 // Legacy overload is intentionally exercised here.
        [Obsolete("Intentional legacy-path probe.")]
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            ShimOverloadInvoked = true;
            return ValueTask.CompletedTask;
        }
#pragma warning restore CS0618
#pragma warning restore CS0809
    }

    [Command("Cancellation exit-code probe.")]
    private sealed class InternalCanceledCommand : Command
    {
        // Cooperative cancellation observed from an internal source only:
        // no outer token and no Ctrl+C involved, so this must stay success (exit 0).
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
            => throw new OperationCanceledException("Internal cooperative stop.");
    }
    [Command("Cancellation exit-code probe.")]
    private sealed class HostRaceCommand : Command
    {
        public static bool CommandDrained;

        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            await applicationHost.Host.StopAsync();
            CommandDrained = true;
        }
    }

    [Fact]
    public async Task NormalReturn_SignalsSuccess()
    {
        var exitCode = await RunCapturedAsync(() => CreateBuilder<SuccessCommand>().RunAsync([]));

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task CommandException_CarriesExitCode()
    {
        var exitCode = await RunCapturedAsync(() => CreateBuilder<FailureCommand>().RunAsync([]));

        Assert.Equal(7, exitCode);
    }

    [Fact]
    public async Task PrecanceledToken_MapsToCanceledExitCode()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exitCode = await RunCapturedAsync(() => CreateBuilder<CancelObservingCommand>().RunAsync([], cts.Token));

        Assert.Equal(CanceledExitCode, exitCode);
    }

    [Fact]
    public async Task CancelDuringRun_MapsToCanceledExitCode()
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(250));

        var exitCode = await RunCapturedAsync(() => CreateBuilder<CancelObservingCommand>().RunAsync([], cts.Token));

        Assert.Equal(CanceledExitCode, exitCode);
    }

    [Fact]
    public async Task InternalCancelWithoutOuterSignal_SignalsSuccess()
    {
        // Cooperative cancellation observed from an internal source only
        // (no outer token, no Ctrl+C): the success path (exit 0) applies.
        var exitCode = await RunCapturedAsync(() => CreateBuilder<InternalCanceledCommand>().RunAsync([]));

        Assert.Equal(0, exitCode);
    }

    [Fact]
    public async Task LifetimeCallbacks_RunOnSuccessPath()
    {
        CallbackCountingCommand.ExitingCount = 0;
        CallbackCountingCommand.ExitedCount = 0;

        var exitCode = await RunCapturedAsync(() => CreateBuilder<CallbackCountingCommand>().RunAsync([]));

        Assert.Equal(0, exitCode);
        Assert.Equal(1, CallbackCountingCommand.ExitingCount);
        Assert.Equal(1, CallbackCountingCommand.ExitedCount);
    }

    [Fact]
    public async Task LifetimeCallbacks_RunOnCanceledPath()
    {
        CallbackCountingCommand.ExitingCount = 0;
        CallbackCountingCommand.ExitedCount = 0;
        CallbackCountingCommand.CallbacksRegistered = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();
        Task<int> runTask = RunCapturedAsync(() => CreateBuilder(() => new CallbackCountingCommand(60_000)).RunAsync([], cts.Token));
        try
        {
            // Gate the 250ms cancel budget on observed callback registration so
            // slow executor startup cannot consume it before the command runs.
            Task registration = await Task.WhenAny(
                CallbackCountingCommand.CallbacksRegistered.Task,
                Task.Delay(TimeSpan.FromSeconds(10)));
            Assert.Same(CallbackCountingCommand.CallbacksRegistered.Task, registration);
            await CallbackCountingCommand.CallbacksRegistered.Task;
            cts.CancelAfter(TimeSpan.FromMilliseconds(250));

            var exitCode = await runTask;

            Assert.Equal(CanceledExitCode, exitCode);
            Assert.Equal(1, CallbackCountingCommand.ExitingCount);
            Assert.Equal(1, CallbackCountingCommand.ExitedCount);
        }
        finally
        {
            cts.Cancel();
            await runTask;
        }
    }

    [Fact]
    public async Task LegacyShimOnly_SignalsSuccess()
    {
        LegacyShimCommand.ShimInvoked = false;

        var exitCode = await RunCapturedAsync(() => CreateBuilder<LegacyShimCommand>().RunAsync([]));

        Assert.Equal(0, exitCode);
        Assert.True(LegacyShimCommand.ShimInvoked);
    }

    [Fact]
    public async Task TokenOverload_WinsOverLegacyShim()
    {
        BothOverloadsCommand.TokenOverloadInvoked = false;
        BothOverloadsCommand.ShimOverloadInvoked = false;

        var exitCode = await RunCapturedAsync(() => CreateBuilder<BothOverloadsCommand>().RunAsync([]));

        Assert.Equal(0, exitCode);
        Assert.True(BothOverloadsCommand.TokenOverloadInvoked);
        Assert.False(BothOverloadsCommand.ShimOverloadInvoked);
    }

    [Fact]
    public async Task HostStoppingFirst_DrainsCommandAsSuccess()
    {
        HostRaceCommand.CommandDrained = false;

        var exitCode = await RunCapturedAsync(() => CreateBuilder<HostRaceCommand>().RunAsync([]));

        Assert.Equal(0, exitCode);
        Assert.True(HostRaceCommand.CommandDrained);
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand, new()
        => ApplicationBuilder.Create()
            .SetExecutableName("cancel-probe")
            .SetExecutableTitle("Cancel Probe")
            .SetExecutableDescription("Cancellation exit-code verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<TCommand, ApplicationBuilder>();

    private static ApplicationBuilder CreateBuilder(Func<ICommand> factory)
    {
        var builder = ApplicationBuilder.Create()
            .SetExecutableName("cancel-probe")
            .SetExecutableTitle("Cancel Probe")
            .SetExecutableDescription("Cancellation exit-code verification CLI.")
            .SetExecutableVersion("9.9.9");
        builder.AddCommand<ApplicationBuilder>(factory());
        return builder;
    }

    private static async Task<int> RunCapturedAsync(Func<Task<int>> run)
    {
        await ConsoleGate.WaitAsync();
        try
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var outWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            try
            {
                return await run();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
        finally
        {
            ConsoleGate.Release();
        }
    }
}
