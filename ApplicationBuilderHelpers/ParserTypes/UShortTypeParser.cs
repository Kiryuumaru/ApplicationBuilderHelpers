using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class UShortTypeParser : ICommandArgsTypeParser
{
    public Type Type => typeof(ushort);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(ushort);
        }
        return ushort.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not ushort)
        {
            return default(ushort).ToString();
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
        if (!ushort.TryParse(valueStr, out ushort _))
        {
            validateError = "Value must be a ushort.";
            return false;
        }
        return true;
    }
}
