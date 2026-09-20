using AbsolutePathHelpers;
using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using System;
using System.IO;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal static class HelpTypeDisplay
{
    internal static string GetPlaceholderToken(Type type)
    {
        var unwrapped = Nullable.GetUnderlyingType(type) ?? type;

        if (unwrapped == typeof(string))
            return "STRING";

        if (unwrapped == typeof(byte)
            || unwrapped == typeof(sbyte)
            || unwrapped == typeof(short)
            || unwrapped == typeof(ushort)
            || unwrapped == typeof(int)
            || unwrapped == typeof(uint)
            || unwrapped == typeof(long)
            || unwrapped == typeof(ulong)
            || unwrapped == typeof(float)
            || unwrapped == typeof(double)
            || unwrapped == typeof(decimal))
            return "NUMBER";

        if (unwrapped == typeof(DateTime)
            || unwrapped == typeof(DateOnly)
            || unwrapped == typeof(TimeOnly)
            || unwrapped == typeof(DateTimeOffset))
            return "DATE";

        if (unwrapped == typeof(FileInfo)
            || unwrapped == typeof(AbsolutePath))
            return "FILE";

        if (unwrapped == typeof(DirectoryInfo))
            return "DIR";

        if (unwrapped == typeof(bool))
            return "BOOL";

        return "VALUE";
    }

    internal static string GetParameterPlaceholder(SubCommandOptionInfo option)
    {
        if (option.IsFlag)
            return string.Empty;

        var resolved = option.PropertyType;
        var isCollection = CollectionShape.IsCollection(option.PropertyType);
        if (isCollection)
        {
            if (CollectionShape.TryGetElementType(option.PropertyType, out var elementType) && elementType is not null)
                resolved = Nullable.GetUnderlyingType(elementType) ?? elementType;
        }

        var token = GetPlaceholderToken(resolved);

        if (isCollection)
        {
            if (token == "BOOL")
                token = "VALUE";
            return $"<{token}...>";
        }

        if (token == "BOOL")
            return string.Empty;

        return $"<{token}>";
    }
}
