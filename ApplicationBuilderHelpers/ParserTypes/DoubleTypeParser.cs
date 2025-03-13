using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class DoubleTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(double);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(double);
        }
        return double.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not double)
        {
            return default(double).ToString();
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
        if (!double.TryParse(valueStr, out double _))
        {
            validateError = "Value must be a double.";
            return false;
        }
        return true;
    }
}
