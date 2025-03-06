using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Attributes;

/// <summary>
/// Attribute to define a command argument for a property.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CommandArgumentAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandArgumentAttribute"/> class.
    /// </summary>
    public CommandArgumentAttribute()
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandArgumentAttribute"/> class with a specified name.
    /// </summary>
    /// <param name="name">The name of the command argument.</param>
    public CommandArgumentAttribute(string name)
    {
        Name = name;
    }

    /// <summary>
    /// Gets or sets the name of the command argument.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the command argument.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the position of the command argument.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the allowed values for the command argument.
    /// </summary>
    public object[] FromAmong { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the possible values for the command argument are case sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}
