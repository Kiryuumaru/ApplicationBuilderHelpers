using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class DecimalTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(decimal);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(decimal);
        }
        return decimal.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not decimal)
        {
            return default(decimal).ToString();
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
        if (!decimal.TryParse(value, out decimal _))
        {
            validateError = "Value must be a decimal.";
            return false;
        }
        return true;
    }
}
