using System;
using System.Diagnostics.CodeAnalysis;

namespace Application.LocalStore.Utilities;

internal static class LocalStoreKey
{
    public const int MaxGroupLength = 128;
    public const int MaxIdentifierLength = 512;
    private const string StorageSeparator = "__";

    public static string NormalizeGroup(string group)
    {
        if (string.IsNullOrWhiteSpace(group))
        {
            throw new ArgumentException("Group cannot be null or whitespace.", nameof(group));
        }

        var normalized = group.Trim();
        if (normalized.Length > MaxGroupLength)
        {
            throw new ArgumentException($"Group '{normalized}' exceeds the maximum length of {MaxGroupLength} characters.", nameof(group));
        }

        return normalized;
    }

    public static string NormalizeId(string id)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            throw new ArgumentException("Identifier cannot be null or whitespace.", nameof(id));
        }

        var normalized = id.Trim();
        if (normalized.Length > MaxIdentifierLength)
        {
            throw new ArgumentException($"Identifier exceeds the maximum length of {MaxIdentifierLength} characters.", nameof(id));
        }

        return normalized;
    }

    public static void NormalizePair(ref string group, ref string id)
    {
        group = NormalizeGroup(group);
        id = NormalizeId(id);
    }

    public static string BuildStorageKey(string group, string id)
    {
        return $"{id}{StorageSeparator}{group}";
    }

    public static bool TryExtractIdFromStorageKey(string storageKey, string group, [NotNullWhen(true)] out string? id)
    {
        var suffix = $"{StorageSeparator}{group}";
        if (!storageKey.EndsWith(suffix, StringComparison.Ordinal))
        {
            id = null;
            return false;
        }

        var extracted = storageKey[..^suffix.Length];
        if (string.IsNullOrEmpty(extracted))
        {
            id = null;
            return false;
        }

        id = extracted;
        return true;
    }
}
