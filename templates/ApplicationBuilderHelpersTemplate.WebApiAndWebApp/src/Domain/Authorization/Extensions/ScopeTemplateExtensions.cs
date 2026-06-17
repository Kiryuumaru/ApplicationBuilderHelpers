using Domain.Authorization.ValueObjects;

namespace Domain.Authorization.Extensions;

/// <summary>
/// Extension methods for working with scope templates.
/// </summary>
public static class ScopeTemplateExtensions
{
    /// <summary>
    /// Expands all scope templates into scope directives using the provided parameter values.
    /// </summary>
    /// <param name="scopeTemplates">The scope templates to expand.</param>
    /// <param name="parameterValues">The parameter values to use for expansion.</param>
    /// <returns>A collection of expanded scope directives.</returns>
    public static IReadOnlyCollection<ScopeDirective> ExpandToDirectives(
        this IEnumerable<ScopeTemplate> scopeTemplates,
        IReadOnlyDictionary<string, string?> parameterValues)
    {
        ArgumentNullException.ThrowIfNull(scopeTemplates);
        ArgumentNullException.ThrowIfNull(parameterValues);

        return [.. scopeTemplates
            .Select(template => template.Expand(parameterValues))
            .Distinct()
            .OrderBy(static directive => directive.ToString(), StringComparer.Ordinal)];
    }
}
