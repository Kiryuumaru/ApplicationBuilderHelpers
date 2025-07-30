using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class IntTypeParser : SingleTypeParser
{
    public override Type Type => typeof(int);

    public override object? ParseSingle(string value, out string? validateError)
    {
        if (int.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid integer value: '{value}'. Expected a valid integer.";
        return null;
    }
}
