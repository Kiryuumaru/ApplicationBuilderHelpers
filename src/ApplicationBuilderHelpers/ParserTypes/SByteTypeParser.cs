using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into an <see cref="sbyte"/> (invariant, range-checked); failure surfaces as InvalidValue, exit 2.</summary>
internal class SByteTypeParser : CommandTypeParser<sbyte>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override sbyte ParseValue(string? value, out string? validateError)
    {
        if (sbyte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
