using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class UShortTypeParser : ICommandLineTypeParser
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

    public bool Validate(string? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        if (value == null || string.IsNullOrEmpty(value))
        {
            return true;
        }
        if (!ushort.TryParse(value, out ushort _))
        {
            validateError = "Value must be a ushort.";
            return false;
        }
        return true;
    }
}
