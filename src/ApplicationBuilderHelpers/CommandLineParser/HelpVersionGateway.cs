using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Help/version pre-parse gate.
/// </summary>
internal sealed class HelpVersionGateway(
    ICommandBuilder commandBuilder,
    ConsoleOutput consoleOutput)
{
    /// <summary>Bare global-help request (exactly one help token).</summary>
    internal static bool ShouldShowGlobalHelp(string[] args)
    {
        if (args.Length == 1 && IsHelpToken(args[0]))
        {
            return true;
        }

        return false;
    }

    /// <summary>Bare help token (<c>--help</c>, <c>-h</c>, <c>-?</c>, or <c>/?</c> only).</summary>
    internal static bool IsHelpToken(string token)
    {
        return token == "--help" || token == "-h" || token == "-?" || token == "/?";
    }

    /// <summary>Bare version token (<c>--version</c> or <c>-V</c> only).</summary>
    internal static bool IsVersionToken(string token)
    {
        return token == "--version" || token == "-V";
    }

    /// <summary>Help requested (bare token or <c>h</c>-cluster, pre-<c>--</c> only).</summary>
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

    /// <summary>Version requested (bare token or <c>V</c>-cluster, pre-<c>--</c> only).</summary>
    internal static bool RequestedVersion(string[] args)
    {
        foreach (var token in args)
        {
            if (token == "--")
                return false;
            if (IsVersionToken(token))
                return true;
            if (IsVersionCluster(token))
                return true;
        }

        return false;
    }

    /// <summary>Decides symmetric validation forgiveness (help &gt; version, pre-<c>--</c> only).</summary>
    internal enum HelpVersionForgiveness
    {
        NoForgive,
        ForgiveHelp,
        ForgiveVersion,
    }

    /// <summary>Decides whether validation is forgiven for help vs version from parse flags plus the raw tail.</summary>
    internal static HelpVersionForgiveness DecideValidationForgiveness(ParseResult result, string[] tail)
    {
        var helpRequested = RequestedHelp(tail);
        if (result.ShowHelp && helpRequested)
            return HelpVersionForgiveness.ForgiveHelp;
        if (!result.ShowHelp && result.ShowVersion && RequestedVersion(tail) && !helpRequested)
            return HelpVersionForgiveness.ForgiveVersion;
        return HelpVersionForgiveness.NoForgive;
    }

    private static bool IsVersionCluster(string token)
    {
        if (token.Length <= 2 || !token.StartsWith('-') || token.StartsWith("--", StringComparison.Ordinal) || token.Contains('='))
            return false;
        if (char.IsDigit(token[1]) || token[1] == '.')
            return false;

        return token[1..].Contains('V');
    }

    private static bool IsHelpCluster(string token)
    {
        if (token.Length <= 2 || !token.StartsWith('-') || token.StartsWith("--", StringComparison.Ordinal) || token.Contains('='))
            return false;
        if (char.IsDigit(token[1]) || token[1] == '.')
            return false;

        return token[1..].Contains('h');
    }

    /// <summary>Renders global help for the whole application.</summary>
    internal void ShowGlobalHelp(SubCommandInfo? rootCommand, Dictionary<string, SubCommandInfo> allCommands)
    {
        var helpFormatter = new HelpFormatter(commandBuilder, rootCommand, allCommands, consoleOutput);
        helpFormatter.ShowGlobalHelp();
    }

    /// <summary>Renders help for a single command.</summary>
    internal void ShowCommandHelp(SubCommandInfo commandInfo, SubCommandInfo? rootCommand, Dictionary<string, SubCommandInfo> allCommands)
    {
        var helpFormatter = new HelpFormatter(commandBuilder, rootCommand, allCommands, consoleOutput);
        helpFormatter.ShowCommandHelp(commandInfo);
    }

    /// <summary>Prints the configured executable version to stdout.</summary>
    internal void ShowVersion()
    {
        var version = commandBuilder.ExecutableVersion ?? AssemblyHelpers.GetAutoDetectedVersion();
        consoleOutput.WriteLine(version);
    }

    /// <summary>Shows a styled error message with footer information.</summary>
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
