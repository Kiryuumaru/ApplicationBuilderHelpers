using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal sealed class ParameterValidator
{
    /// <summary>Collects scalar-only duplicate errors. Collections, flags, and env-supplied values stay exempt; suppressed under help/version.</summary>
    public List<string> CollectDuplicateErrors(ParseResult result)
    {
        var errors = new List<string>();
        if (result.ShowHelp || result.ShowVersion)
            return errors;

        var seenOptionKeys = new HashSet<string>(System.StringComparer.Ordinal);
        foreach (var option in result.TargetCommand.AllOptions)
        {
            var key = ParseResult.GetCanonicalOptionKey(option);
            if (!seenOptionKeys.Add(key))
                continue;

            if (option.IsFlag || option.IsCollection)
                continue;

            if (!result.TryGetCanonicalIdentityOption(key, out var canonical))
                continue;

            if (canonical.IsFlag || canonical.IsCollection)
                continue;

            if (!result.ValuedOptionOccurrenceCounts.TryGetValue(key, out var count) || count < 2)
                continue;

            errors.Add($"Duplicate option: {canonical.GetDisplayName()}");
        }

        return errors;
    }

    /// <summary>Collects every missing-required error. A bare valued occurrence fails on its own, even with env set or a prior value; help wins over missing.</summary>
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

                if (result.BareOptionOccurrences.Contains(ParseResult.GetCanonicalOptionKey(option)))
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

                if (option.IsCollection && result.TryGetMergedOptionValues(option, out _))
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
}
