using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class StringTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(string);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        return value;
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not string)
        {
            return null;
        }
        return value.ToString();
    }

    public bool Validate(string? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        return true;
    }
}
