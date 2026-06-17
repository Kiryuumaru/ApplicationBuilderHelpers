using Application.LocalStore.Interfaces.Inbound;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace Application.LocalStore.Extensions;

/// <summary>
/// Extension methods for <see cref="IConcurrentLocalStore"/> to store and retrieve typed values.
/// Supports all primitive types, string, DateTime, DateTimeOffset, TimeSpan, Guid, and any type
/// implementing <see cref="IFormattable"/> (for Set) or <see cref="IParsable{T}"/> (for Get).
/// </summary>
public static class ConcurrentLocalStoreExtensions
{
    /// <summary>
    /// Stores a boolean value.
    /// </summary>
    public static Task Set(this IConcurrentLocalStore store, string id, bool value, CancellationToken cancellationToken = default)
        => store.Set(id, value ? bool.TrueString : bool.FalseString, cancellationToken);

    /// <summary>
    /// Stores a value that implements <see cref="IFormattable"/>.
    /// Covers: byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal,
    /// char, DateTime, DateTimeOffset, TimeSpan, Guid, and custom IFormattable types.
    /// </summary>
    public static Task Set<T>(this IConcurrentLocalStore store, string id, T value, CancellationToken cancellationToken = default)
        where T : IFormattable
    {
        var format = GetFormat(value);
        var formatted = value.ToString(format, CultureInfo.InvariantCulture);
        return store.Set(id, formatted, cancellationToken);
    }

    /// <summary>
    /// Retrieves a value that implements <see cref="IParsable{T}"/>.
    /// Covers: bool, byte, sbyte, short, ushort, int, uint, long, ulong, float, double, decimal,
    /// char, DateTime, DateTimeOffset, TimeSpan, Guid, and any custom IParsable types (struct or class).
    /// </summary>
    public static async Task<T?> Get<T>(this IConcurrentLocalStore store, string id, CancellationToken cancellationToken = default)
        where T : IParsable<T>
    {
        var stored = await store.Get(id, cancellationToken).ConfigureAwait(false);
        return stored is null ? default : T.Parse(stored, CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Retrieves a JSON-serialized object using the provided <see cref="JsonTypeInfo{T}"/>.
    /// </summary>
    public static async Task<T?> Get<T>(this IConcurrentLocalStore store, string id, JsonTypeInfo<T?> jsonTypeInfo, CancellationToken cancellationToken = default)
        where T : class
    {
        var payload = await store.Get(id, cancellationToken).ConfigureAwait(false);
        return string.IsNullOrEmpty(payload) ? default : JsonSerializer.Deserialize(payload, jsonTypeInfo);
    }

    /// <summary>
    /// Stores an object as JSON using the provided <see cref="JsonTypeInfo{T}"/>.
    /// </summary>
    public static Task Set<T>(this IConcurrentLocalStore store, string id, T? obj, JsonTypeInfo<T?> jsonTypeInfo, CancellationToken cancellationToken = default)
        where T : class
    {
        var data = JsonSerializer.Serialize(obj, jsonTypeInfo);
        return store.Set(id, data, cancellationToken);
    }

    /// <summary>
    /// Gets the appropriate format string for round-trip serialization of the given value.
    /// </summary>
    private static string? GetFormat<T>(T value) => value switch
    {
        float or double or Half => "R",    // Round-trip for floating point precision
        DateTime or DateTimeOffset => "O", // ISO 8601 round-trip
        TimeSpan => "c",                   // Constant (invariant) format
        TimeOnly => "O",                   // Round-trip format for TimeOnly
        Guid => "D",                       // Standard GUID format with hyphens
        _ => null                          // Default format for integers, bool, decimal, char, etc.
    };
}
