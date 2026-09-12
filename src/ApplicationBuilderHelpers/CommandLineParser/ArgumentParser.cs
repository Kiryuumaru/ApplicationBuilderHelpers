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
            var zeroMatchSuggestion = DidYouMean.FindBestMatch(
                args[0],
                DidYouMean.SubCommandCandidates(rootCommand.Children.Keys));
            throw new CommandException(
                DidYouMean.WithSuggestion($"No command found for '{args[0]}'", zeroMatchSuggestion), 2, CommandErrorKind.UnknownCommand);
        }

        // Version asymmetry is intentional: a zero-match unknown command (e.g. "deply --version")
        // errors with "No command found" to catch typos, while a known abstract command
        // (e.g. "config --version") resolves version because the command path is valid.
        // Version resolves after the command path is walked, before hierarchy errors
        // (root/abstract levels would otherwise throw before the token is collected)
        if (!result.TargetCommand.HasImplementation && args.Skip(argIndex).TakeWhile(t => t != "--").Any(HelpVersionGateway.IsVersionToken))
        {
            result.ShowVersion = true;
            return result;
        }

        // If we ended up on a command without implementation, check if it requires subcommands
        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            // Prefix-match + help shows parent help instead of erroring
            if (argIndex > 0 && args.Skip(argIndex).TakeWhile(t => t != "--").Any(HelpVersionGateway.IsHelpToken))
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
        var separatorSeen = false;

        for (int i = startIndex; i < args.Length; i++)
        {
            var arg = args[i];

            // POSIX separator: the first bare `--` is consumed and ends option
            // matching; every following token is a positional argument.
            if (!separatorSeen && arg == "--")
            {
                separatorSeen = true;
                continue;
            }

            if (separatorSeen)
            {
                argumentValues.Add(arg);
                continue;
            }

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

                // Explicit consumed-signal: bare IsFlag (no '=' in token) never consumes
                // next token, even if next == "true". Only bare valued options consume it.
                // Non-flag options always consume nextArg as value even if flag-looking (e.g. --config --version).
                // '='-form and compact '-ovalue' hold the value in-token and never consume.
                var isInTokenValuedForm = arg.Contains('=')
                    || (matchedOption.ShortName.HasValue && !matchedOption.IsFlag && arg.StartsWith($"-{matchedOption.ShortName}") && arg.Length > 2);
                if (!matchedOption.IsFlag && !isInTokenValuedForm && nextArg != null)
                    i++;

                AddParsedOptionValue(result, matchedOption, value, arg, nextArg);
            }
            else if (arg.StartsWith('-') && !IsNumericValue(arg))
            {
                // --no-<name>=value is always a value error, never "Unknown option":
                // matched flags reject in ExtractValue; unmatched (unknown/non-flag) reject here.
                if (arg.StartsWith("--no-", StringComparison.Ordinal) && arg.Contains('='))
                {
                    var name = arg[..arg.IndexOf('=')];
                    var rejected = arg[(arg.IndexOf('=') + 1)..];
                    var negatedLongName = name["--no-".Length..];
                    var owner = allOptions.FirstOrDefault(o => o.LongName != null && o.LongName.Equals(negatedLongName, StringComparison.OrdinalIgnoreCase));
                    throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, owner?.IsSecret ?? false), 2, CommandErrorKind.InvalidValue);
                }

                // Combined short cluster (-abc bool chain, -abdvalue last-takes-value).
                // Only reached when the whole token matched no single option.
                var clusterNextArg = i + 1 < args.Length ? args[i + 1] : null;
                if (TryHandleCombinedShortCluster(arg, clusterNextArg, allOptions, result, out var consumedNext))
                {
                    if (consumedNext)
                        i++;
                    continue;
                }

                var optionSuggestion = DidYouMean.FindBestMatch(
                    arg,
                    DidYouMean.OptionCandidates(allOptions));
                throw new CommandException(
                    DidYouMean.WithSuggestion($"Unknown option: {arg}", optionSuggestion), 2, CommandErrorKind.UnknownOption);
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
                var subcommandSuggestion = DidYouMean.FindBestMatch(
                    argumentValue,
                    DidYouMean.SubCommandCandidates(result.TargetCommand.Children.Keys));
                var surplusMessage = subcommandSuggestion != null
                    ? $"Unknown subcommand '{argumentValue}'"
                    : $"Unexpected argument '{argumentValue}'";
                throw new CommandException(
                    DidYouMean.WithSuggestion(surplusMessage, subcommandSuggestion), 2, CommandErrorKind.UnknownCommand);
            }
        }
    }

    /// <summary>
    /// Records one parsed <c>-o</c> / <c>--option</c> occurrence, enforcing
    /// duplicate rejection for valued occurrences (bare bool flags are exempt).
    /// </summary>
    private static void AddParsedOptionValue(ParseResult result, SubCommandOptionInfo matchedOption, string? value, string arg, string? nextArg)
    {
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

    /// <summary>
    /// Expands a combined short cluster (<c>-abc</c>): each leading flag binds
    /// <c>true</c>; the last short takes the attached remainder as its value
    /// (<c>-abdvalue</c> binds <c>Data=value</c>). A <c>-h</c>/<c>-V</c> char wins
    /// as help/version even mid-cluster. An unknown char rejects the whole
    /// cluster token as <see cref="CommandErrorKind.UnknownOption"/> (exit 2).
    /// Returns false when the token is not a splittable cluster.
    /// </summary>
    private static bool TryHandleCombinedShortCluster(string arg, string? nextArg, List<SubCommandOptionInfo> allOptions, ParseResult result, out bool consumedNext)
    {
        consumedNext = false;

        // Only bare multi-char short bundles qualify: no '=', not '--' long form,
        // and not an already-handled single option match.
        if (arg.Length <= 2 || !arg.StartsWith('-') || arg.StartsWith("--") || arg.Contains('=') || IsNumericValue(arg))
            return false;

        var shorts = allOptions.Where(o => o.ShortName.HasValue).ToList();
        if (shorts.Count == 0)
            return false;

        // AllOptions holds one copy identity per scope (global copies share the
        // same short), so group by short and bind the first identity — the same
        // copy the single-token path matches via FirstOrDefault.
        var byShort = shorts.GroupBy(o => o.ShortName!.Value).ToDictionary(g => g.Key, g => g.First());
        var letters = arg[1..];

        // Every char must resolve before binding anything: distinct whole-token error.
        // The first non-flag short consumes the remainder as its attached value
        // (e.g. -abdvalue binds Data=value), so chars after it are value, not shorts.
        for (var k = 0; k < letters.Length; k++)
        {
            var letter = letters[k];

            // Reserved gateway shorts win as help/version even mid-cluster.
            if (letter == 'h')
                continue;
            if (letter == 'V')
                continue;

            if (!byShort.TryGetValue(letter, out var member))
                throw new CommandException($"Unknown option: {arg}", 2, CommandErrorKind.UnknownOption);

            // A non-flag short takes the attached remainder as its value and ends
            // the cluster; at last position with no remainder it takes next-token.
            if (!member.IsFlag)
                break;
        }

        // All chars resolve: bind in order. The first non-flag short consumes the
        // attached remainder and ends the cluster; later chars are value text.
        for (var k = 0; k < letters.Length; k++)
        {
            var letter = letters[k];

            if (letter == 'h')
            {
                result.ShowHelp = true;
                continue;
            }

            if (letter == 'V')
            {
                result.ShowVersion = true;
                continue;
            }

            var member = byShort[letter];
            if (member.IsFlag)
            {
                // Valued =-form never appears here (clusters contain no '='),
                // so every flag occurrence is valueless and duplicate-exempt.
                AddParsedOptionValue(result, member, "true", $"-{letter}", null);
                continue;
            }

            // First non-flag short: '-dvalue' remainder or next-token value.
            var remainder = arg[(2 + k)..];
            string? value;
            string occurrenceArg;
            string? occurrenceNext;
            if (remainder.Length > 0)
            {
                value = member.ExtractValue($"-{letter}{remainder}", null);
                occurrenceArg = $"-{letter}{remainder}";
                occurrenceNext = null;
            }
            else
            {
                value = member.ExtractValue($"-{letter}", nextArg);
                occurrenceArg = $"-{letter}";
                occurrenceNext = nextArg;
                if (nextArg != null)
                    consumedNext = true;
            }

            AddParsedOptionValue(result, member, value, occurrenceArg, occurrenceNext);
            break;
        }

        return true;
    }

    /// <summary>
    /// Valueless bool flags (bare <c>--verbose</c>, defaulting to "true") are
    /// exempt from duplicate rejection; a bool with an explicit <c>=</c>-form value
    /// (<c>--verbose=true</c>) is not exempt. Space-separated tokens are never
    /// flag values, so a bare flag followed by another token is still valueless.
    /// </summary>
    private static bool IsValuelessFlagOccurrence(SubCommandOptionInfo matchedOption, string arg, string? nextArg, string value) =>
        matchedOption.IsFlag && !arg.Contains('=');

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
