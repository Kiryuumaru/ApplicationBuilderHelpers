using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="bool"/> flag (true/false, case-insensitive; null/empty binds true); failure surfaces as InvalidValue, exit 2.</summary>
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
        if (value.Equals("true", StringComparison.InvariantCultureIgnoreCase))
        {
            validateError = null;
            return true;
        }
        else if (value.Equals("false", StringComparison.InvariantCultureIgnoreCase))
        {
            validateError = null;
            return false;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected 'true' or 'false'";
        return default;
    }
}
