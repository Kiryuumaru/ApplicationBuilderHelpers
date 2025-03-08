using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class IntTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(int);

    public string[] Choices { get; } = [];

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return default(int);
        }
        return int.Parse(value!);
    }

    public string? Parse(object? value)
    {
        if (value == null || value is not int)
        {
            return default(int).ToString();
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
        if (!int.TryParse(value, out int _))
        {
            validateError = "Value must be an int.";
            return false;
        }
        return true;
    }
}
