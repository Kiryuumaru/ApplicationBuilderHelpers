using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Injectable abstraction over the console cancel signal (Ctrl+C only).
/// This side observes Ctrl+C; <see cref="CommandShutdownScope"/> joins outer +
/// Ctrl+C + SIGTERM, and the host lifetime's <c>ApplicationStopping</c> stays
/// host-owned.
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
