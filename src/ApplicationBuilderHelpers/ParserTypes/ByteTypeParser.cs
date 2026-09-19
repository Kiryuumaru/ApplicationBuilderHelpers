using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class ByteTypeParser : CommandTypeParser<byte>
{
    public override byte ParseValue(string? value, out string? validateError)
    {
        if (byte.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
