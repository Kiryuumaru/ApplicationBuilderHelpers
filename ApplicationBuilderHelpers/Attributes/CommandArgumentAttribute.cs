using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class CommandArgumentAttribute : Attribute
{
    public CommandArgumentAttribute()
    {
    }

    public CommandArgumentAttribute(string name)
    {
        Name = name;
    }

    public string? Name { get; set; }

    public string? Description { get; set; }

    public int Position { get; set; }

    public object[] FromAmong { get; set; } = [];
}
