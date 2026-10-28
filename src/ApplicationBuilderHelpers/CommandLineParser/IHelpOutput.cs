using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Output seam for the help renderer; <see cref="ConsoleOutput"/> drops colors on redirected streams.
/// Exists so tests can capture the exact text/color pairs the renderer writes.
/// </summary>
internal interface IHelpOutput
{
    void Write(string? text, ConsoleColor? color = null);

    void WriteLine();

    void WriteLine(string? text, ConsoleColor? color = null);
}
