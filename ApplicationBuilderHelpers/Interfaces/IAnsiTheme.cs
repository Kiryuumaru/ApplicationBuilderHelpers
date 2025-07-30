using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Defines an ANSI color theme for CLI help output.
/// </summary>
public interface IAnsiTheme
{
    /// <summary>
    /// Color for section headers like "USAGE:", "OPTIONS:", "COMMANDS:", etc.
    /// </summary>
    string HeaderColor { get; }

    /// <summary>
    /// Color for command names and option flags like "--help", "-v", "build"
    /// </summary>
    string FlagColor { get; }

    /// <summary>
    /// Color for parameter placeholders like "&lt;FILE&gt;", "&lt;COMMAND&gt;", "&lt;VALUE&gt;"
    /// </summary>
    string ParameterColor { get; }

    /// <summary>
    /// Color for descriptions and main text content
    /// </summary>
    string DescriptionColor { get; }

    /// <summary>
    /// Color for default values, environment variables, and secondary information
    /// </summary>
    string SecondaryColor { get; }

    /// <summary>
    /// Color for required field indicators and important warnings
    /// </summary>
    string RequiredColor { get; }

    /// <summary>
    /// ANSI reset sequence to return to default colors
    /// </summary>
    string Reset { get; }
}