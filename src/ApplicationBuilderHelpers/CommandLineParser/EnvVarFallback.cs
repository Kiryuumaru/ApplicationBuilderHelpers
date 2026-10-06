using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Applies environment-variable fallback for options.
/// </summary>
internal static class EnvVarFallback
{
    /// <summary>
    /// Applies the fallback for a single option when no explicit value was provided.
    /// </summary>
    internal static bool Apply(ParseResult result, SubCommandOptionInfo option, bool requiredOnly)
    {
        if (requiredOnly && !option.IsRequired)
            return false;

        if (string.IsNullOrEmpty(option.EnvironmentVariable))
            return false;

        if (result.TryGetMergedOptionValues(option, out _))
            return false;

        var envValue = Environment.GetEnvironmentVariable(option.EnvironmentVariable);
        if (string.IsNullOrWhiteSpace(envValue))
            return false;

        result.AddOptionValue(option, envValue, isExplicit: false);
        return true;
    }
}
