using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Applies environment-variable fallback for options, preserving the exact
/// single-read semantics of the original CommandLineParser:
/// a single lookup whose value is both guarded on null-or-empty and used.
/// </summary>
internal static class EnvVarFallback
{
    /// <summary>
    /// Applies the fallback for a single option when no explicit value was provided.
    /// </summary>
    /// <param name="result">Parse result to append to.</param>
    /// <param name="option">Option that may declare an environment variable.</param>
    /// <param name="requiredOnly">
    /// When true, only options marked <see cref="SubCommandOptionInfo.IsRequired"/> are considered.
    /// </param>
    /// <returns>True when a value was applied from the environment.</returns>
    internal static bool Apply(ParseResult result, SubCommandOptionInfo option, bool requiredOnly)
    {
        if (requiredOnly && !option.IsRequired)
            return false;

        if (string.IsNullOrEmpty(option.EnvironmentVariable))
            return false;

        if (result.OptionValues.TryGetValue(option, out var existing) && existing.Count != 0)
            return false;

        // Single read: guard and use the same value, matching the original single-lookup.
        var envValue = Environment.GetEnvironmentVariable(option.EnvironmentVariable);
        if (string.IsNullOrEmpty(envValue))
            return false;

        result.AddOptionValue(option, envValue);
        return true;
    }
}
