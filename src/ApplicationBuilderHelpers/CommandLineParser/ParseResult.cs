using System;
using System.Collections.Generic;
using System.Linq;

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
    /// Side-channel occurrence ledger (#470): canonical keys of valued scalars
    /// seen bare (no value token). Separate from the value lists so a bare
    /// occurrence stays visible after a satisfied value; single bare keeps the
    /// #449 sentinel path (env fallback, <c>MissingRequired</c>, omitted).
    /// </summary>
    internal HashSet<string> BareOptionOccurrences { get; } = new(StringComparer.Ordinal);

    /// <summary>
    /// Adds an option value to the parse result. Collections accumulate;
    /// scalars overwrite with the last value (industry last-wins). A later
    /// real value heals a prior bare mark via the same eviction.
    /// </summary>
    internal void AddOptionValue(SubCommandOptionInfo option, string? value)
    {
        if (!option.IsCollection && value != null)
        {
            var key = GetCanonicalOptionKey(option);
            foreach (var storedOption in OptionValues.Keys
                .Where(o => string.Equals(GetCanonicalOptionKey(o), key, StringComparison.Ordinal))
                .ToList())
            {
                OptionValues.Remove(storedOption);
            }
            BareOptionOccurrences.Remove(key);
        }

        if (!OptionValues.ContainsKey(option))
            OptionValues[option] = [];

        if (value != null)
            OptionValues[option].Add(value);
        else if (!option.IsCollection && !option.IsFlag)
            BareOptionOccurrences.Add(GetCanonicalOptionKey(option));
    }

    /// <summary>
    /// Canonical key for one logical option across global-copy identities:
    /// long name, then short name, then property name (compared case-sensitively
    /// so case-distinct long names stay independent).
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
            if (string.Equals(GetCanonicalOptionKey(storedOption), key, StringComparison.Ordinal))
                values.AddRange(storedValues);
        }
        return values.Count != 0;
    }
}
