using ApplicationBuilderHelpers.Exceptions;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Parse stage: resolves the target command then options/arguments against it.</summary>
/// <remarks>Order: path-walk → zero-match guard → pre-scan (misuse/unknown on abstract + concrete-root-leading-help) → abstract help-first → abstract misuse/unknown scans → version gate → concrete-root help probe → RequiresSubcommand guard → options/arguments.</remarks>
internal sealed class ArgumentParser
{
    /// <summary>Parses argv into the target command plus option/argument occurrences; throws on usage errors.</summary>
    public ParseResult ParseCommandLine(SubCommandInfo rootCommand, string[] args, IReadOnlyDictionary<string, SubCommandOptionInfo>? globals = null)
    {
        var result = new ParseResult();
        var argIndex = 0;

        result.TargetCommand = rootCommand!;
        var pathGlobals = globals ?? new Dictionary<string, SubCommandOptionInfo>();

        while (argIndex < args.Length)
        {
            var token = args[argIndex];
            if (token == "--" || token == "/?")
                break;
            if (token.StartsWith('-'))
            {
                if (IsNumericValue(token) || HelpVersionGateway.IsHelpToken(token) || HelpVersionGateway.IsVersionToken(token))
                    break;
                var matched = pathGlobals.Values.FirstOrDefault(o => o.MatchesArgument(token));
                if (matched == null)
                    break;
                var consumable = argIndex + 1 < args.Length && !IsFlagLookingToken(args[argIndex + 1]) ? args[argIndex + 1] : null;
                if (!matched.IsFlag && IsBareValuedToken(matched, token) && consumable == null)
                    break;
                if (FindValuedNotLastToken(result.TargetCommand.AllOptions, token).HasValue)
                    break;
                string? pathValue;
                try
                {
                    pathValue = matched.ExtractValue(token, consumable);
                }
                catch (CommandException)
                {
                    break;
                }
                result.AddOptionValue(matched, pathValue);
                argIndex++;
                if (!matched.IsFlag && IsBareValuedToken(matched, token) && consumable != null)
                    argIndex++;
                continue;
            }
            var child = result.TargetCommand.FindChild(token);
            if (child == null)
                break;
            result.TargetCommand = child;
            argIndex++;
        }

        if (argIndex == 0 && args.Length > 0 && !args[0].StartsWith('-') && args[0] != "/?" && !args[0].StartsWith("/?=", StringComparison.Ordinal))
        {
            if (!IsExemptRootPositional(rootCommand, args[0]))
            {
                var zeroMatchSuggestion = DidYouMean.SuggestSubcommand(
                    args[0],
                    rootCommand.Children.Keys);
                throw new CommandException(
                    DidYouMean.WithSuggestion($"No command found for '{args[0]}'", zeroMatchSuggestion), 2, CommandErrorKind.UnknownCommand);
            }
        }

        if ((!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0) || IsConcreteRootLeadingHelp(result.TargetCommand, args, argIndex))
        {
            var helpOrderMisuse = args.Skip(argIndex).TakeWhile(t => t != "--")
                .FirstOrDefault(t => IsHelpEqualsOrNegatedToken(t, result.TargetCommand.AllOptions) || IsVersionEqualsOrNegatedToken(t, result.TargetCommand.AllOptions));
            if (helpOrderMisuse != null)
                throw IsHelpEqualsOrNegatedToken(helpOrderMisuse, result.TargetCommand.AllOptions) ? HelpMisuseError(helpOrderMisuse, result.TargetCommand.FullCommandName) : VersionMisuseError(helpOrderMisuse, result.TargetCommand.FullCommandName);
            ThrowOnInvalidFlagLiteralPreSentinelOption(result.TargetCommand, args, argIndex);
            ThrowOnUnknownPreSentinelOption(result.TargetCommand, args, argIndex);
        }

        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            // Mistyped subcommand plus --help is still an error, not a help request.
            var surplusSentinelIndex = Array.IndexOf(args, "--");
            var hasSurplusPathToken = argIndex < args.Length
                && !args[argIndex].StartsWith('-')
                && args[argIndex] != "/?"
                && (surplusSentinelIndex < 0 || argIndex < surplusSentinelIndex)
                && result.TargetCommand.FindChild(args[argIndex]) == null;
            if ((result.TargetCommand.IsRoot || argIndex > 0) && !hasSurplusPathToken && args.Skip(argIndex).TakeWhile(t => t != "--").Any(HelpVersionGateway.IsHelpToken))
            {
                var helpTarget = ResolveHelpTargetCommand(result.TargetCommand, args, argIndex);
                if (helpTarget != null)
                {
                    result.TargetCommand = helpTarget;
                    result.ShowHelp = true;
                    return result;
                }
            }
        }

        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            // Unknown option or version-misuse plus --version is still an error, never a version request.
            var abstractEarlyMisuse = args.Skip(argIndex).TakeWhile(t => t != "--")
                .FirstOrDefault(t => IsHelpEqualsOrNegatedToken(t, result.TargetCommand.AllOptions) || IsVersionEqualsOrNegatedToken(t, result.TargetCommand.AllOptions));
            if (abstractEarlyMisuse != null)
                throw IsHelpEqualsOrNegatedToken(abstractEarlyMisuse, result.TargetCommand.AllOptions)
                    ? HelpMisuseError(abstractEarlyMisuse, result.TargetCommand.FullCommandName)
                    : VersionMisuseError(abstractEarlyMisuse, result.TargetCommand.FullCommandName);
            ThrowOnInvalidFlagLiteralPreSentinelOption(result.TargetCommand, args, argIndex);
            ThrowOnUnknownPreSentinelOption(result.TargetCommand, args, argIndex);
        }

        if (!result.TargetCommand.HasImplementation && args.Skip(argIndex).TakeWhile(t => t != "--").Any(HelpVersionGateway.IsVersionToken))
        {
            // Version beats help on abstract paths, so it short-circuits before the help probe.
            result.ShowVersion = true;
            return result;
        }

        if (IsConcreteRootLeadingHelp(result.TargetCommand, args, argIndex))
        {
            var helpTarget = ResolveHelpTargetCommand(result.TargetCommand, args, argIndex);
            if (helpTarget != null)
            {
                result.TargetCommand = helpTarget;
                result.ShowHelp = true;
                return result;
            }
        }

        if (!result.TargetCommand.HasImplementation && result.TargetCommand.Children.Count > 0)
        {
            // First misuse token wins.
            var abstractMisuse = args.Skip(argIndex).TakeWhile(t => t != "--")
                .FirstOrDefault(t => IsHelpEqualsOrNegatedToken(t, result.TargetCommand.AllOptions) || IsVersionEqualsOrNegatedToken(t, result.TargetCommand.AllOptions));
            if (abstractMisuse != null)
                throw IsHelpEqualsOrNegatedToken(abstractMisuse, result.TargetCommand.AllOptions)
                    ? HelpMisuseError(abstractMisuse, result.TargetCommand.FullCommandName)
                    : VersionMisuseError(abstractMisuse, result.TargetCommand.FullCommandName);
            ThrowOnInvalidFlagLiteralPreSentinelOption(result.TargetCommand, args, argIndex);
            ThrowOnUnknownPreSentinelOption(result.TargetCommand, args, argIndex);
            ThrowOnBareValuedPreSentinelOption(result.TargetCommand, args, argIndex);
            var availableSubcommands = string.Join(", ", result.TargetCommand.Children.Keys.OrderBy(k => k));
            var commandName = result.TargetCommand.IsRoot ? "" : result.TargetCommand.FullCommandName;
            var baseMessage = $"'{result.TargetCommand.DisplayName}' requires a subcommand. Available subcommands: {availableSubcommands}";
            string? subcommandSuggestion = null;
            var sentinelIndex = Array.IndexOf(args, "--");
            if (argIndex < args.Length && !args[argIndex].StartsWith('-') && args[argIndex] != "/?" && (sentinelIndex < 0 || argIndex < sentinelIndex))
            {
                subcommandSuggestion = DidYouMean.SuggestSubcommand(
                    args[argIndex],
                    result.TargetCommand.Children.Keys);
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

    /// <summary>Parses options/arguments at the start index into the result.</summary>
    private static void ParseOptionsAndArguments(string[] args, int startIndex, ParseResult result)
    {
        var allOptions = result.TargetCommand.AllOptions;
        var allArguments = result.TargetCommand.AllArguments;
        var argumentValues = new List<string>();
        var argumentIndex = 0;
        var separatorSeen = false;
        var tail = args[startIndex..];
        var versionWins = HelpVersionGateway.RequestedVersion(tail)
            && !HelpVersionGateway.RequestedHelp(tail);

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
            {
                if (versionWins)
                {
                    result.ShowVersion = true;
                    continue;
                }

                throw HelpMisuseError(arg, result.TargetCommand.FullCommandName);
            }

            if (HelpVersionGateway.IsVersionToken(arg))
            {
                result.ShowVersion = true;
                continue;
            }

            if (IsVersionEqualsOrNegatedToken(arg, allOptions))
            {
                if (versionWins)
                {
                    result.ShowVersion = true;
                    continue;
                }

                throw VersionMisuseError(arg, result.TargetCommand.FullCommandName);
            }

            var valuedNotLastProbe = FindValuedNotLastToken(allOptions, arg);
            if (valuedNotLastProbe.HasValue)
                throw new CommandException(
                    $"Option '-{valuedNotLastProbe.Value}' requires a value and must be last in a combined short cluster; use '-{valuedNotLastProbe.Value} <value>', '-{valuedNotLastProbe.Value}=<value>', or place it last.",
                    2, CommandErrorKind.InvalidValue, result.TargetCommand.FullCommandName);

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
                catch (CommandException ex) when (ex.CommandName is null && ex.Kind == CommandErrorKind.InvalidValue && HelpVersionGateway.RequestedHelp(tail))
                {
                    result.ShowHelp = true;
                    continue;
                }
                catch (CommandException ex) when (ex.CommandName is null && ex.Kind == CommandErrorKind.InvalidValue && versionWins)
                {
                    // Pre--- version request outranks eager value errors; let the version gate fire.
                    result.ShowVersion = true;
                    continue;
                }
                catch (CommandException ex) when (ex.CommandName is null)
                {
                    // Option ExtractValue throws nameless; stamp the command name so the footer scopes the hint.
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
                    if (HelpVersionGateway.RequestedHelp(tail))
                    {
                        var helpProbeName = arg[..arg.IndexOf('=')];
                        var helpProbeBase = helpProbeName["--no-".Length..];
                        if (SubCommandOptionInfo.FindNoValueBase(allOptions, helpProbeBase) != null)
                        {
                            result.ShowHelp = true;
                            continue;
                        }
                    }

                    if (versionWins)
                    {
                        var probeName = arg[..arg.IndexOf('=')];
                        var probeBase = probeName["--no-".Length..];
                        if (SubCommandOptionInfo.FindNoValueBase(allOptions, probeBase) != null)
                        {
                            result.ShowVersion = true;
                            continue;
                        }
                    }

                    var name = arg[..arg.IndexOf('=')];
                    var rejected = arg[(arg.IndexOf('=') + 1)..];
                    var resolved = SubCommandOptionInfo.FindNoValueBase(allOptions, name["--no-".Length..]);
                    var noValueCommandName = result.TargetCommand.FullCommandName;
                    if (resolved != null)
                        throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, resolved.IsSecret, isFlag: resolved.IsFlag, positiveLongName: resolved.LongName), 2, CommandErrorKind.InvalidValue, noValueCommandName);
                    if (name.Length == "--no-".Length)
                        throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, isSecret: true, isFlag: false), 2, CommandErrorKind.InvalidValue, noValueCommandName);
                    var noValueSuggestion = DidYouMean.SuggestBlamedToken(
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

                if (arg.StartsWith("--no-", StringComparison.Ordinal) && !arg.Contains('='))
                {
                    var resolvedBare = SubCommandOptionInfo.FindNoValueBase(allOptions, arg["--no-".Length..]);
                    if (resolvedBare != null && !resolvedBare.IsFlag)
                    {
                        if (versionWins)
                        {
                            result.ShowVersion = true;
                            continue;
                        }

                        var bareCommandName = result.TargetCommand.FullCommandName;
                        throw new CommandException(SecretRedaction.NoValueAcceptedMessage(arg, string.Empty, resolvedBare.IsSecret, isFlag: false, positiveLongName: resolvedBare.LongName), 2, CommandErrorKind.InvalidValue, bareCommandName);
                    }
                }

                var unknownName = arg;
                var unknownEquals = unknownName.IndexOf('=');
                if (unknownEquals >= 0)
                    unknownName = unknownName[..unknownEquals];
                var optionSuggestion = DidYouMean.SuggestBlamedToken(
                    unknownName,
                    DidYouMean.OptionCandidates(allOptions));
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
                var subcommandSuggestion = DidYouMean.SuggestSubcommand(
                    argumentValue,
                    result.TargetCommand.Children.Keys);
                var surplusMessage = subcommandSuggestion != null
                    ? $"Unknown subcommand '{argumentValue}'"
                    : $"Unexpected argument '{argumentValue}'";
                throw new CommandException(
                    DidYouMean.WithSuggestion(surplusMessage, subcommandSuggestion), 2, CommandErrorKind.UnknownCommand, result.TargetCommand.FullCommandName);
            }
        }
    }

    /// <summary>Records one parsed option occurrence into the result.</summary>
    private static void AddParsedOptionValue(ParseResult result, SubCommandOptionInfo matchedOption, string? value, string arg, string? nextArg)
    {
        result.AddOptionValue(matchedOption, value);
    }

    /// <summary>Expands a combined short cluster into occurrences; returns false when not splittable.</summary>
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
            {
                if (!ClusterContainsValuedShort(letters, byShort))
                {
                    var nameOnly = arg;
                    var equals = nameOnly.IndexOf('=');
                    if (equals >= 0)
                        nameOnly = nameOnly[..equals];
                    var fullTokenSuggestion = DidYouMean.SuggestBlamedToken(
                        nameOnly,
                        DidYouMean.OptionCandidates(allOptions));
                    if (fullTokenSuggestion != null
                        && string.Equals(DidYouMean.Normalize(fullTokenSuggestion), DidYouMean.Normalize(nameOnly), StringComparison.Ordinal))
                        throw new CommandException(
                            DidYouMean.WithSuggestion($"Unknown option: {nameOnly}", fullTokenSuggestion), 2, CommandErrorKind.UnknownOption, result.TargetCommand.FullCommandName);
                }

                throw new CommandException(SecretRedaction.UnknownClusterCharMessage(arg, 1 + k), 2, CommandErrorKind.UnknownOption, result.TargetCommand.FullCommandName);
            }

            if (!member.IsFlag)
            {
                var violating = FindValuedNotLastLetter(letters, byShort);
                if (violating.HasValue)
                    throw new CommandException(
                        $"Option '-{violating.Value}' requires a value and must be last in a combined short cluster; use '-{violating.Value} <value>', '-{violating.Value}=<value>', or place it last.",
                        2, CommandErrorKind.InvalidValue, result.TargetCommand.FullCommandName);
                break;
            }
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

    /// <summary>Whether any known short in the letters owns a value.</summary>
    private static bool ClusterContainsValuedShort(string letters, Dictionary<char, SubCommandOptionInfo> byShort)
    {
        foreach (var letter in letters)
        {
            if (letter == 'h' || letter == 'V')
                continue;
            if (byShort.TryGetValue(letter, out var member) && !member.IsFlag)
                return true;
        }

        return false;
    }

    /// <summary>First valued short whose remainder is exactly a known short; null when none.</summary>
    private static char? FindValuedNotLastLetter(string letters, Dictionary<char, SubCommandOptionInfo> byShort)
    {
        for (var k = 0; k < letters.Length; k++)
        {
            var letter = letters[k];
            if (letter == 'h' || letter == 'V')
                continue;
            if (!byShort.TryGetValue(letter, out var member))
                return null;
            if (member.IsFlag)
                continue;
            var remainder = letters[(k + 1)..];
            if (remainder.Length == 1
                && (remainder[0] == 'h' || remainder[0] == 'V' || byShort.ContainsKey(remainder[0])))
                return letter;
            return null;
        }

        return null;
    }

    /// <summary>Re-attaches the command name to cluster-extracted errors.</summary>
    private static string? ExtractClusterValue(SubCommandOptionInfo member, string token, string? next, ParseResult result)
    {
        try
        {
            return member.ExtractValue(token, next);
        }
        catch (CommandException ex) when (ex.CommandName is null)
        {
            // Cluster ExtractValue throws nameless; stamp the command name so the footer scopes the hint.
            throw new CommandException(ex.Message, ex.ExitCode, ex.Kind, result.TargetCommand.FullCommandName);
        }
    }

    /// <summary>Appends a positional value into the result.</summary>
    private static void AddArgumentValue(ParseResult result, SubCommandArgumentInfo argument, string value)
    {
        if (!result.ArgumentValues.ContainsKey(argument))
            result.ArgumentValues[argument] = [];

        result.ArgumentValues[argument].Add(value);
    }

    /// <summary>Whether the value parses as a negative number (consumable value, not a flag).</summary>
    private static bool IsNumericValue(string value)
    {
        if (string.IsNullOrEmpty(value) || !value.StartsWith('-') || value.Length < 2)
            return false;

        var afterDash = value[1];
        if (!char.IsDigit(afterDash) && afterDash != '.')
            return false;

        return double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _);
    }

    /// <summary>Whether the token is flag-looking: dash-led and non-numeric.</summary>
    private static bool IsFlagLookingToken(string token) =>
        token.StartsWith('-') && !IsNumericValue(token);

    /// <summary>Throws on an invalid flag literal in =-form on an abstract path.</summary>
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
                // Flag ExtractValue throws nameless; stamp the command name so the footer scopes the hint.
                throw new CommandException(ex.Message, ex.ExitCode, ex.Kind, target.FullCommandName);
            }
        }
    }

    /// <summary>Throws on the first unknown pre-separator option on an abstract path.</summary>
    private static void ThrowOnUnknownPreSentinelOption(SubCommandInfo target, string[] args, int argIndex)
    {
        var allOptions = target.AllOptions;
        var sentinelIndex = Array.IndexOf(args, "--");
        var end = sentinelIndex < 0 ? args.Length : sentinelIndex;
        var tail = args[argIndex..];
        var versionWins = HelpVersionGateway.RequestedVersion(tail)
            && !HelpVersionGateway.RequestedHelp(tail);
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
            var valuedNotLast = FindValuedNotLastToken(allOptions, token);
            if (valuedNotLast.HasValue)
                throw new CommandException(
                    $"Option '-{valuedNotLast.Value}' requires a value and must be last in a combined short cluster; use '-{valuedNotLast.Value} <value>', '-{valuedNotLast.Value}=<value>', or place it last.",
                    2, CommandErrorKind.InvalidValue, target.FullCommandName);
            if (IsClusterToken(allOptions, token))
                continue;
            if (token.StartsWith("--no-", StringComparison.Ordinal) && token.Contains('='))
            {
                if (versionWins)
                {
                    var noProbeName = token[..token.IndexOf('=')];
                    var noProbeBase = noProbeName["--no-".Length..];
                    if (SubCommandOptionInfo.FindNoValueBase(allOptions, noProbeBase) != null || noProbeBase.Length == 0)
                        continue;
                }

                var name = token[..token.IndexOf('=')];
                var rejected = token[(token.IndexOf('=') + 1)..];
                var resolved = SubCommandOptionInfo.FindNoValueBase(allOptions, name["--no-".Length..]);
                if (resolved != null)
                    throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, resolved.IsSecret, isFlag: resolved.IsFlag, positiveLongName: resolved.LongName), 2, CommandErrorKind.InvalidValue, target.FullCommandName);
                if (name.Length == "--no-".Length)
                    throw new CommandException(SecretRedaction.NoValueAcceptedMessage(name, rejected, isSecret: true, isFlag: false), 2, CommandErrorKind.InvalidValue, target.FullCommandName);
                var noValueSuggestion = DidYouMean.SuggestBlamedToken(name, DidYouMean.OptionCandidates(allOptions));
                throw new CommandException(
                    DidYouMean.WithSuggestion($"Unknown option: {name}", noValueSuggestion), 2, CommandErrorKind.UnknownOption, target.FullCommandName);
            }
            if (token.StartsWith("--no-", StringComparison.Ordinal) && !token.Contains('='))
            {
                var resolvedBare = SubCommandOptionInfo.FindNoValueBase(allOptions, token["--no-".Length..]);
                if (resolvedBare != null && !resolvedBare.IsFlag)
                {
                    if (versionWins)
                        continue;
                    throw new CommandException(SecretRedaction.NoValueAcceptedMessage(token, string.Empty, resolvedBare.IsSecret, isFlag: false, positiveLongName: resolvedBare.LongName), 2, CommandErrorKind.InvalidValue, target.FullCommandName);
                }
            }
            var clusterFailing = FindUnknownClusterCharIndex(allOptions, token);
            if (clusterFailing >= 0)
            {
                if (!ClusterContainsValuedShortOption(allOptions, token))
                {
                    var fullNameOnly = token;
                    var fullEquals = fullNameOnly.IndexOf('=');
                    if (fullEquals >= 0)
                        fullNameOnly = fullNameOnly[..fullEquals];
                    var fullSuggestion = DidYouMean.SuggestBlamedToken(
                        fullNameOnly,
                        DidYouMean.OptionCandidates(allOptions));
                    if (fullSuggestion != null
                        && string.Equals(DidYouMean.Normalize(fullSuggestion), DidYouMean.Normalize(fullNameOnly), StringComparison.Ordinal))
                        throw new CommandException(
                            DidYouMean.WithSuggestion($"Unknown option: {fullNameOnly}", fullSuggestion), 2, CommandErrorKind.UnknownOption, target.FullCommandName);
                }

                var clusterFragment = $"-{token[clusterFailing]}";
                var clusterSuggestion = DidYouMean.SuggestBlamedToken(clusterFragment, DidYouMean.OptionCandidates(allOptions));
                throw new CommandException(
                    DidYouMean.WithSuggestion(SecretRedaction.UnknownClusterCharMessage(token, clusterFailing), clusterSuggestion), 2, CommandErrorKind.UnknownOption, target.FullCommandName);
            }
            var unknownName = token;
            var equals = unknownName.IndexOf('=');
            if (equals >= 0)
                unknownName = unknownName[..equals];
            var suggestion = DidYouMean.SuggestBlamedToken(unknownName, DidYouMean.OptionCandidates(allOptions));
            throw new CommandException(
                DidYouMean.WithSuggestion($"Unknown option: {unknownName}", suggestion), 2, CommandErrorKind.UnknownOption, target.FullCommandName);
        }
    }

    /// <summary>Throws on a bare valued option with no consumable neighbor on an abstract path.</summary>
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

    /// <summary>Records a valued short's satisfaction for the token.</summary>
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

    /// <summary>Whether the token is the exact bare form of the valued option.</summary>
    private static bool IsBareValuedToken(SubCommandOptionInfo option, string token)
    {
        if (option.LongName != null && token == $"--{option.LongName}")
            return true;
        if (option.ShortName.HasValue && token == $"-{option.ShortName}")
            return true;
        return false;
    }

    /// <summary>Whether the token is a splittable combined short cluster.</summary>
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

    /// <summary>Index of the first unknown char in the token, or -1 when splittable.</summary>
    private static int FindUnknownClusterCharIndex(List<SubCommandOptionInfo> allOptions, string token)
    {
        if (token.Length <= 2 || !token.StartsWith('-') || token.StartsWith("--", StringComparison.Ordinal) || token.Contains('=') || IsNumericValue(token))
            return -1;
        if (!allOptions.Any(o => o.ShortName.HasValue))
            return -1;
        var byShort = new HashSet<char>();
        foreach (var option in allOptions)
        {
            if (option.ShortName.HasValue)
                byShort.Add(option.ShortName.Value);
        }
        for (var i = 1; i < token.Length; i++)
        {
            var letter = token[i];
            if (letter == 'h' || letter == 'V')
                continue;
            if (!byShort.Contains(letter))
                return i;
            var member = allOptions.First(o => o.ShortName == letter);
            if (!member.IsFlag)
                return -1;
        }
        return -1;
    }

    /// <summary>Valued-not-last probe for the token; returns the offending short letter.</summary>
    private static char? FindValuedNotLastToken(List<SubCommandOptionInfo> allOptions, string token)
    {
        if (token.Length <= 2 || !token.StartsWith('-') || token.StartsWith("--", StringComparison.Ordinal) || token.Contains('=') || IsNumericValue(token))
            return null;
        if (!allOptions.Any(o => o.ShortName.HasValue))
            return null;
        var byShort = new Dictionary<char, SubCommandOptionInfo>();
        foreach (var option in allOptions)
        {
            if (option.ShortName.HasValue && !byShort.ContainsKey(option.ShortName.Value))
                byShort.Add(option.ShortName.Value, option);
        }
        var letters = token[1..];
        return FindValuedNotLastLetter(letters, byShort);
    }

    /// <summary>Whether the token contains a valued short, so reports stay failing-char-only.</summary>
    private static bool ClusterContainsValuedShortOption(List<SubCommandOptionInfo> allOptions, string token)
    {
        if (token.Length <= 2 || !token.StartsWith('-') || token.StartsWith("--", StringComparison.Ordinal) || IsNumericValue(token))
            return false;
        var nameOnly = token;
        var equals = nameOnly.IndexOf('=');
        if (equals >= 0)
            nameOnly = nameOnly[..equals];
        var letters = nameOnly[1..];
        var byShort = new HashSet<char>();
        var valued = new HashSet<char>();
        foreach (var option in allOptions)
        {
            if (!option.ShortName.HasValue)
                continue;
            byShort.Add(option.ShortName.Value);
            if (!option.IsFlag)
                valued.Add(option.ShortName.Value);
        }
        foreach (var letter in letters)
        {
            if (letter == 'h' || letter == 'V')
                continue;
            if (valued.Contains(letter))
                return true;
        }

        return false;
    }

    /// <summary>Whether the root accepts the leading token as a positional instead of an error.</summary>
    /// <remarks>
    /// The tie/silence probe routes through the subcommand emit gate on
    /// purpose: a near-miss leaf name keeps its suggestion downstream, while
    /// a tied or exact-known token returns null here and binds positionally.
    /// </remarks>
    private static bool IsExemptRootPositional(SubCommandInfo rootCommand, string token)
    {
        if (!rootCommand.IsRoot || !rootCommand.HasImplementation)
            return false;
        if (token.StartsWith('-'))
            return false;
        if (!rootCommand.AllArguments.Any(a => a.CanAcceptValueAtPosition(0)))
            return false;
        if (rootCommand.Children.Count == 0)
            return true;
        if (rootCommand.FindChild(token) != null)
            return false;
        return DidYouMean.SuggestSubcommand(
            token,
            rootCommand.Children.Keys) == null;
    }

    /// <summary>Resolves which command a pre-separator help token renders help for.</summary>
    private static SubCommandInfo? ResolveHelpTargetCommand(SubCommandInfo target, string[] args, int argIndex)
    {
        var sentinelIndex = Array.IndexOf(args, "--");
        var end = sentinelIndex < 0 ? args.Length : sentinelIndex;
        var i = argIndex;
        while (i < end && HelpVersionGateway.IsHelpToken(args[i]))
            i++;
        if (i >= end)
            return target;
        if (args[i].StartsWith('-') || args[i] == "/?")
            return target;
        var current = target;
        var resolved = false;
        while (i < end && !args[i].StartsWith('-') && args[i] != "/?")
        {
            var child = current.FindChild(args[i]);
            if (child == null)
                break;
            current = child;
            resolved = true;
            i++;
        }
        return resolved ? current : null;
    }

    /// <summary>Whether a leading bare help token fires early on a concrete root.</summary>
    private static bool IsConcreteRootLeadingHelp(SubCommandInfo target, string[] args, int argIndex)
    {
        return target.IsRoot
            && argIndex == 0
            && argIndex < args.Length
            && HelpVersionGateway.IsHelpToken(args[argIndex])
            && !args.Skip(argIndex).TakeWhile(t => t != "--").Any(HelpVersionGateway.IsVersionToken);
    }

    /// <summary>Whether the token is a reserved help-word misuse form.</summary>
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

        if (token.StartsWith("-?=", StringComparison.Ordinal))
            return true;

        if (token.StartsWith("/?=", StringComparison.Ordinal))
            return true;

        if (string.Equals(token, "--no-help", StringComparison.Ordinal)
            || token.StartsWith("--no-help=", StringComparison.Ordinal))
            return true;

        return false;
    }

    /// <summary>Usage error for reserved help-word misuse; throws exit 2 with the command name.</summary>
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

        if (token.StartsWith("-?=", StringComparison.Ordinal))
        {
            var literal = token["-?=".Length..];
            return new CommandException(SecretRedaction.InvalidFlagLiteralMessage(literal, "-?", isSecret: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        if (token.StartsWith("/?=", StringComparison.Ordinal))
        {
            var literal = token["/?=".Length..];
            return new CommandException(SecretRedaction.InvalidFlagLiteralMessage(literal, "/?", isSecret: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        if (token.StartsWith("--no-help=", StringComparison.Ordinal))
        {
            var rejected = token["--no-help=".Length..];
            return new CommandException(SecretRedaction.NoValueAcceptedMessage("--no-help", rejected, isSecret: false, isFlag: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        return new CommandException("Option '--no-help' is not valid. Use '--help' to show help.", 2, CommandErrorKind.InvalidValue, commandName);
    }

    /// <summary>Whether the token is a reserved version-word misuse form.</summary>
    private static bool IsVersionEqualsOrNegatedToken(string token, List<SubCommandOptionInfo> allOptions)
    {
        if (token.StartsWith("--version=", StringComparison.Ordinal))
            return true;

        if (token.StartsWith("-V=", StringComparison.Ordinal))
        {
            var hasRealShortVOwner = allOptions.Any(o => o.ShortName == 'V');
            return !hasRealShortVOwner;
        }

        if (string.Equals(token, "--no-version", StringComparison.Ordinal)
            || token.StartsWith("--no-version=", StringComparison.Ordinal))
            return true;

        return false;
    }

    /// <summary>Usage error for reserved version-word misuse; throws exit 2 with the command name.</summary>
    private static CommandException VersionMisuseError(string token, string commandName)
    {
        if (token.StartsWith("--version=", StringComparison.Ordinal))
        {
            var literal = token["--version=".Length..];
            return new CommandException(SecretRedaction.InvalidFlagLiteralMessage(literal, "--version", isSecret: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        if (token.StartsWith("-V=", StringComparison.Ordinal))
        {
            var literal = token["-V=".Length..];
            return new CommandException(SecretRedaction.InvalidFlagLiteralMessage(literal, "-V", isSecret: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        if (token.StartsWith("--no-version=", StringComparison.Ordinal))
        {
            var rejected = token["--no-version=".Length..];
            return new CommandException(SecretRedaction.NoValueAcceptedMessage("--no-version", rejected, isSecret: false, isFlag: false), 2, CommandErrorKind.InvalidValue, commandName);
        }

        return new CommandException("Option '--no-version' is not valid. Use '--version' to show version.", 2, CommandErrorKind.InvalidValue, commandName);
    }
}
