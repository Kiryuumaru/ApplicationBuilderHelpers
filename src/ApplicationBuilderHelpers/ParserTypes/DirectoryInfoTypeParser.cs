using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.IO;

namespace ApplicationBuilderHelpers.ParserTypes;

/// <summary>Parses CLI text into a <see cref="DirectoryInfo"/> (no existence check); failure surfaces as InvalidValue, exit 2.</summary>
internal class DirectoryInfoTypeParser : CommandTypeParser<DirectoryInfo>
{
    /// <summary>Converts CLI text to the target value.</summary>
    public override DirectoryInfo? ParseValue(string? value, out string? validateError)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            validateError = null;
            return new DirectoryInfo(value);
        }

        validateError = ParserErrorHints.Shape(Type, value, ParserErrorHints.FilePathValue);
        return default;
    }

    /// <summary>Renders the value as its full path.</summary>
    public override string? GetStringValue(DirectoryInfo? value)
    {
        return value?.FullName;
    }
}
