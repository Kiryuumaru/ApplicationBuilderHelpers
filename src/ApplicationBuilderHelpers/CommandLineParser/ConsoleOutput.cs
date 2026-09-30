using System;
using System.IO;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Internal decoupling of console output for testability.
/// </summary>
internal sealed class ConsoleOutput
{
    /// <summary>
    /// Sink for normal output (help/version).
    /// </summary>
    public TextWriter Out { get; }

    /// <summary>
    /// Sink for diagnostics.
    /// </summary>
    public TextWriter Error { get; }

    public ConsoleOutput()
        : this(Console.Out, Console.Error)
    {
    }

    internal ConsoleOutput(TextWriter outWriter, TextWriter errorWriter)
    {
        Out = outWriter ?? throw new ArgumentNullException(nameof(outWriter));
        Error = errorWriter ?? throw new ArgumentNullException(nameof(errorWriter));
    }

    internal event ConsoleCancelEventHandler CancelKeyPress
    {
        add => Console.CancelKeyPress += value;
        remove => Console.CancelKeyPress -= value;
    }

    /// <summary>
    /// Writes <paramref name="value"/> to <see cref="Out"/>.
    /// </summary>
    internal void Write(string? value, ConsoleColor? color = null)
    {
        WriteInternal(Out, value, color, isError: false, newLine: false);
    }

    /// <summary>
    /// Writes a blank line to <see cref="Out"/>.
    /// </summary>
    internal void WriteLine()
    {
        Out.WriteLine();
    }

    /// <summary>
    /// Writes <paramref name="value"/> plus newline to <see cref="Out"/>.
    /// </summary>
    internal void WriteLine(string? value, ConsoleColor? color = null)
    {
        WriteInternal(Out, value, color, isError: false, newLine: true);
    }

    /// <summary>
    /// Writes <paramref name="value"/> to <see cref="Error"/>.
    /// </summary>
    internal void WriteError(string? value, ConsoleColor? color = null)
    {
        WriteInternal(Error, value, color, isError: true, newLine: false);
    }

    /// <summary>
    /// Writes a blank line to <see cref="Error"/>.
    /// </summary>
    internal void WriteLineError()
    {
        Error.WriteLine();
    }

    /// <summary>
    /// Writes <paramref name="value"/> plus newline to <see cref="Error"/>.
    /// </summary>
    internal void WriteLineError(string? value, ConsoleColor? color = null)
    {
        WriteInternal(Error, value, color, isError: true, newLine: true);
    }

    private void WriteInternal(TextWriter writer, string? value, ConsoleColor? color, bool isError, bool newLine)
    {
        if (color.HasValue && SupportsColor(writer, isError))
        {
            ConsoleColor originalColor;
            try
            {
                originalColor = Console.ForegroundColor;
            }
            catch (IOException)
            {
                WritePlain(writer, value, newLine);
                return;
            }
            catch (PlatformNotSupportedException)
            {
                WritePlain(writer, value, newLine);
                return;
            }
            catch (UnauthorizedAccessException)
            {
                WritePlain(writer, value, newLine);
                return;
            }

            try
            {
                Console.ForegroundColor = color.Value;
                WritePlain(writer, value, newLine);
            }
            catch (IOException)
            {
                // Color set/restore is best-effort; the value is still written plain below.
            }
            catch (PlatformNotSupportedException)
            {
                // Color set/restore is best-effort; the value is still written plain below.
            }
            catch (UnauthorizedAccessException)
            {
                // Color set/restore is best-effort; the value is still written plain below.
            }
            finally
            {
                try
                {
                    Console.ForegroundColor = originalColor;
                }
                catch (IOException)
                {
                    // Color set/restore is best-effort; the value was already written.
                }
                catch (PlatformNotSupportedException)
                {
                    // Color set/restore is best-effort; the value was already written.
                }
                catch (UnauthorizedAccessException)
                {
                    // Color set/restore is best-effort; the value was already written.
                }
            }

            return;
        }

        WritePlain(writer, value, newLine);
    }

    private static void WritePlain(TextWriter writer, string? value, bool newLine)
    {
        if (newLine)
        {
            if (value is null)
            {
                writer.WriteLine();
            }
            else
            {
                writer.WriteLine(value);
            }
        }
        else
        {
            writer.Write(value);
        }
    }

    private static bool SupportsColor(TextWriter writer, bool isError)
    {
        try
        {
            if (isError)
            {
                if (!ReferenceEquals(writer, Console.Error))
                {
                    return false;
                }

                return !Console.IsErrorRedirected;
            }

            if (!ReferenceEquals(writer, Console.Out))
            {
                return false;
            }

            return !Console.IsOutputRedirected;
        }
        catch (IOException)
        {
            return false;
        }
        catch (PlatformNotSupportedException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}
