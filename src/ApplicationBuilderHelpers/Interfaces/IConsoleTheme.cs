using System;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Caller-view color contract for help output.
/// </summary>
public interface IConsoleTheme
{
    /// <summary>
    /// Gets the color for section headers (<c>USAGE:</c>, <c>OPTIONS:</c>, <c>COMMANDS:</c>).
    /// </summary>
    ConsoleColor HeaderColor { get; }

    /// <summary>
    /// Gets the color for command names and option flags (<c>--help</c>, <c>-v</c>, <c>build</c>).
    /// </summary>
    ConsoleColor FlagColor { get; }

    /// <summary>
    /// Gets the color for parameter placeholders (<c>&lt;FILE&gt;</c>, <c>&lt;COMMAND&gt;</c>, <c>&lt;VALUE&gt;</c>).
    /// </summary>
    ConsoleColor ParameterColor { get; }

    /// <summary>
    /// Gets the color for descriptions and main text content.
    /// </summary>
    ConsoleColor DescriptionColor { get; }

    /// <summary>
    /// Gets the color for default values, environment variables, and secondary information.
    /// </summary>
    ConsoleColor SecondaryColor { get; }

    /// <summary>
    /// Gets the color for required markers and important warnings.
    /// </summary>
    ConsoleColor RequiredColor { get; }
}
