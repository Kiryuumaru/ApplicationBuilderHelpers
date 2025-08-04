using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Text.RegularExpressions;

namespace ApplicationBuilderHelpers.Common;

internal static class AnsiThemeValidator
{
    // ANSI escape sequence patterns
    private static readonly Regex AnsiEscapePattern = new(
        @"^(\x1b\[[\d;]*[mK])*$|^$", 
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    // More specific patterns for common ANSI sequences
    private static readonly Regex SpecificAnsiPattern = new(
        @"^(\x1b\[(0|1|2|3|4|5|6|7|8|9|\d{1,3})(;\d{1,3})*[mK])*$|^$",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static bool IsValid(IAnsiTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        return 
            IsValidAnsiSequence(theme.HeaderColor) &&
            IsValidAnsiSequence(theme.FlagColor) &&
            IsValidAnsiSequence(theme.ParameterColor) &&
            IsValidAnsiSequence(theme.DescriptionColor) &&
            IsValidAnsiSequence(theme.SecondaryColor) &&
            IsValidAnsiSequence(theme.RequiredColor) &&
            IsValidAnsiSequence(theme.Reset);
    }

    public static void ValidateAndThrow(IAnsiTheme theme)
    {
        ArgumentNullException.ThrowIfNull(theme);

        ValidatePropertyAndThrow(theme.HeaderColor, nameof(IAnsiTheme.HeaderColor));
        ValidatePropertyAndThrow(theme.FlagColor, nameof(IAnsiTheme.FlagColor));
        ValidatePropertyAndThrow(theme.ParameterColor, nameof(IAnsiTheme.ParameterColor));
        ValidatePropertyAndThrow(theme.DescriptionColor, nameof(IAnsiTheme.DescriptionColor));
        ValidatePropertyAndThrow(theme.SecondaryColor, nameof(IAnsiTheme.SecondaryColor));
        ValidatePropertyAndThrow(theme.RequiredColor, nameof(IAnsiTheme.RequiredColor));
        ValidatePropertyAndThrow(theme.Reset, nameof(IAnsiTheme.Reset));
    }

    public static bool IsValidAnsiSequence(string? ansiSequence)
    {
        if (ansiSequence == null)
            return false;

        // Empty string is valid (no color)
        if (string.IsNullOrEmpty(ansiSequence))
            return true;

        // Check for basic ANSI escape pattern
        if (!AnsiEscapePattern.IsMatch(ansiSequence))
            return false;

        // Additional validation for well-formed sequences
        return SpecificAnsiPattern.IsMatch(ansiSequence);
    }

    private static void ValidatePropertyAndThrow(string? value, string propertyName)
    {
        if (!IsValidAnsiSequence(value))
        {
            throw new ArgumentException($"The {propertyName} property contains an invalid ANSI escape sequence: '{value}'. " +
                                      "ANSI sequences must follow the format \\x1b[<parameters>m or \\x1b[<parameters>K, or be empty string.");
        }
    }
}