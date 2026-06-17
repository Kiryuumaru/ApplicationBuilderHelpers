using System;

namespace Presentation.WebApp.Server.Attributes;

/// <summary>
/// Declares a named placeholder that is substituted into permission identifiers for attribute-based authorization checks.
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
public sealed class PermissionParameterAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="PermissionParameterAttribute"/> class with the required parameter name.
    /// </summary>
    /// <param name="name">Parameter placeholder that should be substituted into the permission string.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is null or whitespace.</exception>
    public PermissionParameterAttribute(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException("Permission parameter name cannot be null or whitespace.", nameof(name));
        }

        Name = name.Trim();
    }

    /// <summary>
    /// Gets the placeholder name expected by the permission definition.
    /// </summary>
    public string Name { get; }
}
