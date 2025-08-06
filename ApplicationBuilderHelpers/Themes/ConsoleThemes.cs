using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.Themes;

/// <summary>
/// Default console color theme - colorful and well-contrasted.
/// </summary>
public class DefaultConsoleTheme : IConsoleTheme
{
    public static DefaultConsoleTheme Instance { get; } = new DefaultConsoleTheme();

    public ConsoleColor HeaderColor => ConsoleColor.Yellow;
    public ConsoleColor FlagColor => ConsoleColor.Green;
    public ConsoleColor ParameterColor => ConsoleColor.Cyan;
    public ConsoleColor DescriptionColor => ConsoleColor.White;
    public ConsoleColor SecondaryColor => ConsoleColor.Gray;
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>
/// Monochrome console theme - uses only grayscale colors for maximum compatibility.
/// </summary>
public class MonochromeConsoleTheme : IConsoleTheme
{
    public static MonochromeConsoleTheme Instance { get; } = new MonochromeConsoleTheme();

    public ConsoleColor HeaderColor => ConsoleColor.White;
    public ConsoleColor FlagColor => ConsoleColor.Gray;
    public ConsoleColor ParameterColor => ConsoleColor.DarkGray;
    public ConsoleColor DescriptionColor => ConsoleColor.White;
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;
    public ConsoleColor RequiredColor => ConsoleColor.White;
}

/// <summary>
/// High contrast console theme - maximum visibility with bright colors.
/// </summary>
public class HighContrastConsoleTheme : IConsoleTheme
{
    public static HighContrastConsoleTheme Instance { get; } = new HighContrastConsoleTheme();

    public ConsoleColor HeaderColor => ConsoleColor.Yellow;
    public ConsoleColor FlagColor => ConsoleColor.Cyan;
    public ConsoleColor ParameterColor => ConsoleColor.Magenta;
    public ConsoleColor DescriptionColor => ConsoleColor.White;
    public ConsoleColor SecondaryColor => ConsoleColor.Gray;
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>
/// Minimal console theme - subtle colors for a clean, professional appearance.
/// </summary>
public class MinimalConsoleTheme : IConsoleTheme
{
    public static MinimalConsoleTheme Instance { get; } = new MinimalConsoleTheme();

    public ConsoleColor HeaderColor => ConsoleColor.Blue;
    public ConsoleColor FlagColor => ConsoleColor.DarkCyan;
    public ConsoleColor ParameterColor => ConsoleColor.DarkBlue;
    public ConsoleColor DescriptionColor => ConsoleColor.Gray;
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;
    public ConsoleColor RequiredColor => ConsoleColor.DarkRed;
}

/// <summary>
/// Dark console theme - designed for dark terminal backgrounds.
/// </summary>
public class DarkConsoleTheme : IConsoleTheme
{
    public static DarkConsoleTheme Instance { get; } = new DarkConsoleTheme();

    public ConsoleColor HeaderColor => ConsoleColor.Magenta;
    public ConsoleColor FlagColor => ConsoleColor.Green;
    public ConsoleColor ParameterColor => ConsoleColor.Cyan;
    public ConsoleColor DescriptionColor => ConsoleColor.White;
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>
/// Light console theme - designed for light terminal backgrounds.
/// </summary>
public class LightConsoleTheme : IConsoleTheme
{
    public static LightConsoleTheme Instance { get; } = new LightConsoleTheme();

    public ConsoleColor HeaderColor => ConsoleColor.DarkBlue;
    public ConsoleColor FlagColor => ConsoleColor.DarkGreen;
    public ConsoleColor ParameterColor => ConsoleColor.DarkCyan;
    public ConsoleColor DescriptionColor => ConsoleColor.Black;
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;
    public ConsoleColor RequiredColor => ConsoleColor.DarkRed;
}