using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.Themes;

/// <summary>Default theme for dark backgrounds.</summary>
public class DefaultConsoleTheme : IConsoleTheme
{
    /// <summary>Shared instance; themes are stateless.</summary>
    public static DefaultConsoleTheme Instance { get; } = new DefaultConsoleTheme();

    /// <summary>Section headers.</summary>
    public ConsoleColor HeaderColor => ConsoleColor.Yellow;

    /// <summary>Command names and option flags.</summary>
    public ConsoleColor FlagColor => ConsoleColor.Green;

    /// <summary>Value placeholders.</summary>
    public ConsoleColor ParameterColor => ConsoleColor.Cyan;

    /// <summary>Descriptions and main text.</summary>
    public ConsoleColor DescriptionColor => ConsoleColor.White;

    /// <summary>Default values and secondary info.</summary>
    public ConsoleColor SecondaryColor => ConsoleColor.Gray;

    /// <summary>Required markers and warnings.</summary>
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>Grayscale-only theme for terminals without color.</summary>
public class MonochromeConsoleTheme : IConsoleTheme
{
    /// <summary>Shared instance; themes are stateless.</summary>
    public static MonochromeConsoleTheme Instance { get; } = new MonochromeConsoleTheme();

    /// <summary>Section headers.</summary>
    public ConsoleColor HeaderColor => ConsoleColor.White;

    /// <summary>Command names and option flags.</summary>
    public ConsoleColor FlagColor => ConsoleColor.Gray;

    /// <summary>Value placeholders.</summary>
    public ConsoleColor ParameterColor => ConsoleColor.DarkGray;

    /// <summary>Descriptions and main text.</summary>
    public ConsoleColor DescriptionColor => ConsoleColor.White;

    /// <summary>Default values and secondary info.</summary>
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;

    /// <summary>Required markers and warnings.</summary>
    public ConsoleColor RequiredColor => ConsoleColor.White;
}

/// <summary>Saturated theme for maximum contrast.</summary>
public class HighContrastConsoleTheme : IConsoleTheme
{
    /// <summary>Shared instance; themes are stateless.</summary>
    public static HighContrastConsoleTheme Instance { get; } = new HighContrastConsoleTheme();

    /// <summary>Section headers.</summary>
    public ConsoleColor HeaderColor => ConsoleColor.Yellow;

    /// <summary>Command names and option flags.</summary>
    public ConsoleColor FlagColor => ConsoleColor.Cyan;

    /// <summary>Value placeholders.</summary>
    public ConsoleColor ParameterColor => ConsoleColor.Magenta;

    /// <summary>Descriptions and main text.</summary>
    public ConsoleColor DescriptionColor => ConsoleColor.White;

    /// <summary>Default values and secondary info.</summary>
    public ConsoleColor SecondaryColor => ConsoleColor.Gray;

    /// <summary>Required markers and warnings.</summary>
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>Muted theme that stays quiet beside command output.</summary>
public class MinimalConsoleTheme : IConsoleTheme
{
    /// <summary>Shared instance; themes are stateless.</summary>
    public static MinimalConsoleTheme Instance { get; } = new MinimalConsoleTheme();

    /// <summary>Section headers.</summary>
    public ConsoleColor HeaderColor => ConsoleColor.Blue;

    /// <summary>Command names and option flags.</summary>
    public ConsoleColor FlagColor => ConsoleColor.DarkCyan;

    /// <summary>Value placeholders.</summary>
    public ConsoleColor ParameterColor => ConsoleColor.DarkBlue;

    /// <summary>Descriptions and main text.</summary>
    public ConsoleColor DescriptionColor => ConsoleColor.Gray;

    /// <summary>Default values and secondary info.</summary>
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;

    /// <summary>Required markers and warnings.</summary>
    public ConsoleColor RequiredColor => ConsoleColor.DarkRed;
}

/// <summary>Bright-on-dark theme tuned for dark terminal backgrounds.</summary>
public class DarkConsoleTheme : IConsoleTheme
{
    /// <summary>Shared instance; themes are stateless.</summary>
    public static DarkConsoleTheme Instance { get; } = new DarkConsoleTheme();

    /// <summary>Section headers.</summary>
    public ConsoleColor HeaderColor => ConsoleColor.Magenta;

    /// <summary>Command names and option flags.</summary>
    public ConsoleColor FlagColor => ConsoleColor.Green;

    /// <summary>Value placeholders.</summary>
    public ConsoleColor ParameterColor => ConsoleColor.Cyan;

    /// <summary>Descriptions and main text.</summary>
    public ConsoleColor DescriptionColor => ConsoleColor.White;

    /// <summary>Default values and secondary info.</summary>
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;

    /// <summary>Required markers and warnings.</summary>
    public ConsoleColor RequiredColor => ConsoleColor.Red;
}

/// <summary>Dark-on-light theme tuned for light terminal backgrounds.</summary>
public class LightConsoleTheme : IConsoleTheme
{
    /// <summary>Shared instance; themes are stateless.</summary>
    public static LightConsoleTheme Instance { get; } = new LightConsoleTheme();

    /// <summary>Section headers.</summary>
    public ConsoleColor HeaderColor => ConsoleColor.DarkBlue;

    /// <summary>Command names and option flags.</summary>
    public ConsoleColor FlagColor => ConsoleColor.DarkGreen;

    /// <summary>Value placeholders.</summary>
    public ConsoleColor ParameterColor => ConsoleColor.DarkCyan;

    /// <summary>Descriptions and main text.</summary>
    public ConsoleColor DescriptionColor => ConsoleColor.Black;

    /// <summary>Default values and secondary info.</summary>
    public ConsoleColor SecondaryColor => ConsoleColor.DarkGray;

    /// <summary>Required markers and warnings.</summary>
    public ConsoleColor RequiredColor => ConsoleColor.DarkRed;
}
