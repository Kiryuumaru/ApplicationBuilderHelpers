using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Reflection;
using System.Runtime.InteropServices;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Cancellation classification coverage through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// <see cref="CommandExitMapper"/> cancel-wins mapping plus
/// <see cref="CommandExitMapper.ThrowIfExternalAbort"/> throw/no-throw, and
/// <see cref="CommandShutdownScope"/> Ctrl+C and SIGTERM observation through a capturing
/// <see cref="IConsoleCancelSignal"/> fake and a SIGTERM fake, with the executor 130 mapping
/// and the orchestrator classification for the host-wins host-canceled and host-success
/// drain outcomes plus the command-first success outcome.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CommandExternalAbortTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private const int CanceledExitCode = 130;

    private sealed class CapturingCancelSignal : IConsoleCancelSignal
    {
        public ConsoleCancelEventHandler? Captured;
        public int SubscribeCount;
        public int UnsubscribeCount;

        public bool Subscribe(ConsoleCancelEventHandler handler)
        {
            SubscribeCount++;
            Captured = handler;
            return true;
        }

        public void Unsubscribe(ConsoleCancelEventHandler handler)
        {
            UnsubscribeCount++;
            if (ReferenceEquals(Captured, handler))
            {
                Captured = null;
            }
        }

        public void RaiseCtrlC()
        {
            var handler = Captured;
            Assert.NotNull(handler);
            var args = CreateArgs();
            handler(null, args);
        }

        private static ConsoleCancelEventArgs CreateArgs()
        {
            var constructor = typeof(ConsoleCancelEventArgs).GetConstructor(
                BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance,
                [typeof(ConsoleSpecialKey)]);
            Assert.NotNull(constructor);
            return (ConsoleCancelEventArgs)constructor.Invoke([ConsoleSpecialKey.ControlC]);
        }
    }

    private sealed class FakeSigtermSignal : ISigtermSignal
    {
        public event Action? Signaled;

        public void Raise() => Signaled?.Invoke();
    }

    [Command("External abort probe.")]
    private sealed class CancelObservingCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
            => await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
    }

    [Command("External abort probe.")]
    private sealed class HostWinsProbeCommand : Command
    {
        // Ignores the run token so the host task wins the orchestrator race deterministically.
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
            => await Task.Delay(TimeSpan.FromMilliseconds(200));
    }

    [Command("External abort probe.")]
    private sealed class HostSuccessNeverSettlingProbeCommand : Command
    {
        // Ignores the run token so the host-success drain expires with the command still pending.
        public static CancellationToken ReleaseToken;

        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
            => await Task.Delay(Timeout.InfiniteTimeSpan, ReleaseToken);
    }

    [Command("External abort probe.")]
    private sealed class HostSuccessSettlingProbeCommand : Command
    {
        // Ignores the run token and settles successfully on its own once the host has stopped.
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var lifetime = applicationHost.Services.GetRequiredService<IHostApplicationLifetime>();
            var stopped = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using var registration = lifetime.ApplicationStopped.Register(() => stopped.TrySetResult(true));
            await stopped.Task;
            await Task.Delay(TimeSpan.FromMilliseconds(500), CancellationToken.None);
        }
    }

    [Command("External abort probe.")]
    private sealed class ExitingCleanupProbeCommand : Command
    {
        public readonly TaskCompletionSource<bool> ExitingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> ReleaseExiting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ExitingCompleted;
        public int ExitedCount;

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var lifetime = applicationHost.Services.GetRequiredService<LifetimeService>();
            lifetime.ApplicationExitingCallback(async () =>
            {
                ExitingStarted.TrySetResult(true);
                await ReleaseExiting.Task.ConfigureAwait(false);
                ExitingCompleted = true;
            });
            lifetime.ApplicationExitedCallback(() => Interlocked.Increment(ref ExitedCount));
            return ValueTask.CompletedTask;
        }
    }

    [Command("External abort probe.")]
    private sealed class ExitedCleanupProbeCommand : Command
    {
        public readonly TaskCompletionSource<bool> ExitedStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> AbortObserved = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public readonly TaskCompletionSource<bool> ReleaseExited = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public bool ExitedCompleted;

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            var lifetime = applicationHost.Services.GetRequiredService<LifetimeService>();
            var runToken = lifetime.CreateCancellationToken();
            lifetime.ApplicationExitedCallback(async () =>
            {
                using var registration = runToken.Register(() => AbortObserved.TrySetResult(true));
                ExitedStarted.TrySetResult(true);
                await ReleaseExited.Task.ConfigureAwait(false);
                ExitedCompleted = true;
            });
            return ValueTask.CompletedTask;
        }
    }

    [Command("External abort probe.")]
    private sealed class CommandSuccessProbeCommand : Command
    {
        private readonly Action _onRun;

        public CommandSuccessProbeCommand(Action onRun) => _onRun = onRun;

        // Raises the abort signal synchronously so the command task settles before the host task exists.
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            _onRun();
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData(false, false, false, false)]
    [InlineData(false, false, true, false)]
    [InlineData(false, true, false, false)]
    [InlineData(false, true, true, false)]
    [InlineData(true, false, false, false)]
    [InlineData(true, false, true, true)]
    [InlineData(true, true, false, true)]
    [InlineData(true, true, true, true)]
    public void IsExternalAbort_MatchesCancelWins(bool shutdownCanceled, bool outerCanceled, bool ctrlCCanceled, bool expected)
    {
        using var shutdownCts = new CancellationTokenSource();
        using var outerCts = new CancellationTokenSource();
        if (shutdownCanceled)
        {
            shutdownCts.Cancel();
        }

        if (outerCanceled)
        {
            outerCts.Cancel();
        }

        Assert.Equal(expected, CommandExitMapper.IsExternalAbort(shutdownCts.Token, outerCts.Token, ctrlCCanceled));
    }

    [Theory]
    [InlineData(true, false, false, true, true)]
    [InlineData(false, false, false, true, false)]
    [InlineData(true, false, false, false, false)]
    [InlineData(true, true, false, false, true)]
    [InlineData(true, false, true, false, true)]
    public void IsExternalAbort_WithSigterm_MatchesShutdownAndFlag(bool shutdownCanceled, bool outerCanceled, bool ctrlCCanceled, bool sigtermCanceled, bool expected)
    {
        using var shutdownCts = new CancellationTokenSource();
        using var outerCts = new CancellationTokenSource();
        if (shutdownCanceled)
        {
            shutdownCts.Cancel();
        }

        if (outerCanceled)
        {
            outerCts.Cancel();
        }

        Assert.Equal(expected, CommandExitMapper.IsExternalAbort(shutdownCts.Token, outerCts.Token, ctrlCCanceled, sigtermCanceled));
    }

    [Fact]
    public void ThrowIfExternalAbort_WithSigterm_WhenShutdownAndFlag_Throws()
    {
        using var shutdownCts = new CancellationTokenSource();
        using var outerCts = new CancellationTokenSource();
        shutdownCts.Cancel();

        Assert.Throws<CommandExecutor.ExternalCancellationException>(
            () => CommandExitMapper.ThrowIfExternalAbort(shutdownCts.Token, outerCts.Token, false, true));
    }

    [Fact]
    public void ThrowIfExternalAbort_WithSigterm_WhenNoShutdown_DoesNotThrow()
    {
        using var shutdownCts = new CancellationTokenSource();
        using var outerCts = new CancellationTokenSource();

        var exception = Record.Exception(
            () => CommandExitMapper.ThrowIfExternalAbort(shutdownCts.Token, outerCts.Token, false, true));
        Assert.Null(exception);
    }

    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(true, true, true)]
    public void ThrowIfExternalAbort_WhenExternal_Throws(bool shutdownCanceled, bool outerCanceled, bool ctrlCCanceled)
    {
        using var shutdownCts = new CancellationTokenSource();
        using var outerCts = new CancellationTokenSource();
        if (shutdownCanceled)
        {
            shutdownCts.Cancel();
        }

        if (outerCanceled)
        {
            outerCts.Cancel();
        }

        Assert.Throws<CommandExecutor.ExternalCancellationException>(
            () => CommandExitMapper.ThrowIfExternalAbort(shutdownCts.Token, outerCts.Token, ctrlCCanceled));
    }

    [Theory]
    [InlineData(false, false, false)]
    [InlineData(false, false, true)]
    [InlineData(false, true, false)]
    [InlineData(false, true, true)]
    [InlineData(true, false, false)]
    public void ThrowIfExternalAbort_WhenNotExternal_DoesNotThrow(bool shutdownCanceled, bool outerCanceled, bool ctrlCCanceled)
    {
        using var shutdownCts = new CancellationTokenSource();
        using var outerCts = new CancellationTokenSource();
        if (shutdownCanceled)
        {
            shutdownCts.Cancel();
        }

        if (outerCanceled)
        {
            outerCts.Cancel();
        }

        var exception = Record.Exception(
            () => CommandExitMapper.ThrowIfExternalAbort(shutdownCts.Token, outerCts.Token, ctrlCCanceled));
        Assert.Null(exception);
    }

    [Fact]
    public void ShutdownScope_CtrlCSignal_MarksExternalAbort()
    {
        var signal = new CapturingCancelSignal();
        using var scope = new CommandShutdownScope(CancellationToken.None, signal);

        Assert.Equal(1, signal.SubscribeCount);
        Assert.False(scope.IsExternalAbortRequested);

        signal.RaiseCtrlC();

        Assert.True(scope.CtrlCCanceled);
        Assert.True(scope.Token.IsCancellationRequested);
        Assert.True(scope.IsExternalAbortRequested);
    }

    [Fact]
    public void ShutdownScope_SigtermSignal_MarksExternalAbort()
    {
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        using var scope = new CommandShutdownScope(CancellationToken.None, signal, sigterm);

        Assert.False(scope.IsExternalAbortRequested);

        sigterm.Raise();

        Assert.True(scope.SigtermCanceled);
        Assert.True(scope.Token.IsCancellationRequested);
        Assert.True(scope.IsExternalAbortRequested);
        Assert.Throws<CommandExecutor.ExternalCancellationException>(() => scope.ThrowIfExternalAbort());
    }

    [Fact]
    public void ShutdownScope_NoSigterm_StaysSilent()
    {
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        using var scope = new CommandShutdownScope(CancellationToken.None, signal, sigterm);

        Assert.False(scope.SigtermCanceled);
        Assert.False(scope.Token.IsCancellationRequested);
        Assert.False(scope.IsExternalAbortRequested);
    }

    [Fact]
    public async Task Orchestrator_HostWinsWithSigterm_MapsToExternalCancellation()
    {
        var builder = Host.CreateApplicationBuilder();
        using var host = builder.Build();
        var applicationHost = new ApplicationHost<HostApplicationBuilder>(builder, host);
        var command = new HostWinsProbeCommand();
        var commandInfo = SubCommandInfo.FromCommand(typeof(HostWinsProbeCommand), command);
        var lifetime = new LifetimeGlobalService();
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        using var scope = new CommandShutdownScope(CancellationToken.None, signal, sigterm);

        sigterm.Raise();

        Task invokeTask = CommandRunOrchestrator.InvokeAsync(command, applicationHost, lifetime, scope, commandInfo);
        Task bounded = await Task.WhenAny(invokeTask, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.Same(invokeTask, bounded);

        await Assert.ThrowsAsync<CommandExecutor.ExternalCancellationException>(() => invokeTask);
    }

    [Fact]
    public async Task Orchestrator_HostSuccessWithCtrlC_WhenCommandNeverSettles_MapsToExternalCancellation()
    {
        var builder = Host.CreateApplicationBuilder();
        using var host = builder.Build();
        var applicationHost = new ApplicationHost<HostApplicationBuilder>(builder, host);
        var command = new HostSuccessNeverSettlingProbeCommand();
        var commandInfo = SubCommandInfo.FromCommand(typeof(HostSuccessNeverSettlingProbeCommand), command);
        var lifetime = new LifetimeGlobalService();
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        using var scope = new CommandShutdownScope(CancellationToken.None, signal, sigterm);
        using var release = new CancellationTokenSource();
        HostSuccessNeverSettlingProbeCommand.ReleaseToken = release.Token;
        Task hostStarted = WaitForHostStartedAsync(applicationHost);

        Task invokeTask = CommandRunOrchestrator.InvokeAsync(command, applicationHost, lifetime, scope, commandInfo);
        try
        {
            await WaitForSignalAsync(hostStarted, "host start");
            signal.RaiseCtrlC();

            Task bounded = await Task.WhenAny(invokeTask, Task.Delay(TimeSpan.FromSeconds(15)));
            Assert.Same(invokeTask, bounded);

            await Assert.ThrowsAsync<CommandExecutor.ExternalCancellationException>(() => invokeTask);
        }
        finally
        {
            release.Cancel();
        }
    }

    [Fact]
    public async Task Orchestrator_HostSuccessWithSigterm_WhenCommandSettlesDuringDrain_MapsToExternalCancellation()
    {
        var builder = Host.CreateApplicationBuilder();
        using var host = builder.Build();
        var applicationHost = new ApplicationHost<HostApplicationBuilder>(builder, host);
        var command = new HostSuccessSettlingProbeCommand();
        var commandInfo = SubCommandInfo.FromCommand(typeof(HostSuccessSettlingProbeCommand), command);
        var lifetime = new LifetimeGlobalService();
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        using var scope = new CommandShutdownScope(CancellationToken.None, signal, sigterm);
        Task hostStarted = WaitForHostStartedAsync(applicationHost);

        Task invokeTask = CommandRunOrchestrator.InvokeAsync(command, applicationHost, lifetime, scope, commandInfo);

        await WaitForSignalAsync(hostStarted, "host start");
        sigterm.Raise();

        Task bounded = await Task.WhenAny(invokeTask, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.Same(invokeTask, bounded);

        await Assert.ThrowsAsync<CommandExecutor.ExternalCancellationException>(() => invokeTask);
    }

    [Fact]
    public async Task SigtermDuringExitingCallbacks_CompletesCleanupAndReturnsCanceledExitCode()
    {
        var command = new ExitingCleanupProbeCommand();
        var commandInfo = SubCommandInfo.FromCommand(typeof(ExitingCleanupProbeCommand), command);
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        var executor = new CommandExecutor(
            ApplicationBuilder.Create(),
            new ConsoleOutput(TextWriter.Null, TextWriter.Null),
            signal,
            sigterm);

        var exitCode = await RunCapturedAsync(async () =>
        {
            Task executeTask = executor.ExecuteCommand(commandInfo, CancellationToken.None);

            await WaitForSignalAsync(command.ExitingStarted.Task, "exiting start");
            sigterm.Raise();
            command.ReleaseExiting.TrySetResult(true);

            return await MapExternalAbortExitCodeAsync(executeTask);
        });

        Assert.Equal(CanceledExitCode, exitCode);
        Assert.True(command.ExitingCompleted, "Expected the ApplicationExiting callback to complete.");
        Assert.Equal(1, command.ExitedCount);
    }

    [Fact]
    public async Task SigtermDuringExitedCallbacks_CompletesCleanupAndReturnsCanceledExitCode()
    {
        var command = new ExitedCleanupProbeCommand();
        var commandInfo = SubCommandInfo.FromCommand(typeof(ExitedCleanupProbeCommand), command);
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        var executor = new CommandExecutor(
            ApplicationBuilder.Create(),
            new ConsoleOutput(TextWriter.Null, TextWriter.Null),
            signal,
            sigterm);

        var exitCode = await RunCapturedAsync(async () =>
        {
            Task executeTask = executor.ExecuteCommand(commandInfo, CancellationToken.None);

            await WaitForSignalAsync(command.ExitedStarted.Task, "exited start");
            sigterm.Raise();
            await WaitForSignalAsync(command.AbortObserved.Task, "SIGTERM observation");
            command.ReleaseExited.TrySetResult(true);

            return await MapExternalAbortExitCodeAsync(executeTask);
        });

        Assert.Equal(CanceledExitCode, exitCode);
        Assert.True(command.ExitedCompleted, "Expected the ApplicationExited callback to complete.");
    }

    [Fact]
    public async Task ExternalAbortDuringExitingCallbacks_RunAsyncReturnsCanceledExitCode()
    {
        var command = new ExitingCleanupProbeCommand();
        var builder = CreateBuilder(command);
        using var cts = new CancellationTokenSource();
        Task<int> runTask = RunCapturedAsync(() => builder.RunAsync([], cts.Token));
        try
        {
            await WaitForSignalAsync(command.ExitingStarted.Task, "exiting start");
            cts.Cancel();
            command.ReleaseExiting.TrySetResult(true);

            var exitCode = await runTask;

            Assert.Equal(CanceledExitCode, exitCode);
            Assert.True(command.ExitingCompleted, "Expected the ApplicationExiting callback to complete.");
            Assert.Equal(1, command.ExitedCount);
        }
        finally
        {
            cts.Cancel();
            command.ReleaseExiting.TrySetResult(true);
            await runTask;
        }
    }

    [Fact]
    public async Task SigtermDuringExitedCallbacks_RunAsyncReturnsCanceledExitCode()
    {
        if (!OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS())
        {
            return;
        }

        var command = new ExitedCleanupProbeCommand();
        var builder = CreateBuilder(command);
        using var sigtermGuard = PosixSignalRegistration.Create(
            PosixSignal.SIGTERM,
            static context => { context.Cancel = true; });

        Task<int> runTask = RunCapturedAsync(() => builder.RunAsync([]));
        try
        {
            await WaitForSignalAsync(command.ExitedStarted.Task, "exited start");

            Assert.Equal(0, Kill(Environment.ProcessId, SigtermSignalNumber));
            await WaitForSignalAsync(command.AbortObserved.Task, "SIGTERM observation");

            command.ReleaseExited.TrySetResult(true);

            var exitCode = await runTask;

            Assert.Equal(CanceledExitCode, exitCode);
            Assert.True(command.ExitedCompleted, "Expected the ApplicationExited callback to complete.");
        }
        finally
        {
            command.ReleaseExited.TrySetResult(true);
            await runTask;
        }
    }

    [Fact]
    public async Task Orchestrator_CommandSuccessWithCtrlC_MapsToExternalCancellation()
    {
        var builder = Host.CreateApplicationBuilder();
        using var host = builder.Build();
        var applicationHost = new ApplicationHost<HostApplicationBuilder>(builder, host);
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        using var scope = new CommandShutdownScope(CancellationToken.None, signal, sigterm);
        var command = new CommandSuccessProbeCommand(signal.RaiseCtrlC);
        var commandInfo = SubCommandInfo.FromCommand(typeof(CommandSuccessProbeCommand), command);
        var lifetime = new LifetimeGlobalService();
        int exitingCount = 0;
        lifetime.ApplicationExitingCallback(() => Interlocked.Increment(ref exitingCount));

        Task invokeTask = CommandRunOrchestrator.InvokeAsync(command, applicationHost, lifetime, scope, commandInfo);
        Task bounded = await Task.WhenAny(invokeTask, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.Same(invokeTask, bounded);

        await Assert.ThrowsAsync<CommandExecutor.ExternalCancellationException>(() => invokeTask);
        Assert.Equal(1, exitingCount);
    }

    [Fact]
    public async Task Orchestrator_CommandSuccessWithSigterm_MapsToExternalCancellation()
    {
        var builder = Host.CreateApplicationBuilder();
        using var host = builder.Build();
        var applicationHost = new ApplicationHost<HostApplicationBuilder>(builder, host);
        var signal = new CapturingCancelSignal();
        var sigterm = new FakeSigtermSignal();
        using var scope = new CommandShutdownScope(CancellationToken.None, signal, sigterm);
        var command = new CommandSuccessProbeCommand(sigterm.Raise);
        var commandInfo = SubCommandInfo.FromCommand(typeof(CommandSuccessProbeCommand), command);
        var lifetime = new LifetimeGlobalService();
        int exitingCount = 0;
        lifetime.ApplicationExitingCallback(() => Interlocked.Increment(ref exitingCount));

        Task invokeTask = CommandRunOrchestrator.InvokeAsync(command, applicationHost, lifetime, scope, commandInfo);
        Task bounded = await Task.WhenAny(invokeTask, Task.Delay(TimeSpan.FromSeconds(15)));
        Assert.Same(invokeTask, bounded);

        await Assert.ThrowsAsync<CommandExecutor.ExternalCancellationException>(() => invokeTask);
        Assert.Equal(1, exitingCount);
    }

    [Fact]
    public void ShutdownScope_Dispose_UnsubscribesSignalOnce()
    {
        var signal = new CapturingCancelSignal();
        var scope = new CommandShutdownScope(CancellationToken.None, signal);
        Assert.NotNull(signal.Captured);

        scope.Dispose();
        scope.Dispose();

        Assert.Null(signal.Captured);
        Assert.Equal(1, signal.UnsubscribeCount);
    }

    [Fact]
    public async Task PrecanceledToken_MapsToCanceledExitCode()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var exitCode = await RunCapturedAsync(() => CreateBuilder<CancelObservingCommand>().RunAsync([], cts.Token));

        Assert.Equal(CanceledExitCode, exitCode);
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand, new()
        => CreateBuilder().AddCommand<TCommand, ApplicationBuilder>();

    private static ApplicationBuilder CreateBuilder(ICommand command)
        => CreateBuilder().AddCommand<ApplicationBuilder>(command);

    private static ApplicationBuilder CreateBuilder()
        => ApplicationBuilder.Create()
            .SetExecutableName("abort-probe")
            .SetExecutableTitle("Abort Probe")
            .SetExecutableDescription("External abort verification CLI.")
            .SetExecutableVersion("9.9.9");

    private static async Task WaitForHostStartedAsync(ApplicationHost<HostApplicationBuilder> applicationHost)
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

    private static async Task<int> MapExternalAbortExitCodeAsync(Task executeTask)
    {
        try
        {
            await executeTask.ConfigureAwait(false);
            return 0;
        }
        catch (CommandExecutor.ExternalCancellationException)
        {
            return CanceledExitCode;
        }
    }

    private const int SigtermSignalNumber = 15;

    [DllImport("libc", EntryPoint = "kill", SetLastError = true)]
    private static extern int Kill(int processId, int signal);

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
