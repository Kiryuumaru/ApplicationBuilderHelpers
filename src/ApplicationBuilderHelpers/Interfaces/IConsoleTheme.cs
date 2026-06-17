using System;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Defines a console color theme for CLI help output using System.ConsoleColor.
/// </summary>
public interface IConsoleTheme
{
    /// <summary>
    /// Color for section headers like "USAGE:", "OPTIONS:", "COMMANDS:", etc.
    /// </summary>
    ConsoleColor HeaderColor { get; }

    /// <summary>
    /// Color for command names and option flags like "--help", "-v", "build"
    /// </summary>
    ConsoleColor FlagColor { get; }

    /// <summary>
    /// Color for parameter placeholders like "&lt;FILE&gt;", "&lt;COMMAND&gt;", "&lt;VALUE&gt;"
    /// </summary>
    ConsoleColor ParameterColor { get; }

    /// <summary>
    /// Color for descriptions and main text content
    /// </summary>
    ConsoleColor DescriptionColor { get; }

    /// <summary>
    /// Color for default values, environment variables, and secondary information
    /// </summary>
    ConsoleColor SecondaryColor { get; }

    /// <summary>
    /// Color for required field indicators and important warnings
    /// </summary>
    ConsoleColor RequiredColor { get; }
}