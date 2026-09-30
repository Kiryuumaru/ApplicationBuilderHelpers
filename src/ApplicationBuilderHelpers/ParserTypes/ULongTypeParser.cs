using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="ulong"/> (invariant, range-checked); failure surfaces as InvalidValue, exit 2.</summary>
internal class ULongTypeParser : CommandTypeParser<ulong>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override ulong ParseValue(string? value, out string? validateError)
    {
        if (ulong.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
