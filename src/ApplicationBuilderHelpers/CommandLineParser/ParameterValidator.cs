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
    /// </summary>
    public void ValidateRequiredParameters(ParseResult result)
    {
        // Check required options
        foreach (var option in result.TargetCommand.AllOptions.Where(o => o.IsRequired))
        {
            if (!result.TryGetMergedOptionValues(option, out _))
            {
                // Check for environment variable fallback
                if (EnvVarFallback.Apply(result, option, requiredOnly: true))
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
