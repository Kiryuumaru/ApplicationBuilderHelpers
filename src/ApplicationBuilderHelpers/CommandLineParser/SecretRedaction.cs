using System;
using System.Collections;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Central secret redaction for help text and error messages.
/// </summary>
/// <remarks>
/// Secret values are never echoed.
/// </remarks>
internal static class SecretRedaction
{
    /// <summary>
    /// Mask shown in place of a secret value.
    /// </summary>
    public const string Mask = "[REDACTED]";

    /// <summary>
    /// Whether a default value should be masked.
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
    /// Display text for a default value.
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
    /// <remarks>
    /// Secret values are never echoed.
    /// </remarks>
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
    /// <remarks>
    /// Secret values are never echoed.
    /// </remarks>
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
    /// Error message for a value that fails type conversion.
    /// </summary>
    /// <remarks>
    /// Secret values are never echoed.
    /// </remarks>
    public static string InvalidFormatMessage(string providedValue, string targetTypeName, bool isSecret)
    {
        if (isSecret)
            return $"Invalid format for provided value of type {targetTypeName}";

        return $"Invalid format for value '{providedValue}' of type {targetTypeName}";
    }

    /// <summary>
    /// Error message for an argument value that fails type conversion or parsing.
    /// </summary>
    /// <remarks>
    /// Secret values are never echoed.
    /// </remarks>
    public static string InvalidArgumentFormatMessage(string providedValue, string displayName, string targetTypeName, bool isSecret)
    {
        if (isSecret)
            return $"Cannot convert provided value to {targetTypeName} for argument '{displayName}'";

        return $"Cannot convert '{providedValue}' to {targetTypeName} for argument '{displayName}'";
    }

    /// <summary>
    /// Error message for an invalid boolean flag literal.
    /// </summary>
    /// <remarks>
    /// Secret values are never echoed.
    /// </remarks>
    public static string InvalidFlagLiteralMessage(string providedLiteral, string displayName, bool isSecret)
    {
        const string expected = "Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'";

        if (isSecret)
            return $"Invalid Boolean value provided for option '{displayName}'. {expected}";

        return $"Invalid Boolean value '{providedLiteral}' for option '{displayName}'. {expected}";
    }

    /// <summary>
    /// Error message for a --no-&lt;name&gt;=value occurrence.
    /// </summary>
    /// <remarks>
    /// Secret values are never echoed.
    /// </remarks>
    public static string NoValueAcceptedMessage(string optionName, string rejectedValue, bool isSecret, bool isFlag, string? positiveLongName = null)
    {
        if (isFlag)
        {
            if (isSecret)
            {
                return $"Option '{optionName}' does not accept a value. Use bare '{optionName}' to set the flag to 'false'.";
            }

            return $"Option '{optionName}' does not accept a value '{rejectedValue}'. Use bare '{optionName}' to set the flag to 'false'.";
        }

        var positive = positiveLongName != null ? $" or use '--{positiveLongName}=<value>'" : string.Empty;
        if (isSecret)
        {
            return $"Option '{optionName}' does not accept a value. Negation applies to boolean flags only; omit '{optionName}'{positive}.";
        }

        return $"Option '{optionName}' does not accept a value '{rejectedValue}'. Negation applies to boolean flags only; omit '{optionName}'{positive}.";
    }

    /// <summary>
    /// Error message for an unknown char in a combined short cluster.
    /// </summary>
    /// <remarks>
    /// Secret values are never echoed.
    /// </remarks>
    public static string UnknownClusterCharMessage(string token, int failingIndex) =>
        $"Unknown option: -{token[failingIndex]}";

    /// <summary>
    /// Redacts the provided value out of a type-parser error string when secret.
    /// </summary>
    /// <remarks>
    /// Secret values are never echoed.
    /// </remarks>
    public static string? RedactParserError(string? parserError, string? providedValue, bool isSecret)
    {
        if (!isSecret || parserError == null)
            return parserError;

        if (!string.IsNullOrEmpty(providedValue))
        {
            var redacted = parserError.Replace($"'{providedValue}'", Mask, StringComparison.Ordinal);
            return ReplaceQuotedCaseInsensitive(redacted, providedValue);
        }

        return parserError;
    }

    /// <summary>
    /// Redacts remaining case variants of the provided value.
    /// </summary>
    private static string ReplaceQuotedCaseInsensitive(string text, string providedValue)
    {
        var result = text;
        var quotedLength = providedValue.Length + 2;
        var start = 0;
        while (true)
        {
            var found = result.IndexOf($"'{providedValue}'", start, StringComparison.OrdinalIgnoreCase);
            if (found < 0)
                return result;
            result = result[..found] + Mask + result[(found + quotedLength)..];
            start = found + Mask.Length;
        }
    }
}
