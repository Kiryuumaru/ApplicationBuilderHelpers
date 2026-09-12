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
    /// </summary>
    internal void ShowErrorMessage(string message, CommandErrorKind kind = CommandErrorKind.Fault, string? commandName = null)
    {
        var theme = commandBuilder.Theme;
        // Use auto-detection for null ExecutableName
        var executableName = commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();

        // Show the error message in red color if theme is available
        var errorColor = theme?.RequiredColor ?? ConsoleColor.Red;
        consoleOutput.WriteLineError($"Error: {message}", errorColor);

        // Add helpful footer message based on error kind
        consoleOutput.WriteLineError();

        switch (kind)
        {
            case CommandErrorKind.RequiresSubcommand:
                if (!string.IsNullOrEmpty(commandName))
                {
                    consoleOutput.WriteLineError($"Run '{executableName} {commandName} --help' to see available subcommands and options.");
                }
                else
                {
                    consoleOutput.WriteLineError($"Run '{executableName} --help' to see available commands and options.");
                }
                break;
            case CommandErrorKind.UnknownOption:
            case CommandErrorKind.MissingRequired:
                consoleOutput.WriteLineError($"Run '{executableName} <command> --help' for more information on specific command options.");
                break;
            default:
                consoleOutput.WriteLineError($"Run '{executableName} --help' for more information on available commands and options.");
                break;
        }
    }
}
