using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="char"/> (exactly one character); failure surfaces as InvalidValue, exit 2.</summary>
internal class CharTypeParser : CommandTypeParser<char>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override char ParseValue(string? value, out string? validateError)
    {
        if (char.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.SingleCharacter);
        return default;
    }
}
