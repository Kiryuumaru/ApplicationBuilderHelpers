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
    /// Validates that all required parameters are provided
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

                throw new CommandException($"Missing required option: {option.GetDisplayName()}", 1);
            }
        }

        // Check required arguments
        foreach (var argument in result.TargetCommand.AllArguments.Where(a => a.IsRequired))
        {
            if (!result.ArgumentValues.TryGetValue(argument, out List<string>? value) || value.Count == 0)
            {
                throw new CommandException($"Missing required argument: {argument.DisplayName}", 1);
            }
        }
    }
}
