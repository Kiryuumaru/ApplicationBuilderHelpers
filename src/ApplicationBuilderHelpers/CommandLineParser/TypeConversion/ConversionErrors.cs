using ApplicationBuilderHelpers.Exceptions;
using System;

namespace ApplicationBuilderHelpers.CommandLineParser.TypeConversion;

/// <summary>
/// Factory for type-conversion <see cref="CommandException"/> errors.
/// Centralizes the message text shared by the option and argument conversion
/// paths (previously thrown from per-type <c>ConvertValue</c> helpers) so options
/// and arguments share one shape:
/// exit code 2 with <see cref="CommandErrorKind.InvalidValue"/>.
/// </summary>
internal static class ConversionErrors
{
    /// <summary>
    /// Creates an invalid-value error, preserving the
    /// <c>"Invalid value '{raw}' for {displayName}: {reason}"</c> shape
    /// (argument conversion parser-error template). Option parser
    /// errors are passed through as <paramref name="reason"/>, so their
    /// <c>"Invalid {Type} value: ..."</c> text stays contained in the message,
    /// and <c>ChangeType</c> fallback text
    /// (<c>"Invalid format for value ..."</c>) is passed as <paramref name="reason"/>
    /// by the caller to avoid losing substrings.
    /// When <paramref name="isSecret"/> is true, the raw value is masked as
    /// <c>[REDACTED]</c> and the parser reason is redacted via
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
                    // Parser reason templates we cannot fully redact (e.g. ChangeType
                    // fallback text) must not leak the value: fall back to the mask.
                    redactedReason = SecretRedaction.Mask;
                }

                // Mask the echoed raw value, keep the (redacted) reason detail.
                return new CommandException(
                    $"Invalid value {SecretRedaction.Mask} for {displayName}: {redactedReason}",
                    2,
                    CommandErrorKind.InvalidValue);
            }

            // No parser detail to preserve (enum / null reason): use the upstream
            // InvalidArgumentFormat shape when the kind prefix marks an argument,
            // else the secret InvalidFormat shape for options.
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
    /// Creates a not-among-allowed-values error, preserving the
    /// <c>"Value '{raw}' is not valid for {displayName}. Must be one of: {allowedDisplay}"</c>
    /// shape shared by the option and argument conversion paths.
    /// When <paramref name="isSecret"/> is true, the provided value is omitted
    /// (upstream <c>SecretRedaction.InvalidOptionValueMessage</c> /
    /// <c>InvalidArgumentValueMessage</c> shapes) while keeping the
    /// valid-values list. The <paramref name="isArgument"/> flag selects the
    /// option vs. argument template. Non-secret calls keep the legacy shape.
    /// </summary>
    /// <param name="raw">The raw CLI text that was rejected.</param>
    /// <param name="displayName">Display name including kind prefix, e.g. <c>"option '--mode'"</c> or <c>"argument 'level'"</c>.</param>
    /// <param name="allowedDisplay">Pre-joined allowed-values display text, e.g. <c>"json, xml"</c>.</param>
    /// <param name="isSecret">Whether the target option/argument is secret.</param>
    /// <param name="isArgument">True for the argument template, false for the option template.</param>
    internal static CommandException NotAmong(string? raw, string displayName, string allowedDisplay, bool isSecret = false, bool isArgument = false)
    {
        if (isSecret)
        {
            // Strip the "option '--x'" / "argument 'x'" kind prefix back to the
            // bare name so the upstream secret templates stay byte-identical.
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
}
