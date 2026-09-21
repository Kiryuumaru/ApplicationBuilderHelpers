using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Injectable abstraction over the console cancel signal (Ctrl+C).
/// Joins the <c>CancelKeyPress</c> source with the host lifetime's
/// <c>ApplicationStopping</c> counterpart via the linked CTS owned by
/// <see cref="CommandShutdownScope"/>: this side observes Ctrl+C, the host run
/// joins <c>ApplicationStopping</c> downstream, and the scope's token is the
/// single joined cancellation for the joint command/host run.
/// Exists so tests can simulate Ctrl+C in-process with a fake.
/// </summary>
internal interface IConsoleCancelSignal
{
    /// <summary>
    /// Subscribes <paramref name="handler"/> to the cancel signal.
    /// Returns <c>true</c> when subscribed; writes state info to the
    /// standard output stream as a fallback note and
    /// returns <c>false</c> when the signal is unsupported on this platform.
    /// </summary>
    bool Subscribe(ConsoleCancelEventHandler handler);

    /// <summary>
    /// Unsubscribes a previously subscribed <paramref name="handler"/>.
    /// </summary>
    void Unsubscribe(ConsoleCancelEventHandler handler);
}
