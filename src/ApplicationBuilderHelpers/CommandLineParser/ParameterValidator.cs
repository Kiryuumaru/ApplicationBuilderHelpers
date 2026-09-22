using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Validates that all required parameters are provided.
/// </summary>
internal sealed class ParameterValidator
{
    /// <summary>
    /// Collects every missing-required error without throwing (#496 aggregation).
    /// Validates that all required parameters are provided.
    /// A satisfied-then-bare repeat (#470) also fails: each bare valued
    /// occurrence is a missing value on its own merits, regardless of env.
    /// An unsatisfied bare optional valued option (#503) fails the same way
    /// even with env set (env rescues only omitted options); a satisfied-then-bare
    /// optional repeat keeps the first value and succeeds.
    /// Help always wins over missing required (#509): when
    /// <see cref="ParseResult.ShowHelp"/> is set, the required-option and
    /// required-argument passes are skipped so help-with-values renders at
    /// Step 7b; the binding probe still runs, so invalid values beat
    /// help-with-values (#483 x #509).
    /// The caller (<see cref="CommandLineParser"/>) joins these with the
    /// binding errors so one failure no longer masks another; missing errors
    /// order before binding errors in the final message. Options are visited
    /// once per logical option (canonical key) so global-copy identities never
    /// report twice, and arguments once per display name.
    /// </summary>
    public List<string> CollectRequiredErrors(ParseResult result)
    {
        var errors = new List<string>();
        // #509 (vitruvius): help always wins over missing required. Gate only
        // the required-option and required-argument passes on !ShowHelp; the
        // optional-bare gate below stays as-is.
        if (!result.ShowHelp)
        {
            // Check required options
            var seenOptionKeys = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var option in result.TargetCommand.AllOptions.Where(o => o.IsRequired))
            {
                if (!seenOptionKeys.Add(ParseResult.GetCanonicalOptionKey(option)))
                    continue;

                if (!result.TryGetMergedOptionValues(option, out _))
                {
                    // Explicit bare claims ownership: env rescues only omitted
                    // (never-typed) options, never a typed bare occurrence.
                    if (!result.BareOptionOccurrences.Contains(ParseResult.GetCanonicalOptionKey(option))
                        && EnvVarFallback.Apply(result, option, requiredOnly: true))
                        continue;

                    errors.Add($"Missing required option: {option.GetDisplayName()}");
                    continue;
                }

                if (!option.IsCollection
                    && !option.IsFlag
                    && result.BareOptionOccurrences.Contains(ParseResult.GetCanonicalOptionKey(option)))
                {
                    errors.Add($"Missing required option: {option.GetDisplayName()}");
                }
            }
        }

        // Check unsatisfied bare optional valued options (#503): a bare
        // occurrence with no merged value is a missing value on its own
        // merits and fails exit 2 like the required path, even with env set.
        // A satisfied-then-bare optional
        // repeat keeps its value and stays omitted-success.
        // Help/version precedence (#509): a bare optional never masks an
        // explicit help or version request (optional-bare gate below stays
        // as-is; the required passes above and below are skipped entirely
        // when ShowHelp is set, so help-with-values renders at Step 7b).
        if (!result.ShowHelp && !result.ShowVersion)
        {
            var seenOptionalBareKeys = new HashSet<string>(System.StringComparer.Ordinal);
            foreach (var option in result.TargetCommand.AllOptions.Where(o => !o.IsRequired && !o.IsFlag && !o.IsCollection))
            {
                var key = ParseResult.GetCanonicalOptionKey(option);
                if (!seenOptionalBareKeys.Add(key))
                    continue;

                if (!result.BareOptionOccurrences.Contains(key))
                    continue;

                if (result.TryGetMergedOptionValues(option, out _))
                    continue;

                // Explicit bare claims ownership: env fallback applies only
                // to omitted (never-typed) options, never to a typed bare.
                errors.Add($"Missing value for option: {option.GetDisplayName()}");
            }
        }

        // Check required arguments (#509: skipped when ShowHelp, like the
        // required-option pass above, so help-with-values renders at Step 7b).
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
