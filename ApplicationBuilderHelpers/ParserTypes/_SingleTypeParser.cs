using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ApplicationBuilderHelpers.ParserTypes;

internal abstract class SingleTypeParser : ICommandTypeParser
{
    public abstract Type Type { get; }

    public object? Parse(string[]? value, out string? validateError)
    {
        if (value == null)
        {
            validateError = $"{Type.Name} cannot be null";
            return null;
        }
        if (value.Length > 1)
        {
            validateError = $"{Type.Name} cannot have more than one value";
            return null;
        }

        return ParseSingle(value.First(), out validateError);
    }

    public abstract object? ParseSingle(string value, out string? validateError);
}
