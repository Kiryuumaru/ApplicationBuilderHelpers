using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.Themes;

/// <summary>
/// Default console color theme - colorful and well-contrasted.
/// </summary>
public class DefaultConsoleTheme : IConsoleTheme
{
    /// <summary>
    /// Gets the singleton instance of the default console theme.
    /// </summary>
    public static DefaultConsoleTheme Instance { get; } = new DefaultConsoleTheme();

    /// <summary>
    /// Gets the color used for headers in the console output.
    /// </summary>
    public ConsoleColor HeaderColor => ConsoleColor.Yellow;

    /// <summary>
    /// Gets the color used for command flags in the console output.
    /// </summary>
    public ConsoleColor FlagColor => ConsoleColor.Green;

    /// <summary>
    /// Gets the color used for parameters in the console output.
    /// </summary>
    public ConsoleColor ParameterColor => ConsoleColor.Cyan;

    /// <summary>
    /// Gets the color used for descriptions in the console output.
    /// </summary>
    public ConsoleColor DescriptionColor => ConsoleColor.White;

    /// <summary>
    /// Gets the color used for secondary text in the console output.
    /// </summary>
    public ConsoleColor SecondaryColor => ConsoleColor.Gray;

    /// <summary>
    /// Gets the color used for required fields in the console output.
    /// </summary>
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>
/// Monochrome console theme - uses only grayscale colors for maximum compatibility.
/// </summary>
public class MonochromeConsoleTheme : IConsoleTheme
{
    /// <summary>
    /// Gets the singleton instance of the monochrome console theme.
    /// </summary>
    public static MonochromeConsoleTheme Instance { get; } = new MonochromeConsoleTheme();

    /// <summary>
    /// Gets the color used for headers in the console output.
    /// </summary>
    public ConsoleColor HeaderColor => ConsoleColor.White;

    /// <summary>
    /// Gets the color used for command flags in the console output.
    /// </summary>
    public ConsoleColor FlagColor => ConsoleColor.Gray;

    /// <summary>
    /// Gets the color used for parameters in the console output.
    /// </summary>
    public ConsoleColor ParameterColor => ConsoleColor.DarkGray;

    /// <summary>
    /// Gets the color used for descriptions in the console output.
    /// </summary>
    public ConsoleColor DescriptionColor => ConsoleColor.White;

    /// <summary>
    /// Gets the color used for secondary text in the console output.
    /// </summary>
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;

    /// <summary>
    /// Gets the color used for required fields in the console output.
    /// </summary>
    public ConsoleColor RequiredColor => ConsoleColor.White;
}

/// <summary>
/// High contrast console theme - maximum visibility with bright colors.
/// </summary>
public class HighContrastConsoleTheme : IConsoleTheme
{
    /// <summary>
    /// Gets the singleton instance of the high contrast console theme.
    /// </summary>
    public static HighContrastConsoleTheme Instance { get; } = new HighContrastConsoleTheme();

    /// <summary>
    /// Gets the color used for headers in the console output.
    /// </summary>
    public ConsoleColor HeaderColor => ConsoleColor.Yellow;

    /// <summary>
    /// Gets the color used for command flags in the console output.
    /// </summary>
    public ConsoleColor FlagColor => ConsoleColor.Cyan;

    /// <summary>
    /// Gets the color used for parameters in the console output.
    /// </summary>
    public ConsoleColor ParameterColor => ConsoleColor.Magenta;

    /// <summary>
    /// Gets the color used for descriptions in the console output.
    /// </summary>
    public ConsoleColor DescriptionColor => ConsoleColor.White;

    /// <summary>
    /// Gets the color used for secondary text in the console output.
    /// </summary>
    public ConsoleColor SecondaryColor => ConsoleColor.Gray;

    /// <summary>
    /// Gets the color used for required fields in the console output.
    /// </summary>
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>
/// Minimal console theme - subtle colors for a clean, professional appearance.
/// </summary>
public class MinimalConsoleTheme : IConsoleTheme
{
    /// <summary>
    /// Gets the singleton instance of the minimal console theme.
    /// </summary>
    public static MinimalConsoleTheme Instance { get; } = new MinimalConsoleTheme();

    /// <summary>
    /// Gets the color used for headers in the console output.
    /// </summary>
    public ConsoleColor HeaderColor => ConsoleColor.Blue;

    /// <summary>
    /// Gets the color used for command flags in the console output.
    /// </summary>
    public ConsoleColor FlagColor => ConsoleColor.DarkCyan;

    /// <summary>
    /// Gets the color used for parameters in the console output.
    /// </summary>
    public ConsoleColor ParameterColor => ConsoleColor.DarkBlue;

    /// <summary>
    /// Gets the color used for descriptions in the console output.
    /// </summary>
    public ConsoleColor DescriptionColor => ConsoleColor.Gray;

    /// <summary>
    /// Gets the color used for secondary text in the console output.
    /// </summary>
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;

    /// <summary>
    /// Gets the color used for required fields in the console output.
    /// </summary>
    public ConsoleColor RequiredColor => ConsoleColor.DarkRed;
}

/// <summary>
/// Dark console theme - designed for dark terminal backgrounds.
/// </summary>
public class DarkConsoleTheme : IConsoleTheme
{
    /// <summary>
    /// Gets the singleton instance of the dark console theme.
    /// </summary>
    public static DarkConsoleTheme Instance { get; } = new DarkConsoleTheme();

    /// <summary>
    /// Gets the color used for headers in the console output.
    /// </summary>
    public ConsoleColor HeaderColor => ConsoleColor.Magenta;

    /// <summary>
    /// Gets the color used for command flags in the console output.
    /// </summary>
    public ConsoleColor FlagColor => ConsoleColor.Green;

    /// <summary>
    /// Gets the color used for parameters in the console output.
    /// </summary>
    public ConsoleColor ParameterColor => ConsoleColor.Cyan;

    /// <summary>
    /// Gets the color used for descriptions in the console output.
    /// </summary>
    public ConsoleColor DescriptionColor => ConsoleColor.White;

    /// <summary>
    /// Gets the color used for secondary text in the console output.
    /// </summary>
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;

    /// <summary>
    /// Gets the color used for required fields in the console output.
    /// </summary>
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>
/// Light console theme - designed for light terminal backgrounds.
/// </summary>
public class LightConsoleTheme : IConsoleTheme
{
    /// <summary>
    /// Gets the singleton instance of the light console theme.
    /// </summary>
    public static LightConsoleTheme Instance { get; } = new LightConsoleTheme();

    /// <summary>
    /// Gets the console color used for headers in the light theme.
    /// </summary>
    public ConsoleColor HeaderColor => ConsoleColor.DarkBlue;
    
    /// <summary>
    /// Gets the console color used for flags in the light theme.
    /// </summary>
    public ConsoleColor FlagColor => ConsoleColor.DarkGreen;
    
    /// <summary>
    /// Gets the console color used for parameters in the light theme.
    /// </summary>
    public ConsoleColor ParameterColor => ConsoleColor.DarkCyan;
    
    /// <summary>
    /// Gets the console color used for descriptions in the light theme.
    /// </summary>
    public ConsoleColor DescriptionColor => ConsoleColor.Black;
    
    /// <summary>
    /// Gets the console color used for secondary text in the light theme.
    /// </summary>
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;
    
    /// <summary>
    /// Gets the console color used for required indicators in the light theme.
    /// </summary>
    public ConsoleColor RequiredColor => ConsoleColor.DarkRed;
}