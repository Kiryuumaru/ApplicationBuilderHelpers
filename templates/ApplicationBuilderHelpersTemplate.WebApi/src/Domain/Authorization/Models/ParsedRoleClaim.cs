using Domain.Shared.Constants;

namespace Domain.Authorization.Models;

/// <summary>
/// Represents a parsed role claim in the format "CODE;param1=value1;param2=value2".
/// </summary>
/// <param name="Original">The original claim string.</param>
/// <param name="Code">The role code (uppercase, normalized).</param>
/// <param name="Parameters">The parameter key-value pairs.</param>
public readonly record struct ParsedRoleClaim(
    string Original,
    string Code,
    IReadOnlyDictionary<string, string> Parameters)
{
    /// <summary>
    /// Gets whether this role claim has any parameters.
    /// </summary>
    public bool HasParameters => Parameters.Count > 0;

    /// <summary>
    /// Formats the role claim back to string: "CODE;param1=value1;param2=value2".
    /// </summary>
    public override string ToString()
    {
        if (Parameters.Count == 0)
        {
            return Code;
        }

        var parts = new List<string>(Parameters.Count + 1) { Code };
        foreach (var (key, value) in Parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            parts.Add($"{key}={value}");
        }

        return string.Join(';', parts);
    }

    /// <summary>
    /// Returns an empty ParsedRoleClaim instance.
    /// </summary>
    public static ParsedRoleClaim Empty => new(string.Empty, string.Empty, EmptyCollections.StringStringDictionary);
}
