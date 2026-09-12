using ApplicationBuilderHelpers.Exceptions;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Parses command line arguments against the built hierarchy.
/// Moved verbatim from CommandLineParser (mechanical split, no behavior change).
/// </summary>
internal sealed class ArgumentParser
{
    /// <summary>
    /// Parses the command line arguments against the built hierarchy
    /// </summary>
    public ParseResult ParseCommandLine(SubCommandInfo rootCommand, string[] args)
    {
        var result = new ParseResult();
        var argIndex = 0;

        // Find the target command by consuming command parts
        result.TargetCommand = rootCommand!;

        while (argIndex < args.Length && !args[argIndex].StartsWith('-'))
        {
            var child = result.TargetCommand.FindChild(args[argIndex]);
            if (child != null)
            {
                result.TargetCommand = child;
                argIndex++;
            }
            else
            {
                break; // No more matching subcommands
            }
        }

        // Zero-match unknown command must error, not show help
        if (argIndex == 0 && args.Length > 0 && !args[0].StartsWith('-'))
        {
            throw new CommandException($"No command found for '{args[0]}'", 2, CommandErrorKind.UnknownCommand);
        }

        // Version asymmetry is intentional: a zero-match unknown command (e.g. "deply --version")
        // errors with "No command found" to catch typos, while a known abstract command
        // (e.g. "config --version") resolves version because the command path is valid.
        // Version resolves after the command path is walked, before hierarchy errors
        // (root/abstract levels would otherwise throw before the token is collected)
        if (!result.TargetCommand.HasImplementation && args.Skip(argIndex).Any(HelpVersionGateway.IsVersionToken))
        {
            result.ShowVersion = true;
            return result;
        }

        // If we ended up on a command without implementation, check if it requires subcommands
        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            // Prefix-match + help shows parent help instead of erroring
            if (argIndex > 0 && args.Skip(argIndex).Any(HelpVersionGateway.IsHelpToken))
            {
                result.ShowHelp = true;
                return result;
            }
            // This is an abstract command that requires a subcommand
            var availableSubcommands = string.Join(", ", result.TargetCommand.Children.Keys.OrderBy(k => k));
            var commandName = string.IsNullOrEmpty(result.TargetCommand.FullCommandName) ? "" : result.TargetCommand.FullCommandName;
            throw new CommandException($"'{commandName}' requires a subcommand. Available subcommands: {availableSubcommands}", 2, CommandErrorKind.RequiresSubcommand, commandName);
        }

        if (!result.TargetCommand.HasImplementation)
        {
            throw new CommandException($"No implementation found for command '{result.TargetCommand.FullCommandName}'", 1, CommandErrorKind.NoImplementation);
        }

        // Parse remaining arguments as options and arguments
        ParseOptionsAndArguments(args, argIndex, result);

        return result;
    }

    /// <summary>
    /// Parses options and arguments from the command line
    /// </summary>
    private static void ParseOptionsAndArguments(string[] args, int startIndex, ParseResult result)
    {
        var allOptions = result.TargetCommand.AllOptions;
        var allArguments = result.TargetCommand.AllArguments;
        var argumentValues = new List<string>();
        var argumentIndex = 0;

        for (int i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];

            // Check for help flag
            if (HelpVersionGateway.IsHelpToken(arg))
            {
                result.ShowHelp = true;
                continue;
            }

            // Check for version flag (only leftover unconsumed tokens reach here)
            if (HelpVersionGateway.IsVersionToken(arg))
            {
                result.ShowVersion = true;
                continue;
            }

            // Check if this is an option
            var matchedOption = allOptions.FirstOrDefault(o => o.MatchesArgument(arg));
            if (matchedOption != null)
            {
                var nextArg = i + 1 < args.Length ? args[i + 1] : null;
                var value = matchedOption.ExtractValue(arg, nextArg);

                // If value came from next argument, skip it
                // Non-flag options always consume nextArg as value even if flag-looking (e.g. --config --version)
                if (value == nextArg && (!matchedOption.IsFlag || !(nextArg?.StartsWith('-') == true && !IsNumericValue(nextArg))))
                    i++;

                if (!matchedOption.IsArray
                    && value != null
                    && !IsValuelessFlagOccurrence(matchedOption, arg, nextArg, value)
                    && result.TryGetMergedOptionValues(matchedOption, out _))
                {
                    var name = matchedOption.LongName != null
                        ? $"--{matchedOption.LongName}"
                        : $"-{matchedOption.ShortName}";
                    throw new CommandException($"Duplicate option '{name}' specified multiple times.", 2, CommandErrorKind.DuplicateOption);
                }

                result.AddOptionValue(matchedOption, value);
            }
            else if (arg.StartsWith('-') && !IsNumericValue(arg))
            {
                throw new CommandException($"Unknown option: {arg}", 2, CommandErrorKind.UnknownOption);
            }
            else
            {
                // This is a positional argument
                argumentValues.Add(arg);
            }
        }

        // Assign argument values to their respective arguments
        foreach (var argumentValue in argumentValues)
        {
            var targetArgument = allArguments.FirstOrDefault(a => a.CanAcceptValueAtPosition(argumentIndex));
            if (targetArgument != null)
            {
                AddArgumentValue(result, targetArgument, argumentValue);
                if (!targetArgument.IsArray)
                    argumentIndex++;
            }
            else
            {
                throw new CommandException($"No command found for '{argumentValue}'", 2, CommandErrorKind.UnknownCommand);
            }
        }
    }

    /// <summary>
    /// Valueless bool flags (bare <c>--verbose</c>, defaulting to "true") are
    /// exempt from duplicate rejection; a bool with an explicit value
    /// (<c>--verbose=true</c>, <c>--verbose false</c>) is not exempt.
    /// </summary>
    private static bool IsValuelessFlagOccurrence(SubCommandOptionInfo matchedOption, string arg, string? nextArg, string value) =>
        matchedOption.IsFlag && value != nextArg && !arg.Contains('=');

    /// <summary>
    /// Adds an argument value to the parse result
    /// </summary>
    private static void AddArgumentValue(ParseResult result, SubCommandArgumentInfo argument, string value)
    {
        if (!result.ArgumentValues.ContainsKey(argument))
            result.ArgumentValues[argument] = [];

        result.ArgumentValues[argument].Add(value);
    }

    private static bool IsNumericValue(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith('-') || value.Length < 2)
            return false;

        // Check if what follows the dash is a digit or decimal point
        var afterDash = value[1];
        if (!char.IsDigit(afterDash) && afterDash != '.')
            return false;

        // Try to parse as a double to confirm it's a valid numeric value
        return double.TryParse(value, out _);
    }
}
