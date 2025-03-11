using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class ULongTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(ulong);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(ulong);
        }
        return ulong.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not ulong)
        {
            return default(ulong).ToString();
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
        if (!ulong.TryParse(value, out ulong _))
        {
            validateError = "Value must be a ulong.";
            return false;
        }
        return true;
    }
}
