using System.Collections.ObjectModel;

namespace Domain.Shared.Constants;

public static class EmptyCollections
{
    public static IReadOnlyDictionary<string, string> StringStringDictionary { get; } =
        new ReadOnlyDictionary<string, string>(new Dictionary<string, string>(0, StringComparer.Ordinal));

    public static IReadOnlyDictionary<string, string?> StringNullableStringDictionary { get; } =
        new ReadOnlyDictionary<string, string?>(new Dictionary<string, string?>(0, StringComparer.Ordinal));
}
