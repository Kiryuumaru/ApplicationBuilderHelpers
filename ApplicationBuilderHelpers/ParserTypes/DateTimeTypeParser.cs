using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class DateTimeTypeParser : ICommandTypeParser
{
    public Type Type => typeof(DateTime);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(DateTime);
        }
        return DateTime.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not DateTime)
        {
            return default(DateTime).ToString();
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
        if (!DateTime.TryParse(valueStr, out DateTime _))
        {
            validateError = "Value must be a DateTime.";
            return false;
        }
        return true;
    }
}
