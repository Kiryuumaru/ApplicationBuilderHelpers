using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Validates that all required parameters are provided.
/// </summary>
internal sealed class ParameterValidator
{
    /// <summary>
    /// Collects every missing-required error without throwing.
    /// Bare valued occurrences always fail; env rescues only omitted options; help wins.
    /// </summary>
    public List<string> CollectRequiredErrors(ParseResult result)
    {
        var errors = new List<string>();
        if (!result.ShowHelp)
        {
            var seenOptionKeys = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var option in result.TargetCommand.AllOptions.Where(o => o.IsRequired))
            {
                if (!seenOptionKeys.Add(ParseResult.GetCanonicalOptionKey(option)))
                    continue;

                if (!result.TryGetMergedOptionValues(option, out _))
                {
                    if (!result.BareOptionOccurrences.Contains(ParseResult.GetCanonicalOptionKey(option))
                        && EnvVarFallback.Apply(result, option, requiredOnly: true))
                        continue;

                    errors.Add($"Missing required option: {option.GetDisplayName()}");
                    continue;
                }

                if (!option.IsFlag
                    && result.BareOptionOccurrences.Contains(ParseResult.GetCanonicalOptionKey(option)))
                {
                    errors.Add($"Missing required option: {option.GetDisplayName()}");
                }
            }
        }

        if (!result.ShowHelp && !result.ShowVersion)
        {
            var seenOptionalBareKeys = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var option in result.TargetCommand.AllOptions.Where(o => !o.IsRequired && !o.IsFlag))
            {
                var key = ParseResult.GetCanonicalOptionKey(option);
                if (!seenOptionalBareKeys.Add(key))
                    continue;

                if (!result.BareOptionOccurrences.Contains(key))
                    continue;

                errors.Add($"Missing value for option: {option.GetDisplayName()}");
            }
        }

        if (!result.ShowHelp)
        {
            var seenArgumentNames = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var argument in result.TargetCommand.AllArguments.Where(a => a.IsRequired))
            {
                if (!seenArgumentNames.Add(argument.DisplayName))
                    continue;

                if (!result.ArgumentValues.TryGetValue(argument, out List<string>? value) || value.Count == 0)
                {
                    errors.Add($"Missing required argument: {argument.DisplayName}");
                }
            }
        }

        return errors;
    }

    /// <summary>
    /// Collects opt-in duplicate errors (#593 strict mode, default off):
    /// a valued non-collection scalar with two or more explicit CLI valued
    /// occurrences fails <c>DuplicateOption</c> (exit 2), naming the second
    /// occurrence. Collections accumulate, flags stay idempotent, env+CLI
    /// is not a duplicate (env never records a count), alias forms share
    /// one canonical key, and bare occurrences never count (bare fails
    /// <c>MissingRequired</c>, never <c>DuplicateOption</c>, per ADR-0005).
    /// </summary>
    public List<string> CollectDuplicateErrors(ParseResult result, bool rejectDuplicates)
    {
        var errors = new List<string>();
        if (!rejectDuplicates || result.ShowHelp)
            return errors;

        var seenKeys = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var option in result.TargetCommand.AllOptions)
        {
            var key = ParseResult.GetCanonicalOptionKey(option);
            if (!seenKeys.Add(key))
                continue;
            if (option.IsFlag || option.IsCollection)
                continue;
            if (result.ValuedOccurrenceCounts.TryGetValue(key, out var count) && count >= 2)
                errors.Add($"Duplicate option: {option.GetDisplayName()}");
        }

        return errors;
    }
}
