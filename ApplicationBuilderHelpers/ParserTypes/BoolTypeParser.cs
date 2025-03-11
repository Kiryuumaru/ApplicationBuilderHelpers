using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class BoolTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(bool);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(bool);
        }
        return bool.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not bool)
        {
            return default(bool).ToString();
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
        if (!bool.TryParse(value, out bool _))
        {
            validateError = "Value must be a bool.";
            return false;
        }
        return true;
    }
}
