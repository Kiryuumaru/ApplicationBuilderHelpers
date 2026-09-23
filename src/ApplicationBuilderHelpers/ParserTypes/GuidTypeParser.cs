using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class GuidTypeParser : CommandTypeParser<Guid>
{
    public override Guid ParseValue(string? value, out string? validateError)
    {
#if NET7_0_OR_GREATER
        if (Guid.TryParse(value, CultureInfo.InvariantCulture, out var result))
#else
        if (Guid.TryParse(value, out var result))
#endif
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
