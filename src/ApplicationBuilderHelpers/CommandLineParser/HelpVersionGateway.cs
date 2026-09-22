using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Help/version gateway delegating to the existing HelpFormatter and ConsoleOutput.
/// Moved verbatim from CommandLineParser (mechanical split, no behavior change).
/// </summary>
internal sealed class HelpVersionGateway(
    ICommandBuilder commandBuilder,
    ConsoleOutput consoleOutput)
{
    internal static bool ShouldShowGlobalHelp(string[] args)
    {
        // Only show global help if explicitly requested with --help/-h
        if (args.Length == 1 && IsHelpToken(args[0]))
        {
            return true;
        }

        // Never show global help for empty args - let ParseCommandLine handle it
        // This allows root commands to execute normally or show subcommand requirements
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
    /// Mirrors the parser's help-detection surface (#509 footer signal):
    /// <see cref="IsHelpToken"/> per-token plus an <c>h</c> char inside a
    /// dash-led cluster (the parser's cluster rule at
    /// <c>ArgumentParser.cs:328-332</c>), scanning only up to the first bare
    /// <c>--</c> separator (tokens after it are positional per
    /// <c>ArgumentParser.cs:113-123</c> and never set <c>ShowHelp</c>).
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
        // Same cluster gate as the parser (ArgumentParser.cs:287): bare
        // multi-char short bundles only — no '=', not '--' long form, not
        // numeric. The 'h' char wins as help even mid-cluster (:328-332).
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
    /// Shows a styled error message with helpful footer information.
    /// Footer selection dispatches on <see cref="CommandErrorKind"/>, never on message text.
    /// The circular <c>--help</c> hint is suppressed when the failing invocation
    /// already requested help (#509): only the <c>--version</c> hint survives.
    /// </summary>
    internal void ShowErrorMessage(string message, CommandErrorKind kind = CommandErrorKind.Fault, string? commandName = null, bool showHelpRequested = false)
    {
        var theme = commandBuilder.Theme;
        // Use auto-detection for null ExecutableName
        var executableName = commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();

        // Show the error message in red color if theme is available
        var errorColor = theme?.RequiredColor ?? ConsoleColor.Red;
        consoleOutput.WriteLineError($"Error: {message}", errorColor);

        // Add helpful footer message based on error kind
        consoleOutput.WriteLineError();

        consoleOutput.WriteLineError(CommandErrorFooter.Resolve(kind, executableName, commandName, showHelpRequested));
    }
}
