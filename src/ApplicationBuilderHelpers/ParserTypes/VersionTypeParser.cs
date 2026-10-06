using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="Version"/>; failure surfaces as InvalidValue, exit 2.</summary>
internal class VersionTypeParser : CommandTypeParser<Version>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override Version? ParseValue(string? value, out string? validateError)
    {
        if (Version.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.VersionValue);
        return default;
    }
}
