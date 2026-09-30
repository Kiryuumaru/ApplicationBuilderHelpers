using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal class ParseResult
{
    public SubCommandInfo TargetCommand { get; set; } = null!;
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public Dictionary<SubCommandOptionInfo, List<string>> OptionValues { get; set; } = [];
    public Dictionary<SubCommandArgumentInfo, List<string>> ArgumentValues { get; set; } = [];

    internal HashSet<string> BareOptionOccurrences { get; } = new(StringComparer.Ordinal);

    /// <summary>Explicit valued occurrence counts per canonical key. Flags and env fallback never count.</summary>
    internal Dictionary<string, int> ValuedOptionOccurrenceCounts { get; } = new(StringComparer.Ordinal);

    /// <summary>Adds an option value. Collections accumulate; scalars keep the last value. A later value clears the bare mark.</summary>
    internal void AddOptionValue(SubCommandOptionInfo option, string? value, bool isExplicit = true)
    {
        if (value != null)
        {
            var key = GetCanonicalOptionKey(option);
            if (isExplicit && !option.IsFlag && !option.IsCollection)
            {
                if (ValuedOptionOccurrenceCounts.TryGetValue(key, out var count))
                    ValuedOptionOccurrenceCounts[key] = count + 1;
                else
                    ValuedOptionOccurrenceCounts[key] = 1;
            }

            if (!option.IsCollection)
            {
                foreach (var storedOption in OptionValues.Keys
                    .Where(o => string.Equals(GetCanonicalOptionKey(o), key, StringComparison.Ordinal))
                    .ToList())
                {
                    OptionValues.Remove(storedOption);
                }
            }

            BareOptionOccurrences.Remove(key);
        }

        if (!OptionValues.ContainsKey(option))
            OptionValues[option] = [];

        if (value != null)
            OptionValues[option].Add(value);
        else if (!option.IsFlag)
            BareOptionOccurrences.Add(GetCanonicalOptionKey(option));
    }

    internal static string GetCanonicalOptionKey(SubCommandOptionInfo option) =>
        option.LongName ?? option.ShortName?.ToString() ?? option.Property.Name;

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

    internal bool TryGetCanonicalIdentityOption(string canonicalKey, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out SubCommandOptionInfo option)
    {
        // Prefer the target command's own copy so global copies report one stable name.
        if (TargetCommand is not null)
        {
            foreach (var candidate in TargetCommand.AllOptions)
            {
                if (string.Equals(GetCanonicalOptionKey(candidate), canonicalKey, StringComparison.Ordinal))
                {
                    option = candidate;
                    return true;
                }
            }
        }

        foreach (var storedOption in OptionValues.Keys)
        {
            if (string.Equals(GetCanonicalOptionKey(storedOption), canonicalKey, StringComparison.Ordinal))
            {
                option = storedOption;
                return true;
            }
        }

        option = null!;
        return false;
    }
}
