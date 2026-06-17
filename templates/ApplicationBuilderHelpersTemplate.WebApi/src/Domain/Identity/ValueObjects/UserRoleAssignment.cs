using System.Collections.ObjectModel;
using Domain.Shared.Constants;
using Domain.Shared.Models;

namespace Domain.Identity.ValueObjects;

/// <summary>
/// Represents a role assigned to a user with optional parameter bindings.
/// </summary>
public sealed class UserRoleAssignment : ValueObject
{
    public Guid RoleId { get; }
    public IReadOnlyDictionary<string, string?> ParameterValues { get; }

    private UserRoleAssignment(Guid roleId, IReadOnlyDictionary<string, string?> parameterValues)
    {
        RoleId = roleId;
        ParameterValues = parameterValues;
    }

    public static UserRoleAssignment Create(Guid roleId, IReadOnlyDictionary<string, string?>? parameterValues = null)
        => new(roleId, NormalizeParameters(parameterValues));

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return RoleId;
        foreach (var kvp in ParameterValues.OrderBy(static pair => pair.Key, StringComparer.Ordinal))
        {
            yield return kvp.Key;
            yield return kvp.Value ?? string.Empty;
        }
    }

    private static IReadOnlyDictionary<string, string?> NormalizeParameters(IReadOnlyDictionary<string, string?>? parameterValues)
    {
        if (parameterValues is null || parameterValues.Count == 0)
        {
            return EmptyCollections.StringNullableStringDictionary;
        }

        var normalized = new Dictionary<string, string?>(StringComparer.Ordinal);
        foreach (var kvp in parameterValues)
        {
            if (string.IsNullOrWhiteSpace(kvp.Key))
            {
                continue;
            }

            var key = kvp.Key.Trim();
            if (normalized.ContainsKey(key))
            {
                continue;
            }

            normalized[key] = kvp.Value?.Trim();
        }

        return normalized.Count == 0
            ? EmptyCollections.StringNullableStringDictionary
            : new ReadOnlyDictionary<string, string?>(normalized);
    }
}
