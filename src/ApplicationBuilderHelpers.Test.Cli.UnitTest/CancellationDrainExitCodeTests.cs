using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process exit-code coverage for external cancellation that arrives while the
/// host-success route is draining the command task: a canceled outer token (the abort
/// trigger reachable through the public entry point) maps to 128 + SIGINT (130)
/// whether the command settles during the drain or keeps running past it, while a
/// host stopped without an abort keeps the drain expiry at success (0).
/// Runs against the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CancellationDrainExitCodeTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private const int CanceledExitCode = 130;

    [Command("Cancellation drain exit-code probe.")]
    private sealed class NeverSettlingHostDrainCommand : Command
    {
        public static TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public static CancellationToken ReleaseToken;

        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            await WaitForApplicationStartedAsync(applicationHost);
            Started.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, ReleaseToken);
        }
    }

    [Command("Cancellation drain exit-code probe.")]
    private sealed class SettlingDuringDrainCommand : Command
    {
        public static TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var lifetime = applicationHost.Services.GetRequiredService<IHostApplicationLifetime>();
            var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var stoppedRegistration = lifetime.ApplicationStopped.Register(() => stopped.TrySetResult(true));
            await WaitForApplicationStartedAsync(applicationHost);
            Started.TrySetResult(true);
            await stopped.Task;
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);
        }
    }

    [Command("Cancellation drain exit-code probe.")]
    private sealed class SwallowingCancellationCommand : Command
    {
        public static TaskCompletionSource<bool> Started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var lifetime = applicationHost.Services.GetRequiredService<IHostApplicationLifetime>();
            var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var stoppedRegistration = lifetime.ApplicationStopped.Register(() => stopped.TrySetResult(true));
            await WaitForApplicationStartedAsync(applicationHost);
            Started.TrySetResult(true);
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Swallows the external cancellation and still reports success.
            }

            await stopped.Task;
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);
        }
    }

    [Command("Cancellation drain exit-code probe.")]
    private sealed class HostStopsDuringRunCommand : Command
    {
        public static TaskCompletionSource<bool> HostStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public static CancellationToken ReleaseToken;

        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            await WaitForApplicationStartedAsync(applicationHost);
            await applicationHost.Host.StopAsync();
            HostStopped.TrySetResult(true);
            await Task.Delay(Timeout.InfiniteTimeSpan, ReleaseToken);
        }
    }

    [Fact]
    public async Task ExternalAbort_WithNeverSettlingCommand_MapsToCanceledExitCode()
    {
        NeverSettlingHostDrainCommand.Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new CancellationTokenSource();
        NeverSettlingHostDrainCommand.ReleaseToken = release.Token;
        using var cts = new CancellationTokenSource();
        Task<int> runTask = RunCapturedAsync(() => CreateBuilder<NeverSettlingHostDrainCommand>().RunAsync([], cts.Token));
        try
        {
            await WaitForSignalAsync(NeverSettlingHostDrainCommand.Started.Task, nameof(NeverSettlingHostDrainCommand.Started));
            cts.Cancel();

            var exitCode = await runTask;

            Assert.Equal(CanceledExitCode, exitCode);
        }
        finally
        {
            release.Cancel();
            cts.Cancel();
            await runTask;
        }
    }

    [Fact]
    public async Task ExternalAbort_WithCommandSettlingDuringDrain_MapsToCanceledExitCode()
    {
        SettlingDuringDrainCommand.Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();
        Task<int> runTask = RunCapturedAsync(() => CreateBuilder<SettlingDuringDrainCommand>().RunAsync([], cts.Token));
        try
        {
            await WaitForSignalAsync(SettlingDuringDrainCommand.Started.Task, nameof(SettlingDuringDrainCommand.Started));
            cts.Cancel();

            var exitCode = await runTask;

            Assert.Equal(CanceledExitCode, exitCode);
        }
        finally
        {
            cts.Cancel();
            await runTask;
        }
    }

    [Fact]
    public async Task ExternalAbort_WithCommandSwallowingCancellation_MapsToCanceledExitCode()
    {
        SwallowingCancellationCommand.Started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var cts = new CancellationTokenSource();
        Task<int> runTask = RunCapturedAsync(() => CreateBuilder<SwallowingCancellationCommand>().RunAsync([], cts.Token));
        try
        {
            await WaitForSignalAsync(SwallowingCancellationCommand.Started.Task, nameof(SwallowingCancellationCommand.Started));
            cts.Cancel();

            var exitCode = await runTask;

            Assert.Equal(CanceledExitCode, exitCode);
        }
        finally
        {
            cts.Cancel();
            await runTask;
        }
    }

    [Fact]
    public async Task HostStoppedWithoutExternalAbort_WhenCommandNeverSettles_StaysSuccess()
    {
        HostStopsDuringRunCommand.HostStopped = new(TaskCreationOptions.RunContinuationsAsynchronously);
        using var release = new CancellationTokenSource();
        HostStopsDuringRunCommand.ReleaseToken = release.Token;
        Task<int> runTask = RunCapturedAsync(() => CreateBuilder<HostStopsDuringRunCommand>().RunAsync([]));
        try
        {
            await WaitForSignalAsync(HostStopsDuringRunCommand.HostStopped.Task, nameof(HostStopsDuringRunCommand.HostStopped));

            var exitCode = await runTask;

            Assert.Equal(0, exitCode);
        }
        finally
        {
            release.Cancel();
            await runTask;
        }
    }

    private static async Task WaitForApplicationStartedAsync(ApplicationHost<HostApplicationBuilder> applicationHost)
    {
        var lifetime = applicationHost.Services.GetRequiredService<IHostApplicationLifetime>();
        var started = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var registration = lifetime.ApplicationStarted.Register(() => started.TrySetResult(true));
        await started.Task;
    }

    private static async Task WaitForSignalAsync(Task signal, string signalName)
    {
        Task bounded = await Task.WhenAny(signal, Task.Delay(TimeSpan.FromSeconds(10)));
        Assert.True(ReferenceEquals(signal, bounded), $"Timed out waiting for {signalName}.");
        await signal;
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand, new()
        => ApplicationBuilder.Create()
            .SetExecutableName("cancel-probe")
            .SetExecutableTitle("Cancel Probe")
            .SetExecutableDescription("Cancellation exit-code verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<TCommand, ApplicationBuilder>();

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
