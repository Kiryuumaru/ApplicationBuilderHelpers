namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Resolves the shared CLI error footer. Both the command-line handling
/// path and the host path render this footer so the same <see cref="CommandErrorKind"/>
/// always prints the same footer regardless of raising path.
/// Pure string resolver: no <c>ICommandBuilder</c>, theme, or console knowledge.
/// </summary>
internal static class CommandErrorFooter
{
    internal static string Resolve(CommandErrorKind kind, string executableName, string? commandName)
    {
        return Resolve(kind, executableName, commandName, showHelpRequested: false);
    }

    /// <summary>
    /// Resolves the footer. When <paramref name="showHelpRequested"/> is true,
    /// omits the <c>--help</c> hint and keeps only the <c>--version</c> hint;
    /// otherwise keeps both hints.
    /// </summary>
    internal static string Resolve(CommandErrorKind kind, string executableName, string? commandName, bool showHelpRequested)
    {
        var helpHint = HelpHint(executableName, commandName);
        var versionHint = VersionHint(executableName, commandName);
        switch (kind)
        {
            case CommandErrorKind.RequiresSubcommand:
                if (!string.IsNullOrEmpty(commandName))
                {
                    var help = showHelpRequested ? versionHint : $"{helpHint} to see available subcommands and options. {versionHint}";
                    return help;
                }
                else
                {
                    var globalSub = showHelpRequested ? versionHint : $"{helpHint} to see available commands and options. {versionHint}";
                    return globalSub;
                }
            case CommandErrorKind.UnknownOption:
            case CommandErrorKind.MissingRequired:
            case CommandErrorKind.UnknownCommand:
            case CommandErrorKind.InvalidValue:
            case CommandErrorKind.DuplicateOption:
                if (!string.IsNullOrEmpty(commandName))
                {
                    if (showHelpRequested)
                        return versionHint;
                    return $"{helpHint} for more information on specific command options. {versionHint}";
                }
                else
                {
                    if (showHelpRequested)
                        return versionHint;
                    return $"{helpHint} for more information on available commands and options. {versionHint}";
                }
            default:
                return $"{HelpHint(executableName, commandName: null)} for more information on available commands and options.";
        }
    }

    private static string HelpHint(string executableName, string? commandName) =>
        !string.IsNullOrEmpty(commandName)
            ? $"Run '{executableName} {commandName} --help'"
            : $"Run '{executableName} --help'";

    private static string VersionHint(string executableName, string? commandName) =>
        !string.IsNullOrEmpty(commandName)
            ? $"Run '{executableName} {commandName} --version' to show version information."
            : $"Run '{executableName} --version' to show version information.";
}
