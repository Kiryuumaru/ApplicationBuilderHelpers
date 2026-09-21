using ApplicationBuilderHelpers.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Validates that all required parameters are provided.
/// </summary>
internal sealed class ParameterValidator
{
    /// <summary>
    /// Validates that all required parameters are provided.
    /// A satisfied-then-bare repeat (#470) also fails: each bare valued
    /// occurrence is a missing value on its own merits, regardless of env.
    /// An unsatisfied bare optional valued option (#503) fails the same way
    /// even with env set (env rescues only omitted options); a satisfied-then-bare
    /// optional repeat keeps the first value and succeeds.
    /// </summary>
    public void ValidateRequiredParameters(ParseResult result)
    {
        // Check required options
        foreach (var option in result.TargetCommand.AllOptions.Where(o => o.IsRequired))
        {
            if (!result.TryGetMergedOptionValues(option, out _))
            {
                // Explicit bare claims ownership: env rescues only omitted
                // (never-typed) options, never a typed bare occurrence.
                if (!result.BareOptionOccurrences.Contains(ParseResult.GetCanonicalOptionKey(option))
                    && EnvVarFallback.Apply(result, option, requiredOnly: true))
                    continue;

                throw new CommandException($"Missing required option: {option.GetDisplayName()}", 2, CommandErrorKind.MissingRequired);
            }

            if (!option.IsCollection
                && !option.IsFlag
                && result.BareOptionOccurrences.Contains(ParseResult.GetCanonicalOptionKey(option)))
            {
                throw new CommandException($"Missing required option: {option.GetDisplayName()}", 2, CommandErrorKind.MissingRequired);
            }
        }

        // Check unsatisfied bare optional valued options (#503): a bare
        // occurrence with no merged value is a missing value on its own
        // merits and fails exit 2 like the required path, even with env set.
        // A satisfied-then-bare optional
        // repeat keeps its value and stays omitted-success.
        // Help/version precedence: a bare optional never masks an explicit
        // help or version request (the required passes above and below still
        // run first/last, preserving Help_Does_Not_Skip_Required_Validation).
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
                throw new CommandException($"Missing value for option: {option.GetDisplayName()}", 2, CommandErrorKind.MissingRequired);
            }
        }

        // Check required arguments
        foreach (var argument in result.TargetCommand.AllArguments.Where(a => a.IsRequired))
        {
            if (!result.ArgumentValues.TryGetValue(argument, out List<string>? value) || value.Count == 0)
            {
                throw new CommandException($"Missing required argument: {argument.DisplayName}", 2, CommandErrorKind.MissingRequired);
            }
        }
    }
}
