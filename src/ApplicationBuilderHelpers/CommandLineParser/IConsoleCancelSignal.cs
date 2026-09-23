using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Injectable abstraction over the console cancel signal (Ctrl+C).
/// Joins the <c>CancelKeyPress</c> source with the host lifetime's
/// <c>ApplicationStopping</c> counterpart through the linked CTS owned by
/// <see cref="CommandShutdownScope"/>: this side observes Ctrl+C, the host run
/// joins <c>ApplicationStopping</c> downstream, and the scope's token is the
/// single combined cancellation for the command task and host task run together.
/// Exists so tests can simulate Ctrl+C in-process with a fake.
/// </summary>
internal interface IConsoleCancelSignal
{
    /// <summary>
    /// Subscribes <paramref name="handler"/> to the cancel signal.
    /// Returns <c>true</c> when subscribed; writes state info to the
    /// standard output stream as a fallback message and
    /// returns <c>false</c> when the signal is unsupported on this platform.
    /// </summary>
    bool Subscribe(ConsoleCancelEventHandler handler);

    /// <summary>
    /// Unsubscribes a subscribed <paramref name="handler"/>.
    /// </summary>
    void Unsubscribe(ConsoleCancelEventHandler handler);
}
