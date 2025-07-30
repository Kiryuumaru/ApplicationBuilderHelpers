using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace ApplicationBuilderHelpers.Attributes;

/// <summary>
/// Attribute to define command metadata for classes that implement application commands.
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public class CommandAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAttribute"/> class with an optional description.
    /// </summary>
    /// <param name="description">The description for the command.</param>
    public CommandAttribute(string? description = null)
    {
        Name = null;
        Description = description;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandAttribute"/> class with a name and an optional description.
    /// </summary>
    /// <param name="name">The name for the command.</param>
    /// <param name="description">The description for the command.</param>
    public CommandAttribute(string name, string? description = null)
    {
        Name = name;
        Description = description;
    }

    /// <summary>
    /// Gets or sets the name of the command.
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Gets or sets the description of the command.
    /// </summary>
    public string? Description { get; set; }
}
