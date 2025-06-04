using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class CharTypeParser : ICommandArgsTypeParser
{
    public Type Type => typeof(char);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(char);
        }
        return char.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not char)
        {
            return default(char).ToString();
        }
        return value.ToString();
    }

    public bool Validate(object? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        if (value == null || value is not string valueStr || string.IsNullOrEmpty(valueStr))
        {
            return true;
        }
        if (!char.TryParse(valueStr, out char _))
        {
            validateError = "Value must be a char.";
            return false;
        }
        return true;
    }
}
