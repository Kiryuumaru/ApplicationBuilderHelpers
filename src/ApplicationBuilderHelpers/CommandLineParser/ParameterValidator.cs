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
    /// Validates that all required parameters are provided.
    /// A satisfied-then-bare repeat also fails: each bare valued
    /// occurrence is a missing value by itself, regardless of env.
    /// An unsatisfied bare optional valued option fails the same way
    /// even with env set (env rescues only omitted options); a satisfied-then-bare
    /// optional repeat keeps the first value and succeeds.
    /// Help always wins over missing required: when
    /// <see cref="ParseResult.ShowHelp"/> is set, the required-option and
    /// required-argument passes are skipped so help-with-values renders;
    /// the binding probe still runs, so invalid values beat
    /// help-with-values.
    /// The caller (<see cref="CommandLineParser"/>) joins these with the
    /// binding errors so one failure no longer masks another; missing errors
    /// order before binding errors in the final message. Options are visited
    /// once per logical option (canonical key) so global-copy identities never
    /// report twice, and arguments once per display name.
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

                if (result.TryGetMergedOptionValues(option, out _))
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
