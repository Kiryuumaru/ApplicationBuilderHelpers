using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class BoolTypeParser : ICommandTypeParser
{
    public Type Type => typeof(bool);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        if (value is bool valueBool)
        {
            return valueBool;
        }
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(bool);
        }
        if (valueStr.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
            valueStr.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
            valueStr.Equals("1", StringComparison.InvariantCultureIgnoreCase))
        {
            return true;
        }
        else if (valueStr.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
            valueStr.Equals("no", StringComparison.InvariantCultureIgnoreCase) ||
            valueStr.Equals("0", StringComparison.InvariantCultureIgnoreCase))
        {
            return false;
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

    public bool Validate(object? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        if (value == null || value is not string valueStr || string.IsNullOrEmpty(valueStr))
        {
            return true;
        }
        if (!bool.TryParse(valueStr, out bool _))
        {
            validateError = "Value must be a bool.";
            return false;
        }
        return true;
    }
}
