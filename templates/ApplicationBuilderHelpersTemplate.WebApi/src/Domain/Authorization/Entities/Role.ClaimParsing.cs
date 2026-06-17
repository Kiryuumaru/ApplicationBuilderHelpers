using System.Collections.ObjectModel;
using Domain.Authorization.Models;
using Domain.Shared.Constants;

namespace Domain.Authorization.Entities;

public sealed partial class Role
{
    /// <summary>
    /// Parses a role claim string in the format "CODE;param1=value1;param2=value2".
    /// </summary>
    /// <param name="claim">The role claim string to parse.</param>
    /// <returns>A parsed role claim with code and parameters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when claim is null.</exception>
    /// <exception cref="FormatException">Thrown when the claim format is invalid.</exception>
    public static ParsedRoleClaim ParseRoleClaim(string claim)
    {
        ArgumentNullException.ThrowIfNull(claim);

        var trimmed = claim.Trim();
        if (trimmed.Length == 0)
        {
            throw new FormatException("Role claim cannot be empty.");
        }

        // Check for control characters (security)
        foreach (var c in trimmed)
        {
            if (char.IsControl(c))
            {
                throw new FormatException("Role claim cannot contain control characters.");
            }
        }

        // Format: "CODE;param1=value1;param2=value2" or just "CODE"
        var semicolonIndex = trimmed.IndexOf(';');
        if (semicolonIndex < 0)
        {
            // No parameters - just the code
            var code = NormalizeCode(trimmed);
            return new ParsedRoleClaim(claim, code, EmptyCollections.StringStringDictionary);
        }

        if (semicolonIndex == 0)
        {
            throw new FormatException("Role claim cannot start with a semicolon.");
        }

        // Extract and normalize the code
        var codeStr = trimmed[..semicolonIndex].Trim();
        if (codeStr.Length == 0)
        {
            throw new FormatException("Role code cannot be empty.");
        }

        var normalizedCode = NormalizeCode(codeStr);

        // Parse parameters from remaining semicolon-separated parts
        var paramPart = trimmed[(semicolonIndex + 1)..];
        if (paramPart.Length == 0)
        {
            // Trailing semicolon with no params - treat as no params
            return new ParsedRoleClaim(claim, normalizedCode, EmptyCollections.StringStringDictionary);
        }

        var parameters = new Dictionary<string, string>(StringComparer.Ordinal);
        var assignments = paramPart.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var assignment in assignments)
        {
            var equalsIndex = assignment.IndexOf('=');
            if (equalsIndex < 0)
            {
                throw new FormatException($"Role claim parameter '{assignment}' must use the 'name=value' format.");
            }

            var name = assignment[..equalsIndex].Trim();
            var value = assignment[(equalsIndex + 1)..].Trim();

            if (name.Length == 0)
            {
                throw new FormatException("Role claim parameter names cannot be empty.");
            }

            if (value.Length == 0)
            {
                throw new FormatException($"Role claim parameter '{name}' requires a value.");
            }

            // Last value wins for duplicate keys
            parameters[name] = value;
        }

        return new ParsedRoleClaim(
            claim,
            normalizedCode,
            parameters.Count == 0 ? EmptyCollections.StringStringDictionary : new ReadOnlyDictionary<string, string>(parameters));
    }

    /// <summary>
    /// Attempts to parse a role claim string without throwing exceptions.
    /// </summary>
    /// <param name="claim">The role claim string to parse.</param>
    /// <param name="parsed">When successful, contains the parsed role claim.</param>
    /// <returns><c>true</c> if parsing succeeded; otherwise <c>false</c>.</returns>
    public static bool TryParseRoleClaim(string claim, out ParsedRoleClaim parsed)
    {
        try
        {
            parsed = ParseRoleClaim(claim);
            return true;
        }
        catch
        {
            parsed = new ParsedRoleClaim(string.Empty, string.Empty, EmptyCollections.StringStringDictionary);
            return false;
        }
    }

    /// <summary>
    /// Formats a role code and parameters into a role claim string.
    /// </summary>
    /// <param name="code">The role code.</param>
    /// <param name="parameters">Optional parameter values.</param>
    /// <returns>A formatted role claim string like "CODE;param1=value1;param2=value2".</returns>
    /// <exception cref="ArgumentException">Thrown when code is null or empty.</exception>
    public static string FormatRoleClaim(string code, IReadOnlyDictionary<string, string?>? parameters)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        var normalizedCode = NormalizeCode(code);

        if (parameters is null || parameters.Count == 0)
        {
            return normalizedCode;
        }

        var parts = new List<string>(parameters.Count + 1) { normalizedCode };

        foreach (var (key, value) in parameters.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(value))
            {
                parts.Add($"{key}={value}");
            }
        }

        return string.Join(';', parts);
    }
}
