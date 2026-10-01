using System;

namespace ApplicationBuilderHelpers.Attributes;

/// <summary>
/// Declares a named-option binding for one command property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CommandOptionAttribute : Attribute
{
    /// <summary>
    /// Declares an option reachable by both short and long flags.
    /// </summary>
    /// <param name="shortTerm">The single-character short flag (e.g. <c>v</c> for <c>-v</c>).</param>
    /// <param name="term">The long flag name (e.g. <c>"verbose"</c> for <c>--verbose</c>).</param>
    public CommandOptionAttribute(char shortTerm, string term)
    {
        ShortTerm = shortTerm;
        Term = term;
    }

    /// <summary>
    /// Declares a short-only option; see the canonical overload for the short-plus-long form.
    /// </summary>
    /// <param name="shortTerm">The single-character short flag (e.g. <c>v</c> for <c>-v</c>).</param>
    public CommandOptionAttribute(char shortTerm)
    {
        ShortTerm = shortTerm;
        Term = null;
    }

    /// <summary>
    /// Declares a long-only option, except a single-letter long also answers its single-dash alias unless reserved; see the canonical overload for the short-plus-long form.
    /// </summary>
    /// <param name="term">The long flag name (e.g. <c>"verbose"</c> for <c>--verbose</c>).</param>
    public CommandOptionAttribute(string term)
    {
        ShortTerm = null;
        Term = term;
    }

    /// <summary>
    /// Gets or sets the long flag name; null (default) exposes the short flag only.
    /// </summary>
    public string? Term { get; set; }

    /// <summary>
    /// Gets or sets the single-character short flag; null (default) exposes the long flag only, except a single-letter long also answers its single-dash alias.
    /// </summary>
    public char? ShortTerm { get; set; }

    /// <summary>
    /// Gets or sets the environment variable fallback name; null (default) disables env fallback.
    /// </summary>
    public string? EnvironmentVariable { get; set; }

    /// <summary>
    /// Gets or sets whether omission fails binding; true reports <c>MissingRequired</c> with exit 2.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the help text shown for the option.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the allowed values; empty (default) accepts any convertible value.
    /// </summary>
    public object[] FromAmong { get; set; } = [];

    /// <summary>
    /// Gets or sets whether <c>FromAmong</c> matching is ordinal; true requires exact case.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;

    /// <summary>
    /// Gets or sets whether the value is secret; true never echoes the value.
    /// </summary>
    public bool Secret { get; set; } = false;
}
