using System;
using System.Collections;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Central helper for secret redaction in help text and error messages.
/// Secret values never echo the provided value; help keeps the Default: label
/// with a masked value, and errors keep the option/argument name plus the
/// valid-values list. Exit codes are unchanged by redaction.
/// </summary>
internal static class SecretRedaction
{
    /// <summary>
    /// Mask shown in place of a secret value.
    /// </summary>
    public const string Mask = "[REDACTED]";

    /// <summary>
    /// Whether a default value should be masked (secret and non-empty).
    /// </summary>
    public static bool ShouldMaskDefault(object? value, bool isSecret)
    {
        if (!isSecret)
            return false;

        return value switch
        {
            null => false,
            string s => s.Length != 0,
            Array arr => arr.Length != 0,
            ICollection collection => collection.Count != 0,
            _ => true
        };
    }

    /// <summary>
    /// Display text for a default value: the mask when secret and non-empty, otherwise the raw value.
    /// </summary>
    public static string? GetDefaultDisplay(object? value, bool isSecret)
    {
        if (ShouldMaskDefault(value, isSecret))
            return Mask;

        return value?.ToString();
    }

    /// <summary>
    /// Error message for an option value rejected by the valid-values list.
    /// </summary>
    public static string InvalidOptionValueMessage(string providedValue, string displayName, string validValuesString, bool isSecret)
    {
        if (isSecret)
        {
            return $"Value provided for option '{displayName}' is not valid. " +
                $"Must be one of: {validValuesString}";
        }

        return $"Value '{providedValue}' is not valid for option '{displayName}'. " +
            $"Must be one of: {validValuesString}";
    }

    /// <summary>
    /// Error message for an argument value rejected by the valid-values list.
    /// </summary>
    public static string InvalidArgumentValueMessage(string providedValue, string displayName, string validValuesString, bool isSecret)
    {
        if (isSecret)
        {
            return $"Value provided for argument '{displayName}' is not valid. " +
                $"Must be one of: {validValuesString}";
        }

        return $"Value '{providedValue}' is not valid for argument '{displayName}'. " +
            $"Must be one of: {validValuesString}";
    }

    /// <summary>
    /// Error message when the binder cannot create the backing array itself
    /// (type-level failure, before any per-value conversion runs). Carries the
    /// element type and count, never the provided value.
    /// </summary>
    public static string ArrayCreationMessage(string elementTypeName, int length)
    {
        return $"Cannot create array of {elementTypeName} with length {length}";
    }

    /// <summary>
    /// Error message for a value that fails type conversion.
    /// </summary>
    public static string InvalidFormatMessage(string providedValue, string targetTypeName, bool isSecret)
    {
        if (isSecret)
            return $"Invalid format for provided value of type {targetTypeName}";

        return $"Invalid format for value '{providedValue}' of type {targetTypeName}";
    }

    /// <summary>
    /// Error message for an argument value that fails type conversion or parsing.
    /// </summary>
    public static string InvalidArgumentFormatMessage(string providedValue, string displayName, string targetTypeName, bool isSecret)
    {
        if (isSecret)
            return $"Cannot convert provided value to {targetTypeName} for argument '{displayName}'";

        return $"Cannot convert '{providedValue}' to {targetTypeName} for argument '{displayName}'";
    }

    /// <summary>
    /// Error message for an invalid boolean flag literal.
    /// </summary>
    public static string InvalidFlagLiteralMessage(string providedLiteral, string displayName, bool isSecret)
    {
        const string expected = "Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'";

        if (isSecret)
            return $"Invalid Boolean value provided for option '{displayName}'. {expected}";

        return $"Invalid Boolean value '{providedLiteral}' for option '{displayName}'. {expected}";
    }

    /// <summary>
    /// Error message for a --no-&lt;name&gt;=value occurrence, which never accepts a value.
    /// </summary>
    public static string NoValueAcceptedMessage(string optionName, string rejectedValue, bool isSecret)
    {
        if (isSecret)
        {
            return $"Option '{optionName}' does not accept a value. Use bare '{optionName}' to set the flag to 'false'.";
        }

        return $"Option '{optionName}' does not accept a value '{rejectedValue}'. Use bare '{optionName}' to set the flag to 'false'.";
    }

    /// <summary>
    /// Redacts the provided value out of a type-parser error string when secret.
    /// Non-secret errors pass through unchanged.
    /// </summary>
    public static string? RedactParserError(string? parserError, string? providedValue, bool isSecret)
    {
        if (!isSecret || parserError == null)
            return parserError;

        if (!string.IsNullOrEmpty(providedValue))
            return parserError.Replace($"'{providedValue}'", Mask, StringComparison.Ordinal);

        return parserError;
    }
}
