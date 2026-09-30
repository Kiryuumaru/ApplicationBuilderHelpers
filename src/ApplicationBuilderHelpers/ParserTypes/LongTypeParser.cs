using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="long"/> (invariant, range-checked); failure surfaces as InvalidValue, exit 2.</summary>
internal class LongTypeParser : CommandTypeParser<long>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override long ParseValue(string? value, out string? validateError)
    {
        if (long.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
