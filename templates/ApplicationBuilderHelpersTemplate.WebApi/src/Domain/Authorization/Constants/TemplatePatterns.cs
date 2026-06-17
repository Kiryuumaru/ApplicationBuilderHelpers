using System.Text.RegularExpressions;

namespace Domain.Authorization.Constants;

/// <summary>
/// Shared regex patterns for authorization template processing.
/// </summary>
public static partial class TemplatePatterns
{
    /// <summary>
    /// Matches parameter placeholders like <c>{userId}</c> in templates.
    /// </summary>
    public static Regex PlaceholderRegex { get; } = PlaceholderRegexGenerated();

    [GeneratedRegex(@"\{([a-zA-Z0-9_]+)\}", RegexOptions.CultureInvariant)]
    private static partial Regex PlaceholderRegexGenerated();
}
