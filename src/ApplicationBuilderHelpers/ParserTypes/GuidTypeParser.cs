using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Globalization;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="Guid"/>; failure surfaces as InvalidValue, exit 2.</summary>
internal class GuidTypeParser : CommandTypeParser<Guid>
{
    /// <summary>Converts CLI text to the target value.</summary>
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

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.GuidValue);
        return default;
    }
}
