namespace ApplicationBuilderHelpers.Exceptions;

/// <summary>
/// Single owner for the shared CLI error footer. Both the command-line gateway
/// path and the host path render this footer so the same <see cref="CommandErrorKind"/>
/// always prints the same footer regardless of raising path.
/// Pure string resolver: no <c>ICommandBuilder</c>, theme, or console knowledge.
/// </summary>
internal static class CommandErrorFooter
{
    internal static string Resolve(CommandErrorKind kind, string executableName, string? commandName)
    {
        switch (kind)
        {
            case CommandErrorKind.RequiresSubcommand:
                if (!string.IsNullOrEmpty(commandName))
                {
                    return $"Run '{executableName} {commandName} --help' to see available subcommands and options. Run '{executableName} {commandName} --version' to show version information.";
                }
                else
                {
                    return $"Run '{executableName} --help' to see available commands and options. Run '{executableName} --version' to show version information.";
                }
            case CommandErrorKind.UnknownOption:
            case CommandErrorKind.MissingRequired:
            case CommandErrorKind.UnknownCommand:
            case CommandErrorKind.InvalidValue:
            case CommandErrorKind.DuplicateOption:
                if (!string.IsNullOrEmpty(commandName))
                {
                    return $"Run '{executableName} {commandName} --help' for more information on specific command options. Run '{executableName} {commandName} --version' to show version information.";
                }
                else
                {
                    return $"Run '{executableName} --help' for more information on available commands and options. Run '{executableName} --version' to show version information.";
                }
            default:
                return $"Run '{executableName} --help' for more information on available commands and options.";
        }
    }
}
