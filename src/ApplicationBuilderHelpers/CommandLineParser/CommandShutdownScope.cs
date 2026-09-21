using System;
using System.Threading;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Per-execution shutdown scope: a linked CTS joining the outer token with the
/// console cancel signal (Ctrl+C via <see cref="IConsoleCancelSignal"/>), plus
/// subscribe/dispose ownership for one <c>ExecuteCommand</c> run.
/// Host-side <c>ApplicationStopping</c> stays host-owned (joined downstream by the
/// host run itself) and is observed via the host-task outcome: linking it here would
/// cancel the command token on graceful host stop and break the host-wins drain
/// contract, so the scope joins outer + Ctrl+C only.
/// </summary>
internal sealed class CommandShutdownScope : IDisposable
{
    private readonly CancellationTokenSource _shutdownCts;
    private readonly IConsoleCancelSignal _signal;
    private readonly ConsoleCancelEventHandler _handler;
    private readonly bool _subscribed;
    private bool _disposed;

    internal CommandShutdownScope(CancellationToken outerToken, IConsoleCancelSignal signal)
    {
        OuterToken = outerToken;
        _signal = signal ?? throw new ArgumentNullException(nameof(signal));
        _shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(outerToken);

        // Ctrl+C requests cancellation alongside the passed token.
        _handler = (sender, e) =>
        {
            CtrlCCanceled = true;
            _shutdownCts.Cancel();
            e.Cancel = true;
        };

        try
        {
            _subscribed = _signal.Subscribe(_handler);
        }
        catch
        {
            _shutdownCts.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The joined shutdown token for the joint command/host run.
    /// </summary>
    internal CancellationToken Token => _shutdownCts.Token;

    /// <summary>
    /// The caller's token passed to <c>ExecuteCommand</c>.
    /// </summary>
    internal CancellationToken OuterToken { get; }

    /// <summary>
    /// Whether Ctrl+C was observed during this scope's lifetime.
    /// </summary>
    internal bool CtrlCCanceled { get; private set; }

    /// <summary>
    /// Single cancel-wins decision point: shutdown was requested via the
    /// outer token or Ctrl+C.
    /// </summary>
    internal bool IsExternalAbortRequested =>
        CommandExitMapper.IsExternalAbort(_shutdownCts.Token, OuterToken, CtrlCCanceled);

    /// <summary>
    /// Throws <see cref="CommandExecutor.ExternalCancellationException"/> when
    /// cancellation was requested via the passed token or Ctrl+C.
    /// </summary>
    internal void ThrowIfExternalAbort() =>
        CommandExitMapper.ThrowIfExternalAbort(_shutdownCts.Token, OuterToken, CtrlCCanceled);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_subscribed)
        {
            _signal.Unsubscribe(_handler);
        }

        _shutdownCts.Dispose();
    }
}
