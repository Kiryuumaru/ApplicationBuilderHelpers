using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class SByteTypeParser : ICommandArgsTypeParser
{
    public Type Type => typeof(sbyte);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(sbyte);
        }
        return sbyte.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not sbyte)
        {
            return default(sbyte).ToString();
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
        if (!sbyte.TryParse(valueStr, out sbyte _))
        {
            validateError = "Value must be a sbyte.";
            return false;
        }
        return true;
    }
}
