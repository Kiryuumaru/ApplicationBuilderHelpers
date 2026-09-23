using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Help/version pre-parse step that calls HelpFormatter and ConsoleOutput.
/// </summary>
internal sealed class HelpVersionGateway(
    ICommandBuilder commandBuilder,
    ConsoleOutput consoleOutput)
{
    internal static bool ShouldShowGlobalHelp(string[] args)
    {
        if (args.Length == 1 && IsHelpToken(args[0]))
        {
            return true;
        }

        return false;
    }

    internal static bool IsHelpToken(string token)
    {
        return token == "--help" || token == "-h";
    }

    internal static bool IsVersionToken(string token)
    {
        return token == "--version" || token == "-V";
    }

    /// <summary>
    /// Matches the parser's help-detection surface (footer signal):
    /// <see cref="IsHelpToken"/> per-token plus an <c>h</c> char inside a
    /// dash-led cluster (the dash-led cluster rule), scanning only up to the first bare
    /// <c>--</c> separator (tokens after it are positional per
    /// the global-help rule and never set <c>ShowHelp</c>).
    /// Footer-only: exit codes are unaffected.
    /// </summary>
    internal static bool RequestedHelp(string[] args)
    {
        foreach (var token in args)
        {
            if (token == "--")
                return false;
            if (IsHelpToken(token))
                return true;
            if (IsHelpCluster(token))
                return true;
        }

        return false;
    }

    private static bool IsHelpCluster(string token)
    {
        if (token.Length <= 2 || !token.StartsWith('-') || token.StartsWith("--", StringComparison.Ordinal) || token.Contains('='))
            return false;
        if (char.IsDigit(token[1]) || token[1] == '.')
            return false;

        return token[1..].Contains('h');
    }

    internal void ShowGlobalHelp(SubCommandInfo? rootCommand, Dictionary<string, SubCommandInfo> allCommands)
    {
        var helpFormatter = new HelpFormatter(commandBuilder, rootCommand, allCommands, consoleOutput);
        helpFormatter.ShowGlobalHelp();
    }

    internal void ShowCommandHelp(SubCommandInfo commandInfo, SubCommandInfo? rootCommand, Dictionary<string, SubCommandInfo> allCommands)
    {
        var helpFormatter = new HelpFormatter(commandBuilder, rootCommand, allCommands, consoleOutput);
        helpFormatter.ShowCommandHelp(commandInfo);
    }

    internal void ShowVersion()
    {
        var version = commandBuilder.ExecutableVersion ?? AssemblyHelpers.GetAutoDetectedVersion();
        consoleOutput.WriteLine(version);
    }

    /// <summary>
    /// Shows a styled error message with footer information.
    /// Footer selection branches on <see cref="CommandErrorKind"/>, never on message text.
    /// The circular <c>--help</c> hint is suppressed when the failing invocation
    /// already requested help: only the <c>--version</c> hint survives.
    /// </summary>
    internal void ShowErrorMessage(string message, CommandErrorKind kind = CommandErrorKind.Fault, string? commandName = null, bool showHelpRequested = false)
    {
        var theme = commandBuilder.Theme;
        var executableName = commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();

        var errorColor = theme?.RequiredColor ?? ConsoleColor.Red;
        consoleOutput.WriteLineError($"Error: {message}", errorColor);

        consoleOutput.WriteLineError();

        consoleOutput.WriteLineError(CommandErrorFooter.Resolve(kind, executableName, commandName, showHelpRequested));
    }
}
