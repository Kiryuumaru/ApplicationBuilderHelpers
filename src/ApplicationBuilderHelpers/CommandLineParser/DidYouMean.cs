using System;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Industry-standard "did you mean" suggestions for CLI dead-ends.
/// Damerau-Levenshtein (optimal string alignment), case-insensitive,
/// dash-stripped, single best match, silent on far-miss.
/// </summary>
internal static class DidYouMean
{
    private const int MaxDistance = 2;

    /// <summary>
    /// Extended bound for the same-first-character prefix bonus. The prefix
    /// path is intentionally capped so far misses stay silent even when they
    /// share an initial character (e.g. "--verbosity-garbage-xyz").
    /// </summary>
    private const int PrefixBonusMaxDistance = 4;

    /// <summary>
    /// Finds the single best suggestion for <paramref name="input"/> among
    /// <paramref name="candidates"/>, or null when nothing is close.
    /// Each candidate is a (normalized comparison key, display text) pair.
    /// Admission rule: distance within <see cref="MaxDistance"/> suggests;
    /// a shared normalized first character (prefix bonus) extends admission
    /// to <see cref="PrefixBonusMaxDistance"/>; anything farther stays silent.
    /// Ties break by prefix match, then alphabetically.
    /// </summary>
    internal static string? FindBestMatch(string input, IEnumerable<(string Key, string Display)> candidates)
    {
        var normalizedInput = Normalize(input);
        if (string.IsNullOrEmpty(normalizedInput))
            return null;

        string? best = null;
        var bestDistance = int.MaxValue;
        var bestPrefix = false;
        string? bestKey = null;

        foreach (var (key, display) in candidates)
        {
            var normalizedKey = Normalize(key);
            if (string.IsNullOrEmpty(normalizedKey))
                continue;

            var prefix = normalizedInput[0] == normalizedKey[0];
            var distance = DamerauLevenshtein(normalizedInput, normalizedKey);

            var limit = prefix ? PrefixBonusMaxDistance : MaxDistance;
            if (distance > limit)
                continue;

            var isBetter = best == null
                || distance < bestDistance
                || (distance == bestDistance && prefix && !bestPrefix)
                || (distance == bestDistance && prefix == bestPrefix && string.Compare(normalizedKey, bestKey, StringComparison.Ordinal) < 0);

            if (isBetter)
            {
                best = display;
                bestDistance = distance;
                bestPrefix = prefix;
                bestKey = normalizedKey;
            }
        }

        return best;
    }

    /// <summary>
    /// Builds option candidates: "--long-name" display form,
    /// falling back to "-s" short form.
    /// </summary>
    internal static IEnumerable<(string Key, string Display)> OptionCandidates(IEnumerable<SubCommandOptionInfo> options)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var option in options)
        {
            string? key = option.LongName ?? option.ShortName?.ToString();
            if (string.IsNullOrEmpty(key) || !seen.Add(key))
                continue;

            var display = option.LongName != null ? $"--{option.LongName}" : $"-{option.ShortName}";
            yield return (key, display);
        }
    }

    /// <summary>
    /// Builds subcommand candidates from child command names.
    /// </summary>
    internal static IEnumerable<(string Key, string Display)> SubCommandCandidates(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!string.IsNullOrEmpty(name))
                yield return (name, name);
        }
    }

    /// <summary>
    /// Appends " Did you mean 'x'?" to a message when a suggestion exists.
    /// </summary>
    internal static string WithSuggestion(string message, string? suggestion) =>
        suggestion == null ? message : $"{message}. Did you mean '{suggestion}'?";

    /// <summary>
    /// Normalizes a token for comparison: strips any "=value" suffix and
    /// leading dashes, lowercases.
    /// </summary>
    internal static string Normalize(string value)
    {
        var token = value;
        var equals = token.IndexOf('=');
        if (equals >= 0)
            token = token[..equals];
        return token.TrimStart('-').ToLowerInvariant();
    }

    /// <summary>
    /// True Damerau-Levenshtein distance with adjacent transposition: a pure
    /// adjacent transposition costs exactly 1. Uses the full matrix so the
    /// transposition term reads d[i-2][j-2] + 1 (no cost substitution).
    /// Inputs must already be normalized.
    /// </summary>
    internal static int DamerauLevenshtein(string source, string target)
    {
        if (source.Length == 0)
            return target.Length;
        if (target.Length == 0)
            return source.Length;

        var d = new int[source.Length + 1, target.Length + 1];

        for (var i = 0; i <= source.Length; i++)
            d[i, 0] = i;
        for (var j = 0; j <= target.Length; j++)
            d[0, j] = j;

        for (var i = 1; i <= source.Length; i++)
        {
            for (var j = 1; j <= target.Length; j++)
            {
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(
                    Math.Min(d[i, j - 1] + 1, d[i - 1, j] + 1),
                    d[i - 1, j - 1] + cost);

                if (i > 1 && j > 1 && source[i - 1] == target[j - 2] && source[i - 2] == target[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }

        return d[source.Length, target.Length];
    }
}
