using ApplicationBuilderHelpers.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
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
            // #512: reserved help-word =-forms and --no-help are usage errors
            // (exit 2 InvalidValue), never a RequiresSubcommand report.
            // Runs BEFORE the #508 unknown-option scan so reserved misuse
            // reports InvalidValue, not UnknownOption.
            var abstractHelpMisuse = args.Skip(argIndex).TakeWhile(t => t != "--")
                .FirstOrDefault(t => IsHelpEqualsOrNegatedToken(t, result.TargetCommand.AllOptions));
            if (abstractHelpMisuse != null)
                throw HelpMisuseError(abstractHelpMisuse, result.TargetCommand.FullCommandName);
            // Issue #508: an unknown dash-led token on an abstract command must
            // report UnknownOption (with help suggestion), not RequiresSubcommand.
            // Runs after the version/help carve-outs so those keep precedence;
            // bare-app, sentinel, numeric, and known-option cases fall through
            // to the RequiresSubcommand path below unchanged.
            ThrowOnUnknownPreSentinelOption(result.TargetCommand, args, argIndex);
            // This is an abstract command that requires a subcommand
            var availableSubcommands = string.Join(", ", result.TargetCommand.Children.Keys.OrderBy(k => k));
            var commandName = string.IsNullOrEmpty(result.TargetCommand.FullCommandName) ? "" : result.TargetCommand.FullCommandName;
            var baseMessage = $"'{commandName}' requires a subcommand. Available subcommands: {availableSubcommands}";
            string? subcommandSuggestion = null;
            var sentinelIndex = Array.IndexOf(args, "--");
            if (argIndex < args.Length && !args[argIndex].StartsWith('-') && (sentinelIndex < 0 || argIndex < sentinelIndex))
            {
                subcommandSuggestion = DidYouMean.FindBestMatch(
                    args[argIndex],
                    DidYouMean.SubCommandCandidates(result.TargetCommand.Children.Keys));
            }
            throw new CommandException(DidYouMean.WithSuggestion(baseMessage, subcommandSuggestion), 2, CommandErrorKind.RequiresSubcommand, commandName);
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

            // #512: reserved help-word =-forms and --no-help never reach the
            // synthetic bool help node (its dummy Property faults in the
            // binder for bool-valid literals). Reject here as a usage error.
            if (IsHelpEqualsOrNegatedToken(arg, allOptions))
                throw HelpMisuseError(arg, result.TargetCommand.FullCommandName);

            // Check for version flag (only leftover unconsumed tokens reach here)
            if (HelpVersionGateway.IsVersionToken(arg))
            {
                result.ShowVersion = true;
                continue;
            }

            // Issue #484: bare numeric tokens (IsNumericValue true, e.g.
            // -1, -12, -1.5, -1e3) skip the single-option match and route to
            // the positional/numeric path below, never a digit ShortName flag.
            // In-token '=' form (-1=value) and compact with non-numeric
            // remainder (-1x) are not numeric per IsNumericValue and still
            // match as options. All-digit compact remainder follows the bare
            // rule to positional. MatchesArgument stays purely lexical by
            // design; the taxonomy decision lives here.
            SubCommandOptionInfo? matchedOption = null;
            if (!IsNumericValue(arg))
                matchedOption = allOptions.FirstOrDefault(o => o.MatchesArgument(arg));
            if (matchedOption != null)
            {
                var nextArg = i + 1 < args.Length ? args[i + 1] : null;
                // Reject-by-default (#469): a bare valued option never consumes a
                // flag-looking neighbor (any dash-led non-numeric token, known or
                // unknown, including --help/--version and the -- separator). The
                // neighbor is left to bind or error on its own merits; the valued
                // option falls back to the trailing-bare missing sentinel (#449:
                // null value, later satisfied by env fallback or MissingRequired).
                // '='-form and compact '-ovalue' hold the value in-token and never
                // consume; numeric neighbors ('-5') are still real values.
                var consumableNext = nextArg != null && !IsFlagLookingToken(nextArg) ? nextArg : null;
                string? value;
                try
                {
                    value = matchedOption.ExtractValue(arg, consumableNext);
                }
                catch (CommandException ex) when (ex.CommandName is null)
                {
                    throw new CommandException(ex.Message, ex.ExitCode, ex.Kind, result.TargetCommand.FullCommandName);
                }

                // Explicit consumed-signal: bare IsFlag (no '=' in token) never consumes
                // next token, even if next == "true". Only bare valued options consume it.
                // '='-form and compact '-ovalue' hold the value in-token and never consume.
                var isInTokenValuedForm = arg.Contains('=')
                    || (matchedOption.ShortName.HasValue && !matchedOption.IsFlag && arg.StartsWith($"-{matchedOption.ShortName}", StringComparison.Ordinal) && arg.Length > 2);
                if (!matchedOption.IsFlag && !isInTokenValuedForm && consumableNext != null)
                    i++;

                AddParsedOptionValue(result, matchedOption, value, arg, consumableNext);
            }
            else if (arg.StartsWith('-') && !IsNumericValue(arg))
            {
                // --no-<name>=value never accepts a value. Dispatch-only: a known
                // base (any kind: flag, valued, collection) in the AllOptions
                // scope rejects as InvalidValue with secret-aware text; an
                // unknown base rejects as UnknownOption naming only the option
                // (plus suggestion), never echoing the value. An empty base
                // fails closed as InvalidValue with redaction on.
                if (arg.StartsWith("--no-", StringComparison.Ordinal) && arg.Contains('='))
                {
                    var name = arg[..arg.IndexOf('=')];
                    var rejected = arg[(arg.IndexOf('=') + 1)..];
                    var resolved = SubCommandOptionInfo.FindNoValueBase(allOptions, name["--no-".Length..]);
                    var noValueCommandName = result.TargetCommand.FullCommandName;
                    if (resolved != null)
                        throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, resolved.IsSecret), 2, CommandErrorKind.InvalidValue, noValueCommandName);
                    if (name.Length == "--no-".Length)
                        throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, isSecret: true), 2, CommandErrorKind.InvalidValue, noValueCommandName);
                    var noValueSuggestion = DidYouMean.FindBestMatch(
                        name,
                        DidYouMean.OptionCandidates(allOptions));
                    throw new CommandException(
                        DidYouMean.WithSuggestion($"Unknown option: {name}", noValueSuggestion), 2, CommandErrorKind.UnknownOption, noValueCommandName);
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
                // Name-only unknown errors (fail-closed logging): strip any
                // '=value' suffix like the --no- path above so a typo such as
                // --pasword=hunter2 never echoes the value to stderr/logs.
                // DidYouMean.Normalize already compares name-only, so the
                // suggestion input stays the full token.
                var unknownName = arg;
                var unknownEquals = unknownName.IndexOf('=');
                if (unknownEquals >= 0)
                    unknownName = unknownName[..unknownEquals];
                throw new CommandException(
                    DidYouMean.WithSuggestion($"Unknown option: {unknownName}", optionSuggestion), 2, CommandErrorKind.UnknownOption, result.TargetCommand.FullCommandName);
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
                if (!targetArgument.IsCollection)
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
                    DidYouMean.WithSuggestion(surplusMessage, subcommandSuggestion), 2, CommandErrorKind.UnknownCommand, result.TargetCommand.FullCommandName);
            }
        }
    }

    /// <summary>
    /// Records one parsed <c>-o</c> / <c>--option</c> occurrence. Scalars and
    /// valued flags resolve last-wins (overwrite); collections accumulate.
    /// Bare bool flags stay idempotent.
    /// </summary>
    private static void AddParsedOptionValue(ParseResult result, SubCommandOptionInfo matchedOption, string? value, string arg, string? nextArg)
    {
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
        if (arg.Length <= 2 || !arg.StartsWith('-') || arg.StartsWith("--", StringComparison.Ordinal) || arg.Contains('=') || IsNumericValue(arg))
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
                throw new CommandException($"Unknown option: {arg}", 2, CommandErrorKind.UnknownOption, result.TargetCommand.FullCommandName);

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
            // A flag-looking neighbor is never consumed (#469, same
            // reject-by-default rule as the single-option path above).
            var remainder = arg[(2 + k)..];
            var consumableClusterNext = nextArg != null && !IsFlagLookingToken(nextArg) ? nextArg : null;
            string? value;
            string occurrenceArg;
            string? occurrenceNext;
            if (remainder.Length > 0)
            {
                value = ExtractClusterValue(member, $"-{letter}{remainder}", null, result);
                occurrenceArg = $"-{letter}{remainder}";
                occurrenceNext = null;
            }
            else
            {
                value = ExtractClusterValue(member, $"-{letter}", consumableClusterNext, result);
                occurrenceArg = $"-{letter}";
                occurrenceNext = consumableClusterNext;
                if (consumableClusterNext != null)
                    consumedNext = true;
            }

            AddParsedOptionValue(result, member, value, occurrenceArg, occurrenceNext);
            break;
        }

        return true;
    }

    private static string? ExtractClusterValue(SubCommandOptionInfo member, string token, string? next, ParseResult result)
    {
        try
        {
            return member.ExtractValue(token, next);
        }
        catch (CommandException ex) when (ex.CommandName is null)
        {
            throw new CommandException(ex.Message, ex.ExitCode, ex.Kind, result.TargetCommand.FullCommandName);
        }
    }

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

        // Try to parse as a double to confirm it's a valid numeric value.
        // InvariantCulture: CLI tokens must resolve identically regardless of CurrentCulture.
        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// Reject-by-default neighbor gate (#469): any dash-led non-numeric token is
    /// flag-looking — known or unknown, including <c>--help</c>/<c>-h</c>,
    /// <c>--version</c>/<c>-V</c>, and the <c>--</c> separator. Only numeric
    /// neighbors (<c>-5</c>) and plain words pass as consumable values.
    /// </summary>
    private static bool IsFlagLookingToken(string token) =>
        token.StartsWith('-') && !IsNumericValue(token);

    /// <summary>
    /// Issue #508: scan the pre-<c>--</c> leftovers on an abstract command for
    /// the first dash-led non-numeric token that matches no known option
    /// (<see cref="SubCommandOptionInfo.MatchesArgument"/>, including combined
    /// short clusters via the same reachability as
    /// <see cref="ParseOptionsAndArguments"/>). Help/version tokens already
    /// returned above, so any remaining match here is a genuine unknown:
    /// throw <see cref="CommandErrorKind.UnknownOption"/> with a name-only
    /// message (fail-closed: strip any <c>=value</c> suffix) and a did-you-mean
    /// option hint. The bare <c>--</c> itself ends the scan (sentinel
    /// precedence); post-separator tokens stay silent for RequiresSubcommand.
    /// NOTE: the #512 reserved help-word scan runs BEFORE this method at the
    /// call site, so <c>--help=x</c>/<c>-h=x</c>/<c>--no-help</c> report
    /// InvalidValue, never UnknownOption here.
    /// </summary>
    private static void ThrowOnUnknownPreSentinelOption(SubCommandInfo target, string[] args, int argIndex)
    {
        var allOptions = target.AllOptions;
        var sentinelIndex = Array.IndexOf(args, "--");
        var end = sentinelIndex < 0 ? args.Length : sentinelIndex;
        for (var i = argIndex; i < end; i++)
        {
            var token = args[i];
            if (!token.StartsWith('-') || IsNumericValue(token) || token == "--")
                continue;
            if (HelpVersionGateway.IsHelpToken(token) || HelpVersionGateway.IsVersionToken(token))
                continue;
            if (allOptions.Any(o => o.MatchesArgument(token)))
                continue;
            if (IsClusterToken(allOptions, token))
                continue;
            if (token.StartsWith("--no-", StringComparison.Ordinal) && token.Contains('='))
            {
                var name = token[..token.IndexOf('=')];
                var rejected = token[(token.IndexOf('=') + 1)..];
                var resolved = SubCommandOptionInfo.FindNoValueBase(allOptions, name["--no-".Length..]);
                if (resolved != null)
                    throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, resolved.IsSecret), 2, CommandErrorKind.InvalidValue, target.FullCommandName);
                if (name.Length == "--no-".Length)
                    throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, isSecret: true), 2, CommandErrorKind.InvalidValue, target.FullCommandName);
                var noValueSuggestion = DidYouMean.FindBestMatch(name, DidYouMean.OptionCandidates(allOptions));
                throw new CommandException(
                    DidYouMean.WithSuggestion($"Unknown option: {name}", noValueSuggestion), 2, CommandErrorKind.UnknownOption, target.FullCommandName);
            }
            var suggestion = DidYouMean.FindBestMatch(token, DidYouMean.OptionCandidates(allOptions));
            var unknownName = token;
            var equals = unknownName.IndexOf('=');
            if (equals >= 0)
                unknownName = unknownName[..equals];
            throw new CommandException(
                DidYouMean.WithSuggestion($"Unknown option: {unknownName}", suggestion), 2, CommandErrorKind.UnknownOption, target.FullCommandName);
        }
    }

    /// <summary>
    /// Mirrors the combined-short-cluster reachability in
    /// <see cref="TryHandleCombinedShortCluster"/>: a bare multi-char single-dash
    /// token with no <c>=</c> whose every short resolves (reserved <c>h</c>/<c>V</c>
    /// gateway shorts included, valued shorts allowed since the last one takes
    /// the remainder/next-token as its value) is a known token, not unknown.
    /// </summary>
    private static bool IsClusterToken(List<SubCommandOptionInfo> allOptions, string token)
    {
        if (token.Length <= 2 || !token.StartsWith('-') || token.StartsWith("--", StringComparison.Ordinal) || token.Contains('=') || IsNumericValue(token))
            return false;
        if (!allOptions.Any(o => o.ShortName.HasValue))
            return false;
        var byShort = new HashSet<char>();
        foreach (var option in allOptions)
        {
            if (option.ShortName.HasValue)
                byShort.Add(option.ShortName.Value);
        }
        foreach (var letter in token[1..])
        {
            if (letter == 'h' || letter == 'V')
                continue;
            if (!byShort.Contains(letter))
                return false;
            var member = allOptions.First(o => o.ShortName == letter);
            if (!member.IsFlag)
                return true;
        }
        return true;
    }

    /// #512 reserved help-word gate: <c>--help=&lt;anything&gt;</c> (including
    /// empty), <c>-h=&lt;anything&gt;</c> (including empty), bare
    /// <c>--no-help</c>, and <c>--no-help=&lt;anything&gt;</c>. Ordinal and
    /// anchored on <c>=</c>/exact: bare <c>--help</c>/<c>-h</c> stay real help
    /// (handled by <see cref="HelpVersionGateway.IsHelpToken"/>), lookalikes
    /// (<c>--helpful</c>, <c>--HELP=x</c>) never match, clusters without
    /// <c>=</c> (e.g. <c>-hfalse</c>) never match, and post-separator tokens
    /// never reach this gate. The synthetic bool help node must not shadow a
    /// real <c>-h</c> owner for <c>=</c>-forms: when some non-help option owns
    /// short <c>'h'</c> (e.g. <c>serve --host</c>), <c>-h=</c> tokens belong
    /// to that option and are left to normal parsing.
    /// </summary>
    private static bool IsHelpEqualsOrNegatedToken(string token, List<SubCommandOptionInfo> allOptions)
    {
        if (token.StartsWith("--help=", StringComparison.Ordinal))
            return true;

        if (token.StartsWith("-h=", StringComparison.Ordinal))
        {
            var hasRealShortHOwner = allOptions.Any(o =>
                o.ShortName == 'h' && !string.Equals(o.LongName, "help", StringComparison.Ordinal));
            return !hasRealShortHOwner;
        }

        if (string.Equals(token, "--no-help", StringComparison.Ordinal)
            || token.StartsWith("--no-help=", StringComparison.Ordinal))
            return true;

        return false;
    }

    /// <summary>
    /// #512 usage error for reserved help-word misuse: exit 2
    /// <see cref="CommandErrorKind.InvalidValue"/> with the per-command name
    /// attached (same pattern as the <c>ExtractValue</c> rethrow above).
    /// <c>=</c>-forms reuse the secret-aware helpers with
    /// <c>isSecret:false</c>; bare <c>--no-help</c> uses a dedicated message
    /// (never <c>NoValueAcceptedMessage</c> with an empty value).
    /// </summary>
    private static CommandException HelpMisuseError(string token, string commandName)
    {
        if (token.StartsWith("--help=", StringComparison.Ordinal))
        {
            var literal = token["--help=".Length..];
            return new CommandException(SecretRedaction.InvalidFlagLiteralMessage(literal, "--help", isSecret: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        if (token.StartsWith("-h=", StringComparison.Ordinal))
        {
            var literal = token["-h=".Length..];
            return new CommandException(SecretRedaction.InvalidFlagLiteralMessage(literal, "-h", isSecret: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        if (token.StartsWith("--no-help=", StringComparison.Ordinal))
        {
            var rejected = token["--no-help=".Length..];
            return new CommandException(SecretRedaction.NoValueAcceptedMessage("--no-help", rejected, isSecret: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        return new CommandException("Option '--no-help' is not valid. Use '--help' to show help.", 2, CommandErrorKind.InvalidValue, commandName);
    }
}
