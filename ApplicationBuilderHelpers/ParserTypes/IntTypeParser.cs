using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class IntTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(int);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(int);
        }
        return int.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
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
