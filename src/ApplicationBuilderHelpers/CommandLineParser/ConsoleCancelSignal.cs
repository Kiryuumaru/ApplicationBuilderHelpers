using System;
using System.IO;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Production <see cref="IConsoleCancelSignal"/> over <see cref="ConsoleOutput"/>,
/// which stays the adapter-only forwarder to <see cref="Console.CancelKeyPress"/>.
/// Moved verbatim from the executor's subscribe/unsubscribe block (mechanical
/// split, no behavior change): the two fallback notes are preserved exactly.
/// </summary>
internal sealed class ConsoleCancelSignal(ConsoleOutput consoleOutput) : IConsoleCancelSignal
{
    /// <inheritdoc />
    public bool Subscribe(ConsoleCancelEventHandler handler)
    {
        try
        {
            consoleOutput.CancelKeyPress += handler;
            return true;
        }
        catch (PlatformNotSupportedException)
        {
            consoleOutput.WriteLineError("Note: Console.CancelKeyPress is not supported on this platform.");
            return false;
        }
        catch (IOException)
        {
            consoleOutput.WriteLineError("Note: Console.CancelKeyPress is unavailable (I/O).");
            return false;
        }
    }

    /// <inheritdoc />
    public void Unsubscribe(ConsoleCancelEventHandler handler)
    {
        consoleOutput.CancelKeyPress -= handler;
    }
}
