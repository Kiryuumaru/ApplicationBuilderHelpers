using ApplicationBuilderHelpers.Interfaces;
using System;
using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Abstracts;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class AbsolutePathTypeParser : CommandTypeParser<AbsolutePath>
{
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
