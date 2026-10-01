namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Renders the shared CLI error footer so every raising path prints the same hint per kind.
/// </summary>
internal static class CommandErrorFooter
{
    internal static string Resolve(CommandErrorKind kind, string executableName, string? commandName)
    {
        return Resolve(kind, executableName, commandName, showHelpRequested: false);
    }

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

    /// <summary>
    /// Version hint is global-only per ADR-0012; commandName is kept for signature compatibility and intentionally unused.
    /// </summary>
    private static string VersionHint(string executableName, string? commandName)
    {
        _ = commandName;
        return $"Run '{executableName} --version' to show version information.";
    }
}
