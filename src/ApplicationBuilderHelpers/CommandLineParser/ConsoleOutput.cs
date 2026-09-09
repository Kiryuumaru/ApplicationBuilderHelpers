using System;
using System.IO;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Internal decoupling of console output for testability.
/// Routes normal output (help/version) to <see cref="Out"/> and
/// diagnostics (errors, fallback notes) to <see cref="Error"/>,
/// defaulting to <see cref="Console.Out"/> / <see cref="Console.Error"/>.
/// Color is only applied when writing to the real console and the
/// corresponding stream is not redirected.
/// </summary>
internal sealed class ConsoleOutput
{
    public TextWriter Out { get; }

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

    internal void Write(string? value, ConsoleColor? color = null)
    {
        WriteInternal(Out, value, color, isError: false, newLine: false);
    }

    internal void WriteLine()
    {
        Out.WriteLine();
    }

    internal void WriteLine(string? value, ConsoleColor? color = null)
    {
        WriteInternal(Out, value, color, isError: false, newLine: true);
    }

    internal void WriteError(string? value, ConsoleColor? color = null)
    {
        WriteInternal(Error, value, color, isError: true, newLine: false);
    }

    internal void WriteLineError()
    {
        Error.WriteLine();
    }

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
                // Stream may have closed mid-write; value already attempted.
            }
            catch (PlatformNotSupportedException)
            {
                // Color unsupported; text already written or unrecoverable.
            }
            catch (UnauthorizedAccessException)
            {
                // Console access denied; text already written or unrecoverable.
            }
            finally
            {
                try
                {
                    Console.ForegroundColor = originalColor;
                }
                catch (IOException)
                {
                }
                catch (PlatformNotSupportedException)
                {
                }
                catch (UnauthorizedAccessException)
                {
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
