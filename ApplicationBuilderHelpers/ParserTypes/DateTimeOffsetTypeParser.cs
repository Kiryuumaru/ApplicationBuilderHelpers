using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class DateTimeOffsetTypeParser : ICommandArgsTypeParser
{
    public Type Type => typeof(DateTimeOffset);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(DateTimeOffset);
        }
        return DateTimeOffset.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not DateTimeOffset)
        {
            return default(DateTimeOffset).ToString();
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
        if (!DateTimeOffset.TryParse(valueStr, out DateTimeOffset _))
        {
            validateError = "Value must be a DateTimeOffset.";
            return false;
        }
        return true;
    }
}
