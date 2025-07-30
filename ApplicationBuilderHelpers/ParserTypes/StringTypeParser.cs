using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class StringTypeParser : SingleTypeParser
{
    public override Type Type => typeof(string);

    public override object? ParseSingle(string value, out string? validateError)
    {
        validateError = null;
        return value;
    }
}
