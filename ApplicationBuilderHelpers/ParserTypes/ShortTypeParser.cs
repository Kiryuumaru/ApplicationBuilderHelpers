using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class ShortTypeParser : ICommandArgsTypeParser
{
    public Type Type => typeof(short);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(short);
        }
        return short.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not short)
        {
            return default(short).ToString();
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
        if (!short.TryParse(valueStr, out short _))
        {
            validateError = "Value must be a short.";
            return false;
        }
        return true;
    }
}
