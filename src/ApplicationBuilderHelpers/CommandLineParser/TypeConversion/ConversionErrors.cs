using ApplicationBuilderHelpers.Exceptions;

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
    /// </summary>
    /// <param name="raw">The raw CLI text that failed conversion.</param>
    /// <param name="displayName">Display name including kind prefix, e.g. <c>"option '--mode'"</c> or <c>"argument 'level'"</c>.</param>
    /// <param name="reason">Underlying parser error or conversion detail.</param>
    internal static CommandException InvalidValue(string? raw, string displayName, string? reason)
    {
        string message = string.IsNullOrWhiteSpace(reason)
            ? $"Invalid value '{raw}' for {displayName}"
            : $"Invalid value '{raw}' for {displayName}: {reason}";
        return new CommandException(message, 2, CommandErrorKind.InvalidValue);
    }

    /// <summary>
    /// Creates a not-among-allowed-values error, preserving the
    /// <c>"Value '{raw}' is not valid for {displayName}. Must be one of: {allowedDisplay}"</c>
    /// shape shared by the option and argument conversion paths.
    /// </summary>
    /// <param name="raw">The raw CLI text that was rejected.</param>
    /// <param name="displayName">Display name including kind prefix, e.g. <c>"option '--mode'"</c> or <c>"argument 'level'"</c>.</param>
    /// <param name="allowedDisplay">Pre-joined allowed-values display text, e.g. <c>"json, xml"</c>.</param>
    internal static CommandException NotAmong(string? raw, string displayName, string allowedDisplay)
    {
        return new CommandException(
            $"Value '{raw}' is not valid for {displayName}. Must be one of: {allowedDisplay}",
            2,
            CommandErrorKind.InvalidValue);
    }
}
