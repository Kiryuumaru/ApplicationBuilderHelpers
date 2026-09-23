using ApplicationBuilderHelpers.Exceptions;
using System;

namespace ApplicationBuilderHelpers.CommandLineParser.TypeConversion;

/// <summary>
/// Factory for type-conversion <see cref="CommandException"/> errors.
/// Centralizes the message text shared by the option and argument conversion
/// paths so options
/// and arguments share one shape:
/// exit code 2 with <see cref="CommandErrorKind.InvalidValue"/>.
/// </summary>
internal static class ConversionErrors
{
    /// <summary>
    /// Creates an invalid-value error, with the
    /// <c>"Invalid value '{raw}' for {displayName}: {reason}"</c> shape
    /// (argument conversion parser-error template). Option parser
    /// errors are passed through as <paramref name="reason"/>, so their
    /// <c>"Invalid {Type} value: ..."</c> text stays contained in the message,
    /// and <c>ChangeType</c> fallback text
    /// (<c>"Invalid format for value ..."</c>) is passed as <paramref name="reason"/>
    /// by the caller to avoid losing substrings.
    /// When <paramref name="isSecret"/> is true, the raw value is masked as
    /// <c>[REDACTED]</c> and the parser reason is redacted through
    /// <see cref="SecretRedaction.RedactParserError"/> so secret values never echo.
    /// </summary>
    /// <param name="raw">The raw CLI text that failed conversion.</param>
    /// <param name="displayName">Display name including kind prefix, e.g. <c>"option '--mode'"</c> or <c>"argument 'level'"</c>.</param>
    /// <param name="reason">Underlying parser error or conversion detail.</param>
    /// <param name="isSecret">Whether the target option/argument is secret.</param>
    /// <param name="targetTypeName">Target type short name used by the secret InvalidFormat shape.</param>
    internal static CommandException InvalidValue(string? raw, string displayName, string? reason, bool isSecret = false, string? targetTypeName = null)
    {
        if (isSecret)
        {
            string? redactedReason = SecretRedaction.RedactParserError(reason, raw, true);
            if (!string.IsNullOrWhiteSpace(redactedReason))
            {
                bool reasonStillEchoesValue = raw is not null
                    && raw.Length > 0
                    && redactedReason.Contains(raw, StringComparison.Ordinal);
                if (reasonStillEchoesValue)
                {
                    redactedReason = SecretRedaction.Mask;
                }

                return new CommandException(
                    $"Invalid value {SecretRedaction.Mask} for {displayName}: {redactedReason}",
                    2,
                    CommandErrorKind.InvalidValue);
            }

            const string argumentPrefix = "argument '";
            if (displayName.StartsWith(argumentPrefix, StringComparison.Ordinal)
                && displayName.EndsWith("'", StringComparison.Ordinal)
                && !string.IsNullOrWhiteSpace(targetTypeName))
            {
                string bareName = displayName.Substring(
                    argumentPrefix.Length, displayName.Length - argumentPrefix.Length - 1);
                return new CommandException(
                    SecretRedaction.InvalidArgumentFormatMessage(raw ?? string.Empty, bareName, targetTypeName, true),
                    2,
                    CommandErrorKind.InvalidValue);
            }

            string detail = targetTypeName ?? string.Empty;
            string secretMessage = string.IsNullOrWhiteSpace(detail)
                ? $"Invalid value {SecretRedaction.Mask} for {displayName}"
                : $"Invalid value {SecretRedaction.Mask} for {displayName}: Invalid format for provided value of type {detail}";
            return new CommandException(secretMessage, 2, CommandErrorKind.InvalidValue);
        }

        string message = string.IsNullOrWhiteSpace(reason)
            ? $"Invalid value '{raw}' for {displayName}"
            : $"Invalid value '{raw}' for {displayName}: {reason}";
        return new CommandException(message, 2, CommandErrorKind.InvalidValue);
    }

    /// <summary>
    /// Creates a not-among-allowed-values error.
    /// When <paramref name="isSecret"/> is true, the provided value is omitted
    /// while keeping the valid-values list. The <paramref name="isArgument"/>
    /// flag selects the argument template, otherwise the option template.
    /// </summary>
    /// <param name="raw">The raw CLI text that was rejected.</param>
    /// <param name="displayName">Display name including kind prefix, e.g. <c>"option '--mode'"</c> or <c>"argument 'level'"</c>.</param>
    /// <param name="allowedDisplay">Pre-joined allowed-values display text, e.g. <c>"json, xml"</c>.</param>
    /// <param name="isSecret">Whether the target option/argument is secret.</param>
    /// <param name="isArgument"><c>true</c> for the argument template, <c>false</c> for the option template.</param>
    internal static CommandException NotAmong(string? raw, string displayName, string allowedDisplay, bool isSecret = false, bool isArgument = false)
    {
        if (isSecret)
        {
            string bareName = displayName;
            const string optionPrefix = "option '--";
            const string argumentPrefix = "argument '";
            if (bareName.StartsWith(optionPrefix, StringComparison.Ordinal) && bareName.EndsWith("'", StringComparison.Ordinal))
            {
                bareName = "--" + bareName.Substring(optionPrefix.Length, bareName.Length - optionPrefix.Length - 1);
            }
            else if (bareName.StartsWith(argumentPrefix, StringComparison.Ordinal) && bareName.EndsWith("'", StringComparison.Ordinal))
            {
                bareName = bareName.Substring(argumentPrefix.Length, bareName.Length - argumentPrefix.Length - 1);
            }

            string message = isArgument
                ? SecretRedaction.InvalidArgumentValueMessage(raw ?? string.Empty, bareName, allowedDisplay, true)
                : SecretRedaction.InvalidOptionValueMessage(raw ?? string.Empty, bareName, allowedDisplay, true);
            return new CommandException(message, 2, CommandErrorKind.InvalidValue);
        }

        return new CommandException(
            $"Value '{raw}' is not valid for {displayName}. Must be one of: {allowedDisplay}",
            2,
            CommandErrorKind.InvalidValue);
    }

    /// <summary>
    /// Creates a collection-materialization error for the <see cref="CollectionShape"/>
    /// array path: the element parser is missing or its typed-array factory failed,
    /// and no exactly-typed fallback exists. Never returns a wrong-typed array
    /// (e.g. <c>object[]</c> for an <c>int[]</c> property, which would throw
    /// <see cref="ArgumentException"/> at the bind site). Shape: exit code 2
    /// with <see cref="CommandErrorKind.InvalidValue"/>.
    /// </summary>
    /// <param name="propertyType">The collection property type being materialized.</param>
    /// <param name="elementType">The resolved element type.</param>
    /// <param name="detail">Why materialization failed (missing parser or factory error).</param>
    /// <param name="displayName">Display name including kind prefix, e.g. <c>"option '--scores'"</c>; included in the message so the error names the failing flag.</param>
    /// <param name="isSecret">Whether the target option/argument is secret. Collection-materialization details carry no raw values (type FullNames only), except a factory-failure tail already masked by the caller, so the message shape is identical; the flag is accepted for parity with <c>InvalidValue</c>/<c>NotAmong</c> and future-proofing.</param>
    internal static CommandException CollectionMaterialization(Type propertyType, Type elementType, string detail, string? displayName = null, bool isSecret = false)
    {
        ArgumentNullException.ThrowIfNull(propertyType);
        ArgumentNullException.ThrowIfNull(elementType);
        string target = string.IsNullOrWhiteSpace(displayName)
            ? $"collection of type '{propertyType.FullName}' with element type '{elementType.FullName}'"
            : $"{displayName} (collection of type '{propertyType.FullName}' with element type '{elementType.FullName}')";
        return new CommandException(
            $"Cannot bind {target}: {detail}",
            2,
            CommandErrorKind.InvalidValue);
    }
}
