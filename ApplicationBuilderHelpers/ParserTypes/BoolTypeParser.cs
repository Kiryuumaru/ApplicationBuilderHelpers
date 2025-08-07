using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class BoolTypeParser : ICommandTypeParser
{
    public Type Type => typeof(bool);

    public object? Parse(string? value, out string? validateError)
    {
        if (string.IsNullOrEmpty(value)) // bool only needs a flag, not a value
        {
            validateError = null;
            return true; // Default to true if no value is provided
        }
        if (value.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("on", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("1", StringComparison.InvariantCultureIgnoreCase))
        {
            validateError = null;
            return true;
        }
        else if (value.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("no", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("off", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("0", StringComparison.InvariantCultureIgnoreCase))
        {
            validateError = null;
            return false;
        }
        else if (bool.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'";
        return null;
    }

    public string? GetString(object? value)
    {
        return value switch
        {
            bool b => b ? "true" : "false",
            _ => null
        };
    }
}
