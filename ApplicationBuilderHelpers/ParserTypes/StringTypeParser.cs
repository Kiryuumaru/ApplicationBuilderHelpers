using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class StringTypeParser : ICommandTypeParser
{
    public Type Type => typeof(string);

    public object? Parse(string? value, out string? validateError)
    {
        validateError = null;
        return value;
    }

    public string? GetString(object? value)
    {
        return value?.ToString();
    }
}
