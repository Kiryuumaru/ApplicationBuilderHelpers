using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

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
        if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h"))
        {
            return true;
        }

        // Never show global help for empty args - let ParseCommandLine handle it
        // This allows root commands to execute normally or show subcommand requirements
        return false;
    }

    /// <summary>
    /// Legacy contains-anywhere version check. Obsolete: version is resolved
    /// post-parse from leftover unconsumed tokens via <see cref="IsVersionToken"/>,
    /// so consumed option values never trigger version output.
    /// </summary>
    [Obsolete("Use IsVersionToken on leftover unconsumed tokens instead.")]
    internal static bool ShouldShowVersion(string[] args)
    {
        return args.Any(IsVersionToken);
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
    /// Shows a styled error message with helpful footer information
    /// </summary>
    internal void ShowErrorMessage(string message)
    {
        var theme = commandBuilder.Theme;
        // Use auto-detection for null ExecutableName
        var executableName = commandBuilder.ExecutableName ?? AssemblyHelpers.GetAutoDetectedExecutableName();

        // Show the error message in red color if theme is available
        var errorColor = theme?.RequiredColor ?? ConsoleColor.Red;
        consoleOutput.WriteLineError($"Error: {message}", errorColor);

        // Add helpful footer message based on error type
        consoleOutput.WriteLineError();

        if (message.Contains("requires a subcommand"))
        {
            // Extract command name if present for more specific help
            if (message.StartsWith('\'') && message.Contains('\''))
            {
                var commandName = message[1..message.IndexOf('\'', 1)];
                if (!string.IsNullOrEmpty(commandName))
                {
                    consoleOutput.WriteLineError($"Run '{executableName} {commandName} --help' to see available subcommands and options.");
                }
                else
                {
                    consoleOutput.WriteLineError($"Run '{executableName} --help' to see available commands and options.");
                }
            }
            else
            {
                consoleOutput.WriteLineError($"Run '{executableName} --help' to see available commands and options.");
            }
        }
        else if (message.Contains("Unknown option") || message.Contains("Missing required"))
        {
            consoleOutput.WriteLineError($"Run '{executableName} <command> --help' for more information on specific command options.");
        }
        else
        {
            consoleOutput.WriteLineError($"Run '{executableName} --help' for more information on available commands and options.");
        }
    }
}
