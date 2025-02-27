using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Attributes;

[AttributeUsage(AttributeTargets.Property)]
public class CommandOptionAttribute : Attribute
{
    public CommandOptionAttribute(char shortTerm, string term)
    {
        ShortTerm = shortTerm;
        Term = term;
    }

    public CommandOptionAttribute(char shortTerm)
    {
        ShortTerm = shortTerm;
        Term = null;
    }

    public CommandOptionAttribute(string term)
    {
        ShortTerm = null;
        Term = term;
    }

    public string? Term { get; set; }

    public char? ShortTerm { get; set; }

    public string? EnvironmentVariable { get; set; }

    public bool Required { get; set; }

    public string? Description { get; set; }

    public object[] FromAmong { get; set; } = [];
}
