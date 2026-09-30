using System;

namespace ApplicationBuilderHelpers.Attributes;

/// <summary>
/// Declares a positional argument binding for one command property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CommandArgumentAttribute : Attribute
{
    /// <summary>
    /// Declares an argument with a null name; the property name supplies the display name.
    /// </summary>
    public CommandArgumentAttribute()
    {
    }

    /// <summary>
    /// Declares an argument with the given display/FromAmong name.
    /// </summary>
    /// <param name="name">The argument display name.</param>
    public CommandArgumentAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets or sets the argument display name; null (default) uses the property name.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the help text shown for the argument.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the zero-based binding position; default 0 binds first.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets whether omission fails binding; true reports <c>MissingRequired</c> with exit 2.
    /// </summary>
    public bool Required { get; set; } = false;

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
