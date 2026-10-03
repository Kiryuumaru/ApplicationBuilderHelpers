using System;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// "Did you mean" suggestions for unmatched option or command names.
/// </summary>
internal static class DidYouMean
{
    private const int MaxDistance = 2;

    private const int PrefixBonusMaxDistance = 4;

    /// <summary>
    /// Finds the single best suggestion, or null when nothing is close.
    /// </summary>
    /// <remarks>
    /// Back-compat alias over the fail-closed core for pinned unit coverage;
    /// new call sites must use the <c>SuggestBlamedToken</c>/<c>SuggestSubcommand</c>
    /// emit gates. Fail-closed: byte-identical display tokens never suggest,
    /// distance ties stay silent with no winner picked, and distances 3-4 need
    /// a shared first letter plus a long token. The first-letter bonus only
    /// widens the limit; it never outranks a smaller edit distance. Reserved
    /// <c>help</c>/<c>version</c> names win an exact distance + same-initial
    /// tie; a strictly closer user option still wins.
    /// </remarks>
    internal static string? FindBestMatch(string input, IEnumerable<(string Key, string Display)> candidates) =>
        SuggestCore(input, candidates);

    /// <summary>Builds option candidates.</summary>
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

            if (option.ShouldShowNegation && option.NegatedBareName != null && option.NegatedLongName != null)
                yield return (option.NegatedBareName, option.NegatedLongName);
        }

        foreach (var reserved in ReservedCandidates)
        {
            if (seen.Add(reserved.Key))
                yield return reserved;
        }
    }

    private static readonly (string Key, string Display)[] ReservedCandidates =
    [
        ("help", "--help"),
        ("version", "--version"),
    ];

    private static bool IsReserved(string normalizedKey) =>
        normalizedKey is "help" or "version";

    /// <summary>Finds the single best subcommand-name suggestion among sibling children.</summary>
    internal static IEnumerable<(string Key, string Display)> SubCommandCandidates(IEnumerable<string> names)
    {
        foreach (var name in names)
        {
            if (!string.IsNullOrEmpty(name))
                yield return (name, name);
        }
    }

    /// <summary>Option emit gate: single owner for option-token suggestions.</summary>
    internal static string? SuggestBlamedToken(string input, IEnumerable<(string Key, string Display)> candidates) =>
        SuggestCore(input, candidates);

    /// <summary>Subcommand emit gate: single owner for subcommand-name suggestions.</summary>
    internal static string? SuggestSubcommand(string input, IEnumerable<string> childNames)
    {
        foreach (var child in childNames)
        {
            if (!string.IsNullOrEmpty(child) && string.Equals(child, input, StringComparison.Ordinal))
                return null;
        }

        return SuggestCore(input, SubCommandCandidates(childNames));
    }

    /// <summary>Fail-closed core: byte-identical tokens never suggest, distance ties stay silent.</summary>
    /// <remarks>
    /// Ties stay silent by counting every best-distance match and returning
    /// null; the loop purposefully tracks no tie winner, so each tied
    /// candidate only increments the count. Whole-query exact suppression
    /// (byte-identical raw input only) is symmetric across both gates: option
    /// display tokens ('--name') and subcommand names ('name') share the raw
    /// argv token shape, so case variants like '--VERBOSITY' vs '--verbosity'
    /// keep a distance-0 self-echo instead of leaking a sibling.
    /// Reserved help/version wins an exact distance + same-initial tie; a
    /// strictly closer user option still wins (#635 preservation).
    /// </remarks>
    private static string? SuggestCore(string input, IEnumerable<(string Key, string Display)> candidates)
    {
        var normalizedInput = Normalize(input);
        if (string.IsNullOrEmpty(normalizedInput))
            return null;

        var bestDistance = int.MaxValue;
        var bestCount = 0;

        foreach (var (key, display) in candidates)
        {
            var normalizedKey = Normalize(key);
            if (string.IsNullOrEmpty(normalizedKey))
                continue;
            if (string.Equals(display, input, StringComparison.Ordinal))
                continue;

            var prefix = normalizedInput[0] == normalizedKey[0];
            var distance = DamerauLevenshtein(normalizedInput, normalizedKey);

            var limit = prefix ? PrefixBonusMaxDistance : MaxDistance;
            if (distance > limit)
                continue;
            if (distance > MaxDistance && Math.Max(normalizedInput.Length, normalizedKey.Length) < 6)
                continue;

            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestCount = 1;
            }
            else if (distance == bestDistance)
            {
                bestCount++;
            }
        }

        if (bestCount != 1)
        {
            // Single exception to tie-silence: same-distance + same-initial
            // ties resolve to reserved help/version (#635). All other ties
            // stay silent (fail-closed #601). A strictly closer distance
            // never reaches this path (bestCount tracks only bestDistance).
            // Note: reserved preference applies only when the tied reserved
            // candidate itself shares the best distance (BreakReservedTie
            // re-checks distance); e.g. --versoin d(version)=1 beats
            // d(verbose)=3 outright and needs no tie-break.
            var tieWinner = BreakReservedTie(input, candidates, bestDistance);
            return tieWinner;
        }

        string? best = null;
        var bestPrefix = false;
        string? bestKey = null;

        foreach (var (key, display) in candidates)
        {
            var normalizedKey = Normalize(key);
            if (string.IsNullOrEmpty(normalizedKey))
                continue;
            if (string.Equals(display, input, StringComparison.Ordinal))
                continue;

            var prefix = normalizedInput[0] == normalizedKey[0];
            var distance = DamerauLevenshtein(normalizedInput, normalizedKey);

            var limit = prefix ? PrefixBonusMaxDistance : MaxDistance;
            if (distance > limit)
                continue;
            if (distance > MaxDistance && Math.Max(normalizedInput.Length, normalizedKey.Length) < 6)
                continue;
            if (distance != bestDistance)
                continue;

            var isBetter = best == null
                || (prefix && !bestPrefix)
                || (prefix == bestPrefix && string.Compare(normalizedKey, bestKey, StringComparison.Ordinal) < 0);

            if (isBetter)
            {
                best = display;
                bestPrefix = prefix;
                bestKey = normalizedKey;
            }
        }

        return best;
    }

    /// <summary>Reserved tie-break (#635): exact distance + same-initial ties resolve to help/version.</summary>
    private static string? BreakReservedTie(string input, IEnumerable<(string Key, string Display)> candidates, int bestDistance)
    {
        if (bestDistance == int.MaxValue)
            return null;

        var normalizedInput = Normalize(input);
        if (string.IsNullOrEmpty(normalizedInput))
            return null;

        string? reservedBest = null;
        var reservedCount = 0;

        foreach (var (key, display) in candidates)
        {
            var normalizedKey = Normalize(key);
            if (string.IsNullOrEmpty(normalizedKey))
                continue;
            if (string.Equals(display, input, StringComparison.Ordinal))
                continue;
            if (!IsReserved(normalizedKey))
                continue;

            var prefix = normalizedInput[0] == normalizedKey[0];
            if (!prefix)
                continue;
            var distance = DamerauLevenshtein(normalizedInput, normalizedKey);
            if (distance != bestDistance)
                continue;

            var limit = PrefixBonusMaxDistance;
            if (distance > limit)
                continue;
            if (distance > MaxDistance && Math.Max(normalizedInput.Length, normalizedKey.Length) < 6)
                continue;

            reservedCount++;
            reservedBest = display;
        }

        // Exactly one reserved candidate shares the best distance: that is the
        // #635 pinned case (--versoin/--ver rows). Zero or 2+ reserved sharers
        // keep fail-closed silence.
        return reservedCount == 1 ? reservedBest : null;
    }

    /// <summary>Appends the suggestion hint to a message when one exists.</summary>
    internal static string WithSuggestion(string message, string? suggestion) =>
        suggestion == null ? message : $"{message}. Did you mean '{suggestion}'?";

    /// <summary>Normalizes a token for comparison.</summary>
    internal static string Normalize(string value)
    {
        var token = value;
        var equals = token.IndexOf('=');
        if (equals >= 0)
            token = token[..equals];
        return token.TrimStart('-').ToLowerInvariant();
    }

    /// <summary>True Damerau-Levenshtein distance with adjacent transposition.</summary>
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
