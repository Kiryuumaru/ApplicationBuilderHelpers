using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class ShortTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(short);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(short);
        }
        return short.Parse(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not short)
        {
            return default(short).ToString();
        }
        return value.ToString();
    }

    public bool Validate(string? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        if (value == null || string.IsNullOrEmpty(value))
        {
            return true;
        }
        if (!short.TryParse(value, out short _))
        {
            validateError = "Value must be a short.";
            return false;
        }
        return true;
    }
}
