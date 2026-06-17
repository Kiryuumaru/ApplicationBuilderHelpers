using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Shared.Exceptions;

namespace Application.Server.Authorization.Utilities;

public static class RoleValidationHelper
{
    public static void ValidateRoleParameters(Role role, IReadOnlyDictionary<string, string?>? providedParameters)
    {
        ArgumentNullException.ThrowIfNull(role);

        var requiredParameters = new HashSet<string>(StringComparer.Ordinal);
        foreach (var scopeTemplate in role.ScopeTemplates)
        {
            foreach (var requiredParam in scopeTemplate.RequiredParameters)
            {
                requiredParameters.Add(requiredParam);
            }
        }

        if (requiredParameters.Count == 0)
        {
            return;
        }

        var providedKeys = providedParameters?.Keys ?? [];
        var missingParameters = requiredParameters.Where(p => !providedKeys.Contains(p)).ToList();

        if (missingParameters.Count > 0)
        {
            throw new ValidationException(
                "parameterValues",
                $"Role '{role.Code}' requires parameters [{string.Join(", ", missingParameters)}] but they were not provided.");
        }
    }
}
