using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class LongTypeParser : ICommandArgsTypeParser
{
    public Type Type => typeof(long);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(long);
        }
        return long.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not long)
        {
            return default(long).ToString();
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
        if (!long.TryParse(valueStr, out long _))
        {
            validateError = "Value must be a long.";
            return false;
        }
        return true;
    }
}
