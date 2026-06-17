using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class StringTypeParser : CommandTypeParser<string>
{
    public override string? ParseValue(string? value, out string? validateError)
    {
        validateError = null;
        return value;
    }
}
