using System.Collections.ObjectModel;
using Domain.Authorization.Entities;
using Domain.Shared.Constants;

namespace Domain.Identity.Models;

/// <summary>
/// Represents a resolved role assignment with parameter values.
/// </summary>
public sealed class UserRoleResolution(Role role, IReadOnlyDictionary<string, string?>? parameterValues = null)
{
    public Role Role { get; } = role ?? throw new ArgumentNullException(nameof(role));
    public IReadOnlyDictionary<string, string?> ParameterValues { get; } = NormalizeParameters(parameterValues);
    public string Code => Role.Code;
    public string ToFormattedClaim() => Role.FormatRoleClaim(Role.Code, ParameterValues);

    private static IReadOnlyDictionary<string, string?> NormalizeParameters(IReadOnlyDictionary<string, string?>? parameterValues)
    {
        if (parameterValues is null || parameterValues.Count == 0)
        {
            return EmptyCollections.StringNullableStringDictionary;
        }

        if (parameterValues is ReadOnlyDictionary<string, string?>)
        {
            return parameterValues;
        }

        var dictionary = new Dictionary<string, string?>(parameterValues.Count, StringComparer.Ordinal);
        foreach (var pair in parameterValues)
        {
            dictionary[pair.Key] = pair.Value;
        }

        return new ReadOnlyDictionary<string, string?>(dictionary);
    }
}
