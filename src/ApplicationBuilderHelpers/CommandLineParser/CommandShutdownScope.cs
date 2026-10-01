using System;
using System.Threading;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Per-execution shutdown scope: a linked CTS joining the outer token with the
/// console cancel signal (Ctrl+C with <see cref="IConsoleCancelSignal"/>) and the
/// SIGTERM signal (with <see cref="ISigtermSignal"/>), plus
/// subscribe/dispose ownership for one <c>ExecuteCommand</c> run.
/// Host-side <c>ApplicationStopping</c> stays host-owned (joined downstream by the
/// host run itself) and is observed with the host-task outcome: linking it here would
/// cancel the command token on host stop and break the host-wins drain
/// contract, so the scope joins outer + Ctrl+C + SIGTERM only. Never link
/// <c>ApplicationStopping</c> here.
/// </summary>
internal sealed class CommandShutdownScope : IDisposable
{
    private readonly CancellationTokenSource _shutdownCts;
    private readonly IConsoleCancelSignal _signal;
    private readonly ISigtermSignal? _sigtermSignal;
    private readonly bool _ownsSigtermSignal;
    private readonly Action? _sigtermHandler;
    private readonly ConsoleCancelEventHandler _handler;
    private bool _subscribed;
    private bool _disposed;
    private volatile bool _ctrlCCanceled;
    private volatile bool _sigtermCanceled;

    internal CommandShutdownScope(CancellationToken outerToken, IConsoleCancelSignal signal)
        : this(outerToken, signal, null)
    {
    }

    internal CommandShutdownScope(CancellationToken outerToken, IConsoleCancelSignal signal, ISigtermSignal? sigtermSignal)
    {
        OuterToken = outerToken;
        _signal = signal ?? throw new ArgumentNullException(nameof(signal));
        if (sigtermSignal is null)
        {
            sigtermSignal = new PosixSigtermSignal();
            _ownsSigtermSignal = true;
        }

        _sigtermSignal = sigtermSignal;
        _shutdownCts = CancellationTokenSource.CreateLinkedTokenSource(outerToken);

        _handler = (sender, e) =>
        {
            _ctrlCCanceled = true;
            try { _shutdownCts.Cancel(); } catch (ObjectDisposedException) { }
            e.Cancel = true;
        };

        if (_sigtermSignal is not null)
        {
            _sigtermHandler = () =>
            {
                _sigtermCanceled = true;
                try { _shutdownCts.Cancel(); } catch (ObjectDisposedException) { }
            };
        }

        try
        {
            _subscribed = _signal.Subscribe(_handler);
            if (_sigtermSignal is not null)
            {
                _sigtermSignal.Signaled += _sigtermHandler!;
            }
        }
        catch
        {
            if (_subscribed)
            {
                _signal.Unsubscribe(_handler);
                _subscribed = false;
            }

            if (_ownsSigtermSignal && _sigtermSignal is IDisposable ownedDisposable)
            {
                ownedDisposable.Dispose();
            }

            _shutdownCts.Dispose();
            throw;
        }
    }

    /// <summary>
    /// The combined shutdown token for the command task and host task run together.
    /// </summary>
    internal CancellationToken Token => _shutdownCts.Token;

    /// <summary>
    /// The caller's token passed to <c>ExecuteCommand</c>.
    /// </summary>
    internal CancellationToken OuterToken { get; }

    /// <summary>
    /// Whether Ctrl+C was observed during this scope's lifetime.
    /// </summary>
    internal bool CtrlCCanceled => _ctrlCCanceled;

    /// <summary>
    /// Whether SIGTERM was observed during this scope's lifetime.
    /// </summary>
    internal bool SigtermCanceled => _sigtermCanceled;

    /// <summary>
    /// Single classification where cancellation takes precedence: shutdown was requested
    /// with the outer token, Ctrl+C, or SIGTERM.
    /// </summary>
    internal bool IsExternalAbortRequested =>
        CommandExitMapper.IsExternalAbort(_shutdownCts.Token, OuterToken, CtrlCCanceled, SigtermCanceled);

    /// <summary>
    /// Throws <see cref="CommandExecutor.ExternalCancellationException"/> when
    /// cancellation was requested with the passed token, Ctrl+C, or SIGTERM.
    /// </summary>
    internal void ThrowIfExternalAbort() =>
        CommandExitMapper.ThrowIfExternalAbort(_shutdownCts.Token, OuterToken, CtrlCCanceled, SigtermCanceled);

    /// <inheritdoc />
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        if (_sigtermSignal is not null && _sigtermHandler is not null)
        {
            _sigtermSignal.Signaled -= _sigtermHandler;
        }

        if (_subscribed)
        {
            _signal.Unsubscribe(_handler);
        }

        if (_ownsSigtermSignal && _sigtermSignal is IDisposable disposable)
        {
            disposable.Dispose();
        }

        _shutdownCts.Dispose();
    }
}
