using System;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Result of parsing command line arguments
/// </summary>
internal class ParseResult
{
    public SubCommandInfo TargetCommand { get; set; } = null!;
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public Dictionary<SubCommandOptionInfo, List<string>> OptionValues { get; set; } = [];
    public Dictionary<SubCommandArgumentInfo, List<string>> ArgumentValues { get; set; } = [];

    /// <summary>
    /// Adds an option value to the parse result
    /// </summary>
    internal void AddOptionValue(SubCommandOptionInfo option, string? value)
    {
        if (!OptionValues.ContainsKey(option))
            OptionValues[option] = [];

        if (value != null)
            OptionValues[option].Add(value);
    }

    /// <summary>
    /// Canonical key for one logical option across global-copy identities:
    /// long name, then short name, then property name (compared case-insensitively).
    /// </summary>
    internal static string GetCanonicalOptionKey(SubCommandOptionInfo option) =>
        option.LongName ?? option.ShortName?.ToString() ?? option.Property.Name;

    /// <summary>
    /// Merged values for one logical option across all copy identities.
    /// The merged list decides CLI-wins: non-empty means a value is present
    /// regardless of which copy identity holds it.
    /// </summary>
    internal bool TryGetMergedOptionValues(SubCommandOptionInfo option, out List<string> values)
    {
        var key = GetCanonicalOptionKey(option);
        values = [];
        foreach (var (storedOption, storedValues) in OptionValues)
        {
            if (string.Equals(GetCanonicalOptionKey(storedOption), key, StringComparison.OrdinalIgnoreCase))
                values.AddRange(storedValues);
        }
        return values.Count != 0;
    }
}
