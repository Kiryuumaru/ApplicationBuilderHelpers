using ApplicationBuilderHelpers.Exceptions;
using System;

namespace ApplicationBuilderHelpers.CommandLineParser.TypeConversion;

internal static class ConversionErrors
{
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
        else
        {
            return new CommandException(
                $"Value '{raw}' is not valid for {displayName}. Must be one of: {allowedDisplay}",
                2,
                CommandErrorKind.InvalidValue);
        }
    }

    /// <summary>Missing parser or failed typed factory; exit 2 InvalidValue.</summary>
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
