using System.Collections.ObjectModel;

namespace Domain.Shared.Constants;

/// <summary>
/// Shared empty collection instances to avoid redundant allocations.
/// Add more as needed for your domain types.
/// </summary>
public static class EmptyCollections
{
    /// <summary>
    /// Empty read-only list of strings.
    /// </summary>
    public static IReadOnlyList<string> StringList { get; } = Array.Empty<string>();

    /// <summary>
    /// Empty read-only list of Guids.
    /// </summary>
    public static IReadOnlyList<Guid> GuidList { get; } = Array.Empty<Guid>();

    /// <summary>
    /// Empty dictionary of string to string.
    /// </summary>
    public static IReadOnlyDictionary<string, string> StringStringDictionary { get; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0, StringComparer.Ordinal));

    /// <summary>
    /// Empty dictionary of string to nullable string.
    /// </summary>
    public static IReadOnlyDictionary<string, string?> StringNullableStringDictionary { get; } =
        new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>(0, StringComparer.Ordinal));
}
