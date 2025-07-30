using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class BoolTypeParser : SingleTypeParser
{
    public override Type Type => typeof(bool);

    public override object? ParseSingle(string value, out string? validateError)
    {
        if (value.Equals("true", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("yes", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("1", StringComparison.InvariantCultureIgnoreCase))
        {
            validateError = null;
            return true;
        }
        else if (value.Equals("false", StringComparison.InvariantCultureIgnoreCase) ||
            value.Equals("no", StringComparison.InvariantCultureIgnoreCase) ||
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

        validateError = $"Invalid boolean value: '{value}'. Expected 'true', 'false', 'yes', 'no', '1', or '0'";
        return null;
    }
}
