using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.IO;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class FileInfoTypeParser : CommandTypeParser<FileInfo>
{
    public override FileInfo? ParseValue(string? value, out string? validateError)
    {
        if (!string.IsNullOrWhiteSpace(value))
        {
            validateError = null;
            return new FileInfo(value);
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return default;
    }

    public override string? GetStringValue(FileInfo? value)
    {
        return value?.FullName;
    }
}
