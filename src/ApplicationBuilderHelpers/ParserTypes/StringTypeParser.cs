using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="string"/> (pass-through; never fails).</summary>
internal class StringTypeParser : CommandTypeParser<string>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override string? ParseValue(string? value, out string? validateError)
    {
        validateError = null;
        return value;
    }
}
