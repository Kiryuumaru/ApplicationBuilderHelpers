using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Domain.Authorization.Entities;
using Domain.Authorization.Models;
using Domain.Shared.Exceptions;
using Domain.Shared.Models;

namespace Domain.Authorization.ValueObjects;

public class RolePermissionTemplate : ValueObject
{
    private static readonly Regex PlaceholderRegex = new("\\{([a-zA-Z0-9_]+)\\}", RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public string IdentifierTemplate { get; }
    public string? Description { get; }
    public IReadOnlyCollection<string> RequiredParameters { get; }

    protected RolePermissionTemplate(string template, IEnumerable<string>? requiredParameters, string? description)
    {
        IdentifierTemplate = NormalizeTemplate(template);
        Description = string.IsNullOrWhiteSpace(description) ? null : description.Trim();

        var parameters = requiredParameters?
            .Where(static parameter => !string.IsNullOrWhiteSpace(parameter))
            .Select(static parameter => parameter.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static parameter => parameter, StringComparer.Ordinal)
            .ToArray() ?? Array.Empty<string>();

        var placeholders = ExtractPlaceholders(IdentifierTemplate);
        if (parameters.Length == 0 && placeholders.Count > 0)
        {
            parameters = [.. placeholders
                .Distinct(StringComparer.Ordinal)
                .OrderBy(static parameter => parameter, StringComparer.Ordinal)];
        }

        RequiredParameters = parameters;
        ValidatePlaceholderConfiguration(IdentifierTemplate, placeholders, RequiredParameters);
    }

    public static RolePermissionTemplate Create(string template, IEnumerable<string>? requiredParameters = null, string? description = null)
        => new(template, requiredParameters, description);

    public static RolePermissionTemplate FromPermission(Permission permission, IReadOnlyDictionary<string, string?>? parameters = null, string? description = null)
    {
        ArgumentNullException.ThrowIfNull(permission);
        var identifier = permission.BuildPath(parameters);
        return Create(identifier, requiredParameters: null, description ?? permission.Description);
    }

    public bool RequiresParameters => RequiredParameters.Count > 0;

    public string Expand(IReadOnlyDictionary<string, string?>? parameterValues)
    {
        if (RequiresParameters && (parameterValues is null || parameterValues.Count == 0))
        {
            throw new DomainException($"Role permission template '{IdentifierTemplate}' requires parameters [{string.Join(", ", RequiredParameters)}].");
        }

        var resolved = IdentifierTemplate;
        if (RequiredParameters.Count > 0)
        {
            ArgumentNullException.ThrowIfNull(parameterValues);

            foreach (var parameter in RequiredParameters)
            {
                if (!parameterValues.TryGetValue(parameter, out var value) || string.IsNullOrWhiteSpace(value))
                {
                    throw new DomainException($"Missing value for role permission parameter '{parameter}'.");
                }

                var placeholder = "{" + parameter + "}";
                resolved = resolved.Replace(placeholder, value.Trim(), StringComparison.Ordinal);
            }
        }

        if (resolved.IndexOf('{') >= 0 || resolved.IndexOf('}') >= 0)
        {
            throw new DomainException($"Role permission template '{IdentifierTemplate}' contains unresolved parameters.");
        }

        var parsed = Permission.ParseIdentifier(resolved);
        return parsed.Identifier;
    }

    public IEnumerable<string> ExpandMany(IEnumerable<IReadOnlyDictionary<string, string?>> parameterSets)
    {
        ArgumentNullException.ThrowIfNull(parameterSets);
        foreach (var parameters in parameterSets)
        {
            yield return Expand(parameters);
        }
    }

    public bool ContainsPlaceholder(string parameterName)
        => RequiredParameters.Contains(parameterName, StringComparer.Ordinal);

    protected override IEnumerable<object> GetEqualityComponents()
    {
        yield return IdentifierTemplate;
        yield return Description ?? string.Empty;
        foreach (var parameter in RequiredParameters)
        {
            yield return parameter;
        }
    }

    private static string NormalizeTemplate(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            throw new DomainException("Permission template cannot be null or empty.");
        }

        var trimmed = template.Trim();
        if (!PlaceholderRegex.IsMatch(trimmed))
        {
            // No placeholders; validate immediately
            return Permission.ParseIdentifier(trimmed).Canonical;
        }

        return trimmed;
    }

    private static IReadOnlyCollection<string> ExtractPlaceholders(string template)
    {
        if (string.IsNullOrWhiteSpace(template))
        {
            return Array.Empty<string>();
        }

        var matches = PlaceholderRegex.Matches(template);
        if (matches.Count == 0)
        {
            return Array.Empty<string>();
        }

        return [.. matches
            .Select(static match => match.Groups[1].Value)
            .Where(static name => !string.IsNullOrWhiteSpace(name))
            .Select(static name => name.Trim())
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static name => name, StringComparer.Ordinal)];
    }

    private static void ValidatePlaceholderConfiguration(
        string template,
        IReadOnlyCollection<string> placeholders,
        IReadOnlyCollection<string> requiredParameters)
    {
        if (placeholders.Count == 0)
        {
            if (requiredParameters.Count > 0)
            {
                throw new DomainException(
                    $"Permission template '{template}' does not allow parameters [{string.Join(", ", requiredParameters)}].");
            }

            return;
        }

        if (requiredParameters.Count == 0)
        {
            throw new DomainException(
                $"Permission template '{template}' requires parameters [{string.Join(", ", placeholders)}].");
        }

        var placeholderSet = new HashSet<string>(placeholders, StringComparer.Ordinal);
        var parameterSet = new HashSet<string>(requiredParameters, StringComparer.Ordinal);

        var missing = placeholderSet.Where(placeholder => !parameterSet.Contains(placeholder)).ToArray();
        if (missing.Length > 0)
        {
            throw new DomainException(
                $"Permission template '{template}' is missing required parameters [{string.Join(", ", missing)}].");
        }

        var extras = parameterSet.Where(parameter => !placeholderSet.Contains(parameter)).ToArray();
        if (extras.Length > 0)
        {
            throw new DomainException(
                $"Permission template '{template}' does not use provided parameters [{string.Join(", ", extras)}].");
        }
    }
}
