using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="bool"/> flag (true/yes/on/1, false/no/off/0, case-insensitive; null/empty binds true); failure surfaces as InvalidValue, exit 2.</summary>
internal class BoolTypeParser : CommandTypeParser<bool>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override bool ParseValue(string? value, out string? validateError)
    {
        if (string.IsNullOrEmpty(value))
        {
            validateError = null;
            return true;
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
        return default;
    }
}
