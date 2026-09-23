using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;
using System.Reflection;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Cancellation classification coverage through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// <see cref="CommandExitMapper"/> cancel-wins mapping plus
/// <see cref="CommandExitMapper.ThrowIfExternalAbort"/> throw/no-throw, and
/// <see cref="CommandShutdownScope"/> Ctrl+C observation through a capturing
/// <see cref="IConsoleCancelSignal"/> fake, with the executor 130 mapping.
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

    [Command("External abort probe.")]
    private sealed class CancelObservingCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
            => await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
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
        => ApplicationBuilder.Create()
            .SetExecutableName("abort-probe")
            .SetExecutableTitle("Abort Probe")
            .SetExecutableDescription("External abort verification CLI.")
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
