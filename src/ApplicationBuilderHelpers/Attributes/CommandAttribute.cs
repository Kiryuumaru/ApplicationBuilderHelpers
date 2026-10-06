using System;

namespace ApplicationBuilderHelpers.Attributes;

/// <summary>
/// Declares command identity for one command class.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    /// <summary>
    /// Declares an unnamed command showing only the given description.
    /// </summary>
    /// <param name="description">The help description.</param>
    public CommandAttribute(string? description = null)
    {
        Term = null;
        Description = description;
    }

    /// <summary>
    /// Declares a named command; see the description-only overload for the unnamed form.
    /// </summary>
    /// <param name="name">The space-separated route term.</param>
    /// <param name="description">The help description.</param>
    public CommandAttribute(string name, string? description = null)
    {
        Term = name;
        Description = description;
    }

    /// <summary>
    /// Gets or sets the route term; null (default) leaves the command unnamed.
    /// </summary>
    public string? Term { get; set; }

    /// <summary>
    /// Gets or sets the help description; null (default) omits it.
    /// </summary>
    public string? Description { get; set; }
}
