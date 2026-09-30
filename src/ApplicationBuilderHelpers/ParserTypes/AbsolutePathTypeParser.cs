using ApplicationBuilderHelpers.Interfaces;
using System;
using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Abstracts;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into an <see cref="AbsolutePath"/>; failure surfaces as InvalidValue, exit 2.</summary>
internal class AbsolutePathTypeParser : CommandTypeParser<AbsolutePath>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override AbsolutePath? ParseValue(string? value, out string? validateError)
    {
        if (AbsolutePath.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }
}
