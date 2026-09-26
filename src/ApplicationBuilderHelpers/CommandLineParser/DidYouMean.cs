using System;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// "Did you mean" suggestions for unmatched option or command names.
/// True Damerau-Levenshtein, case-insensitive,
/// dash-stripped, single best match, null when nothing is close.
/// </summary>
internal static class DidYouMean
{
    private const int MaxDistance = 2;

    /// <summary>
    /// Larger limit when the first character matches.
    /// </summary>
    private const int PrefixBonusMaxDistance = 4;

    /// <summary>
    /// Finds the single best suggestion for <paramref name="input"/> among
    /// <paramref name="candidates"/>, or null when nothing is close.
    /// Each candidate is a (normalized comparison key, display text) pair.
    /// Suggests when the distance is within <see cref="MaxDistance"/>;
    /// a shared normalized first character extends the limit
    /// to <see cref="PrefixBonusMaxDistance"/>; anything farther returns null.
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
    /// adjacent transposition costs exactly 1. Lowrance-Rader algorithm with
    /// a last-row dictionary so multiple edits on overlapping substrings
    /// (e.g. "CA" vs "ABC") score correctly.
    /// Inputs must already be normalized.
    /// </summary>
    internal static int DamerauLevenshtein(string source, string target)
    {
        if (source.Length == 0)
            return target.Length;
        if (target.Length == 0)
            return source.Length;

        var len1 = source.Length;
        var len2 = target.Length;
        var inf = len1 + len2;
        var d = new int[len1 + 2, len2 + 2];

        d[0, 0] = inf;
        for (var i = 0; i <= len1; i++)
        {
            d[i + 1, 0] = inf;
            d[i + 1, 1] = i;
        }
        for (var j = 0; j <= len2; j++)
        {
            d[0, j + 1] = inf;
            d[1, j + 1] = j;
        }

        var da = new Dictionary<char, int>();
        for (var i = 1; i <= len1; i++)
        {
            var db = 0;
            for (var j = 1; j <= len2; j++)
            {
                var i1 = da.TryGetValue(target[j - 1], out var lastRow) ? lastRow : 0;
                var j1 = db;
                var cost = source[i - 1] == target[j - 1] ? 0 : 1;
                if (cost == 0)
                    db = j;

                d[i + 1, j + 1] = Math.Min(
                    Math.Min(d[i, j + 1] + 1, d[i + 1, j] + 1),
                    Math.Min(d[i, j] + cost, d[i1, j1] + (i - i1 - 1) + 1 + (j - j1 - 1)));
            }
            da[source[i - 1]] = i;
        }

        return d[len1 + 1, len2 + 1];
    }
}
