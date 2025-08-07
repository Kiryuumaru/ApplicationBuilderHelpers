using System;

namespace ApplicationBuilderHelpers.Attributes;

/// <summary>
/// Attribute to define command line options for properties.
/// </summary>
[AttributeUsage(AttributeTargets.Property)]
public class CommandOptionAttribute : Attribute
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CommandOptionAttribute"/> class with a short term and a term.
    /// </summary>
    /// <param name="shortTerm">The short term for the command option.</param>
    /// <param name="term">The term for the command option.</param>
    public CommandOptionAttribute(char shortTerm, string term)
    {
        ShortTerm = shortTerm;
        Term = term;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandOptionAttribute"/> class with a short term.
    /// </summary>
    /// <param name="shortTerm">The short term for the command option.</param>
    public CommandOptionAttribute(char shortTerm)
    {
        ShortTerm = shortTerm;
        Term = null;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="CommandOptionAttribute"/> class with a term.
    /// </summary>
    /// <param name="term">The term for the command option.</param>
    public CommandOptionAttribute(string term)
    {
        ShortTerm = null;
        Term = term;
    }

    /// <summary>
    /// Gets or sets the term for the command option.
    /// </summary>
    public string? Term { get; set; }

    /// <summary>
    /// Gets or sets the short term for the command option.
    /// </summary>
    public char? ShortTerm { get; set; }

    /// <summary>
    /// Gets or sets the environment variable for the command option.
    /// </summary>
    public string? EnvironmentVariable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the command option is required.
    /// </summary>
    public bool Required { get; set; }

    /// <summary>
    /// Gets or sets the description for the command option.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the possible values for the command option.
    /// </summary>
    public object[] FromAmong { get; set; } = [];

    /// <summary>
    /// Gets or sets a value indicating whether the possible values for the command option are case sensitive.
    /// </summary>
    public bool CaseSensitive { get; set; } = false;
}
