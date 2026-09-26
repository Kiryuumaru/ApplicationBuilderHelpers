using ApplicationBuilderHelpers.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Parses command line arguments against the built hierarchy.
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
                break;
            }
        }

        if (argIndex == 0 && args.Length > 0 && !args[0].StartsWith('-'))
        {
            var zeroMatchSuggestion = DidYouMean.FindBestMatch(
                args[0],
                DidYouMean.SubCommandCandidates(rootCommand.Children.Keys));
            throw new CommandException(
                DidYouMean.WithSuggestion($"No command found for '{args[0]}'", zeroMatchSuggestion), 2, CommandErrorKind.UnknownCommand);
        }

        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            // #558; see docs/advanced.md help-precedence: a mistyped subcommand plus
            // --help is still an error (exit 2), not a help request — same as a mistyped
            // top-level command. Skip ShowHelp and fall through to RequiresSubcommand below.
            var surplusSentinelIndex = Array.IndexOf(args, "--");
            var hasSurplusPathToken = argIndex < args.Length
                && !args[argIndex].StartsWith('-')
                && (surplusSentinelIndex < 0 || argIndex < surplusSentinelIndex)
                && result.TargetCommand.FindChild(args[argIndex]) == null;
            if ((result.TargetCommand.IsRoot || argIndex > 0) && !hasSurplusPathToken && args.Skip(argIndex).TakeWhile(t => t != "--").Any(HelpVersionGateway.IsHelpToken))
            {
                result.ShowHelp = true;
                return result;
            }
        }

        if (!result.TargetCommand.HasImplementation && args.Skip(argIndex).TakeWhile(t => t != "--").Any(HelpVersionGateway.IsVersionToken))
        {
            result.ShowVersion = true;
            return result;
        }

        if (IsConcreteRootLeadingHelp(result.TargetCommand, args, argIndex))
        {
            result.ShowHelp = true;
            return result;
        }

        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            var abstractHelpMisuse = args.Skip(argIndex).TakeWhile(t => t != "--")
                .FirstOrDefault(t => IsHelpEqualsOrNegatedToken(t, result.TargetCommand.AllOptions));
            if (abstractHelpMisuse != null)
                throw HelpMisuseError(abstractHelpMisuse, result.TargetCommand.FullCommandName);
            ThrowOnInvalidFlagLiteralPreSentinelOption(result.TargetCommand, args, argIndex);
            ThrowOnUnknownPreSentinelOption(result.TargetCommand, args, argIndex);
            ThrowOnBareValuedPreSentinelOption(result.TargetCommand, args, argIndex);
            var availableSubcommands = string.Join(", ", result.TargetCommand.Children.Keys.OrderBy(k => k));
            var commandName = result.TargetCommand.IsRoot ? "" : result.TargetCommand.FullCommandName;
            var baseMessage = $"'{result.TargetCommand.DisplayName}' requires a subcommand. Available subcommands: {availableSubcommands}";
            string? subcommandSuggestion = null;
            var sentinelIndex = Array.IndexOf(args, "--");
            if (argIndex < args.Length && !args[argIndex].StartsWith('-') && (sentinelIndex < 0 || argIndex < sentinelIndex))
            {
                subcommandSuggestion = DidYouMean.FindBestMatch(
                    args[argIndex],
                    DidYouMean.SubCommandCandidates(result.TargetCommand.Children.Keys));
                if (string.Equals(subcommandSuggestion, args[argIndex], StringComparison.Ordinal))
                    subcommandSuggestion = null;
                if (subcommandSuggestion != null
                    && !args.Skip(argIndex).Any(HelpVersionGateway.IsHelpToken))
                    throw new CommandException(
                        DidYouMean.WithSuggestion($"Unknown subcommand '{args[argIndex]}'", subcommandSuggestion), 2, CommandErrorKind.UnknownCommand, result.TargetCommand.FullCommandName);
            }
            throw new CommandException(DidYouMean.WithSuggestion(baseMessage, subcommandSuggestion), 2, CommandErrorKind.RequiresSubcommand, commandName);
        }

        if (!result.TargetCommand.HasImplementation)
        {
            throw new CommandException($"No implementation found for command '{result.TargetCommand.FullCommandName}'", 1, CommandErrorKind.NoImplementation);
        }

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

            if (HelpVersionGateway.IsHelpToken(arg))
            {
                result.ShowHelp = true;
                continue;
            }

            if (IsHelpEqualsOrNegatedToken(arg, allOptions))
                throw HelpMisuseError(arg, result.TargetCommand.FullCommandName);

            if (HelpVersionGateway.IsVersionToken(arg))
            {
                result.ShowVersion = true;
                continue;
            }

            SubCommandOptionInfo? matchedOption = null;
            if (!IsNumericValue(arg))
                matchedOption = allOptions.FirstOrDefault(o => o.MatchesArgument(arg));
            if (matchedOption != null)
            {
                var nextArg = i + 1 < args.Length ? args[i + 1] : null;
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

                var isInTokenValuedForm = arg.Contains('=')
                    || (matchedOption.ShortName.HasValue && !matchedOption.IsFlag && arg.StartsWith($"-{matchedOption.ShortName}", StringComparison.Ordinal) && arg.Length > 2);
                if (!matchedOption.IsFlag && !isInTokenValuedForm && consumableNext != null)
                    i++;

                AddParsedOptionValue(result, matchedOption, value, arg, consumableNext);
            }
            else if (arg.StartsWith('-') && !IsNumericValue(arg))
            {
                if (arg.StartsWith("--no-", StringComparison.Ordinal) && arg.Contains('='))
                {
                    var name = arg[..arg.IndexOf('=')];
                    var rejected = arg[(arg.IndexOf('=') + 1)..];
                    var resolved = SubCommandOptionInfo.FindNoValueBase(allOptions, name["--no-".Length..]);
                    var noValueCommandName = result.TargetCommand.FullCommandName;
                    if (resolved != null)
                        throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, resolved.IsSecret, isFlag: resolved.IsFlag, positiveLongName: resolved.LongName), 2, CommandErrorKind.InvalidValue, noValueCommandName);
                    if (name.Length == "--no-".Length)
                        throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, isSecret: true, isFlag: false), 2, CommandErrorKind.InvalidValue, noValueCommandName);
                    var noValueSuggestion = DidYouMean.FindBestMatch(
                        name,
                        DidYouMean.OptionCandidates(allOptions));
                    throw new CommandException(
                        DidYouMean.WithSuggestion($"Unknown option: {name}", noValueSuggestion), 2, CommandErrorKind.UnknownOption, noValueCommandName);
                }

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
                var unknownName = arg;
                var unknownEquals = unknownName.IndexOf('=');
                if (unknownEquals >= 0)
                    unknownName = unknownName[..unknownEquals];
                throw new CommandException(
                    DidYouMean.WithSuggestion($"Unknown option: {unknownName}", optionSuggestion), 2, CommandErrorKind.UnknownOption, result.TargetCommand.FullCommandName);
            }
            else
            {
                argumentValues.Add(arg);
            }
        }

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
                if (string.Equals(subcommandSuggestion, argumentValue, StringComparison.Ordinal))
                    subcommandSuggestion = null;
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

        if (arg.Length <= 2 || !arg.StartsWith('-') || arg.StartsWith("--", StringComparison.Ordinal) || arg.Contains('=') || IsNumericValue(arg))
            return false;

        var shorts = allOptions.Where(o => o.ShortName.HasValue).ToList();
        if (shorts.Count == 0)
            return false;

        var byShort = shorts.GroupBy(o => o.ShortName!.Value).ToDictionary(g => g.Key, g => g.First());
        var letters = arg[1..];

        for (var k = 0; k < letters.Length; k++)
        {
            var letter = letters[k];

            if (letter == 'h')
                continue;
            if (letter == 'V')
                continue;

            if (!byShort.TryGetValue(letter, out var member))
                throw new CommandException($"Unknown option: {arg}", 2, CommandErrorKind.UnknownOption, result.TargetCommand.FullCommandName);

            if (!member.IsFlag)
                break;
        }

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
                AddParsedOptionValue(result, member, "true", $"-{letter}", null);
                continue;
            }

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

        var afterDash = value[1];
        if (!char.IsDigit(afterDash) && afterDash != '.')
            return false;

        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>
    /// Reject-by-default neighbor gate: any dash-led non-numeric token is
    /// flag-looking, known or unknown, including <c>--help</c>/<c>-h</c>,
    /// <c>--version</c>/<c>-V</c>, and the <c>--</c> separator. Only numeric
    /// neighbors (<c>-5</c>) and plain words pass as consumable values.
    /// </summary>
    private static bool IsFlagLookingToken(string token) =>
        token.StartsWith('-') && !IsNumericValue(token);

    /// <summary>
    /// Scan the pre-<c>--</c> leftovers on an abstract command for
    /// a known flag in in-token <c>=</c>-form whose literal is invalid (e.g.
    /// <c>--verbose=banana</c>). The literal check uses
    /// <see cref="SubCommandOptionInfo.ExtractValue"/>, which throws
    /// <see cref="CommandErrorKind.InvalidValue"/> naming the option plus the
    /// valid literals with the secret-aware check. Scope is
    /// <c>target.AllOptions</c> through <see cref="SubCommandOptionInfo.MatchesArgument"/>.
    /// Flags plus in-token <c>=</c> only: bare tokens, valued options,
    /// unknown tokens, numerics, help/version tokens, and the
/// <c>--no-</c> prefix are skipped; valid literals continue
/// to <c>RequiresSubcommand</c>.
    /// Runs after the reserved-misuse scan and before the
    /// unknown-option scan, so an invalid literal beats both
    /// <c>RequiresSubcommand</c> and <c>UnknownOption</c>. Post-separator
    /// tokens stay silent for <c>RequiresSubcommand</c>.
    /// </summary>
    private static void ThrowOnInvalidFlagLiteralPreSentinelOption(SubCommandInfo target, string[] args, int argIndex)
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
            if (!token.Contains('='))
                continue;
            if (token.StartsWith("--no-", StringComparison.Ordinal))
                continue;
            var matched = allOptions.FirstOrDefault(o => o.MatchesArgument(token));
            if (matched == null || !matched.IsFlag)
                continue;
            try
            {
                matched.ExtractValue(token, null);
            }
            catch (CommandException ex) when (ex.CommandName is null)
            {
                throw new CommandException(ex.Message, ex.ExitCode, ex.Kind, target.FullCommandName);
            }
        }
    }

    /// <summary>
    /// Scan the pre-<c>--</c> leftovers on an abstract command for
    /// the first dash-led non-numeric token that matches no known option
    /// (<see cref="SubCommandOptionInfo.MatchesArgument"/>, including combined
    /// short clusters with the same reachability as
    /// <see cref="ParseOptionsAndArguments"/>). Help/version tokens already
    /// returned above, so any remaining match here is an unmatched option:
    /// throw <see cref="CommandErrorKind.UnknownOption"/> with a name-only
    /// message (strip any <c>=value</c> suffix) and a did-you-mean
    /// option hint. The bare <c>--</c> itself ends the scan;
    /// post-separator tokens stay silent for RequiresSubcommand.
    /// The reserved help-word scan runs before this method at the
    /// invocation point, so <c>--help=x</c>/<c>-h=x</c>/<c>--no-help</c> report
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
            if (allOptions.Any(o => o.MatchesArgument(token))
                && !(token.StartsWith("--no-", StringComparison.Ordinal) && token.Contains('=')))
                continue;
            if (IsClusterToken(allOptions, token))
                continue;
            if (token.StartsWith("--no-", StringComparison.Ordinal) && token.Contains('='))
            {
                var name = token[..token.IndexOf('=')];
                var rejected = token[(token.IndexOf('=') + 1)..];
                var resolved = SubCommandOptionInfo.FindNoValueBase(allOptions, name["--no-".Length..]);
                if (resolved != null)
                    throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, resolved.IsSecret, isFlag: resolved.IsFlag, positiveLongName: resolved.LongName), 2, CommandErrorKind.InvalidValue, target.FullCommandName);
                if (name.Length == "--no-".Length)
                    throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, isSecret: true, isFlag: false), 2, CommandErrorKind.InvalidValue, target.FullCommandName);
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
    /// Scan the pre-<c>--</c> leftovers on an abstract command for
    /// a valued option in bare form whose neighbor cannot supply
    /// its value. A bare token (exact <c>--long</c>/<c>-s</c> match, no
    /// <c>=</c>, no attached short remainder) matched by
    /// <see cref="SubCommandOptionInfo.MatchesArgument"/> with an
    /// unconsumable neighbor — end of pre-sentinel input or a
    /// flag-looking next token under <see cref="IsFlagLookingToken"/>
    /// (the same consumability rule as
    /// <see cref="ParseOptionsAndArguments"/>) — would extract a null
    /// value in normal parsing, and <see cref="ParameterValidator"/>
    /// reports each bare valued occurrence as missing by itself,
    /// regardless of env. Throw
    /// <see cref="CommandErrorKind.MissingRequired"/> naming the option,
    /// mirroring the validator message (<c>Missing required option</c>
    /// for required, <c>Missing value for option</c> for optional),
    /// instead of letting the
    /// <c>RequiresSubcommand</c> fallback mask it. Runs after the
    /// invalid-literal and unknown scans so <c>InvalidValue</c> and
    /// <c>UnknownOption</c> keep precedence (unknown-first); equals-forms,
    /// attached remainders, flags, numerics, help/version tokens, cluster
    /// tokens, and post-separator tokens stay silent for
    /// <c>RequiresSubcommand</c>. A bare repeat of an already-satisfied
    /// optional valued option stays silent too (record-then-filter over the
    /// pre-sentinel range, canonical key, so either order converges with
    /// <see cref="ParameterValidator"/>); required repeats still throw.
    /// Peek only: consumes nothing.
    /// </summary>
    private static void ThrowOnBareValuedPreSentinelOption(SubCommandInfo target, string[] args, int argIndex)
    {
        var allOptions = target.AllOptions;
        var sentinelIndex = Array.IndexOf(args, "--");
        var end = sentinelIndex < 0 ? args.Length : sentinelIndex;
        var satisfiedKeys = new HashSet<string>(StringComparer.Ordinal);
        for (var i = argIndex; i < end; i++)
        {
            var token = args[i];
            if (!token.StartsWith('-') || IsNumericValue(token) || token == "--")
                continue;
            if (HelpVersionGateway.IsHelpToken(token) || HelpVersionGateway.IsVersionToken(token))
                continue;
            if (token.Contains('='))
            {
                var equalsMatched = allOptions.FirstOrDefault(o => o.MatchesArgument(token));
                if (equalsMatched != null && !equalsMatched.IsFlag)
                    satisfiedKeys.Add(ParseResult.GetCanonicalOptionKey(equalsMatched));
                continue;
            }
            if (IsClusterToken(allOptions, token))
            {
                var clusterNext = i + 1 < end ? args[i + 1] : null;
                RecordClusterSatisfaction(allOptions, token, clusterNext, satisfiedKeys);
                continue;
            }
            var seen = allOptions.FirstOrDefault(o => o.MatchesArgument(token));
            if (seen == null || seen.IsFlag)
                continue;
            if (!IsBareValuedToken(seen, token))
            {
                satisfiedKeys.Add(ParseResult.GetCanonicalOptionKey(seen));
                continue;
            }
            var seenNext = i + 1 < end ? args[i + 1] : null;
            if (seenNext != null && !IsFlagLookingToken(seenNext))
                satisfiedKeys.Add(ParseResult.GetCanonicalOptionKey(seen));
        }
        for (var i = argIndex; i < end; i++)
        {
            var token = args[i];
            if (!token.StartsWith('-') || IsNumericValue(token) || token == "--")
                continue;
            if (HelpVersionGateway.IsHelpToken(token) || HelpVersionGateway.IsVersionToken(token))
                continue;
            if (token.Contains('='))
                continue;
            if (IsClusterToken(allOptions, token))
                continue;
            var matched = allOptions.FirstOrDefault(o => o.MatchesArgument(token));
            if (matched == null || matched.IsFlag)
                continue;
            if (!IsBareValuedToken(matched, token))
                continue;
            var next = i + 1 < end ? args[i + 1] : null;
            if (next != null && !IsFlagLookingToken(next))
                continue;
            if (!matched.IsRequired && satisfiedKeys.Contains(ParseResult.GetCanonicalOptionKey(matched)))
                continue;
            var message = matched.IsRequired
                ? $"Missing required option: {matched.GetDisplayName()}"
                : $"Missing value for option: {matched.GetDisplayName()}";
            throw new CommandException(message, 2, CommandErrorKind.MissingRequired, target.FullCommandName);
        }
    }

    private static void RecordClusterSatisfaction(List<SubCommandOptionInfo> allOptions, string token, string? next, HashSet<string> satisfiedKeys)
    {
        var byShort = new Dictionary<char, SubCommandOptionInfo>();
        foreach (var option in allOptions)
        {
            if (option.ShortName.HasValue && !byShort.ContainsKey(option.ShortName.Value))
                byShort.Add(option.ShortName.Value, option);
        }
        var letters = token[1..];
        for (var k = 0; k < letters.Length; k++)
        {
            var letter = letters[k];
            if (letter == 'h' || letter == 'V')
                continue;
            if (!byShort.TryGetValue(letter, out var member) || member.IsFlag)
                continue;
            var remainder = token[(2 + k)..];
            if (remainder.Length > 0)
                satisfiedKeys.Add(ParseResult.GetCanonicalOptionKey(member));
            else if (next != null && !IsFlagLookingToken(next))
                satisfiedKeys.Add(ParseResult.GetCanonicalOptionKey(member));
            return;
        }
    }

    private static bool IsBareValuedToken(SubCommandOptionInfo option, string token)
    {
        if (option.LongName != null && token == $"--{option.LongName}")
            return true;
        if (option.ShortName.HasValue && token == $"-{option.ShortName}")
            return true;
        return false;
    }

    /// <summary>
    /// Covers combined-short-cluster reachability in
    /// <see cref="TryHandleCombinedShortCluster"/>: a bare multi-char single-dash
    /// token with no <c>=</c> whose every short resolves (reserved <c>h</c>/<c>V</c>
    /// shorts included, valued shorts allowed since the last one takes
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

    /// <summary>
    /// Concrete-root help-first probe: the root command merged with a
    /// <c>MainCommand</c> implementation (<see cref="SubCommandInfo.HasImplementation"/>)
    /// skips the abstract help-first branch, so a leading bare help token would
    /// fall into <see cref="ParseOptionsAndArguments"/> and lose to trailing
    /// tokens (surplus arguments, unknown options) before help renders. Fires
    /// only for a leading bare help token (<see cref="HelpVersionGateway.IsHelpToken"/>)
    /// at the root scope: <c>=</c>-forms, negations, post-separator tokens,
    /// and non-leading help stay on the normal parse path. Yields to a
    /// pre-separator version token, mirroring the abstract branch where the
    /// version check runs before the help check (version beats help).
    /// </summary>
    private static bool IsConcreteRootLeadingHelp(SubCommandInfo target, string[] args, int argIndex)
    {
        return target.IsRoot
            && argIndex == 0
            && argIndex < args.Length
            && HelpVersionGateway.IsHelpToken(args[argIndex])
            && !args.Skip(argIndex).TakeWhile(t => t != "--").Any(HelpVersionGateway.IsVersionToken);
    }

    /// <summary>
    /// Reserved help-word gate: <c>--help=&lt;anything&gt;</c> (including
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
    /// Usage error for reserved help-word misuse: exit 2
    /// <see cref="CommandErrorKind.InvalidValue"/> with the per-command name
    /// attached (as in the <c>ExtractValue</c> rethrow above).
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
            return new CommandException(SecretRedaction.NoValueAcceptedMessage("--no-help", rejected, isSecret: false, isFlag: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        return new CommandException("Option '--no-help' is not valid. Use '--help' to show help.", 2, CommandErrorKind.InvalidValue, commandName);
    }
}
