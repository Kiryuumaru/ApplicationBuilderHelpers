using AbsolutePathHelpers;
using System;
using System.Globalization;
using System.IO;

namespace ApplicationBuilderHelpers.ParserTypes;

internal static class ParserErrorHints
{
    internal const string SingleCharacter = "Expected a single character.";

    internal const string FloatingPointNumber = "Expected a number (for example '1.5').";

    internal const string DecimalNumber = "Expected a decimal number (for example '123.45').";

    internal const string DecimalWithoutExponent = "Expected a decimal number without an exponent (for example '123.45').";

    internal const string GuidValue = "Expected a GUID (for example '3f2504e0-4f89-11d3-9a0c-0305e82c3301').";

    internal const string DateTimeValue = "Expected a date and time (for example '2024-01-15' or '2024-01-15 13:30:00').";

    internal const string DateTimeOffsetValue = "Expected a date and time with an offset (for example '2024-01-15 13:30:00 +02:00').";

    internal const string DateOnlyValue = "Expected a date (for example '2024-01-15').";

    internal const string TimeOnlyValue = "Expected a time (for example '13:30').";

    internal const string TimeSpanValue = "Expected a time span (for example '01:30:00').";

    internal const string UriValue = "Expected a URI (for example 'https://example.com').";

    internal const string VersionValue = "Expected a version (for example '1.2.3').";

    internal const string AbsolutePathValue = "Expected an absolute path (for example '/tmp/output').";

    internal const string FilePathValue = "Expected a non-empty file path.";

    internal static string WholeNumberRange(long min, long max) =>
        $"Expected a whole number between {min.ToString(CultureInfo.InvariantCulture)} and {max.ToString(CultureInfo.InvariantCulture)}.";

    internal static string WholeNumberRange(ulong min, ulong max) =>
        $"Expected a whole number between {min.ToString(CultureInfo.InvariantCulture)} and {max.ToString(CultureInfo.InvariantCulture)}.";

    internal static string Shape(Type type, string? value, string hint) =>
        $"Invalid {type.Name} value: '{value}'. {hint}";

    internal static string OutOfRange(Type type, string? value, string hint) =>
        $"Value '{value}' is out of range for {type.Name}. {hint}";

    internal static bool IsWholeNumber(string? value) =>
        decimal.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out _);

    internal static bool IsNumeric(string? value)
    {
        if (!double.TryParse(value, NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out var parsed))
        {
            return false;
        }

        return !double.IsNaN(parsed) && !double.IsInfinity(parsed);
    }

    internal static string EnumAllowedValues(Type enumType) =>
        $"Expected one of: {string.Join(", ", Enum.GetNames(Nullable.GetUnderlyingType(enumType) ?? enumType))}.";

    internal static string HintFor(Type type)
    {
        Type effective = Nullable.GetUnderlyingType(type) ?? type;
        if (effective == typeof(sbyte))
        {
            return WholeNumberRange(sbyte.MinValue, sbyte.MaxValue);
        }

        if (effective == typeof(byte))
        {
            return WholeNumberRange(byte.MinValue, byte.MaxValue);
        }

        if (effective == typeof(short))
        {
            return WholeNumberRange(short.MinValue, short.MaxValue);
        }

        if (effective == typeof(ushort))
        {
            return WholeNumberRange(ushort.MinValue, ushort.MaxValue);
        }

        if (effective == typeof(int))
        {
            return WholeNumberRange(int.MinValue, int.MaxValue);
        }

        if (effective == typeof(uint))
        {
            return WholeNumberRange(uint.MinValue, uint.MaxValue);
        }

        if (effective == typeof(long))
        {
            return WholeNumberRange(long.MinValue, long.MaxValue);
        }

        if (effective == typeof(ulong))
        {
            return WholeNumberRange(ulong.MinValue, ulong.MaxValue);
        }

        if (effective == typeof(char))
        {
            return SingleCharacter;
        }

        if (effective == typeof(float) || effective == typeof(double))
        {
            return FloatingPointNumber;
        }

        if (effective == typeof(decimal))
        {
            return DecimalNumber;
        }

        if (effective == typeof(Guid))
        {
            return GuidValue;
        }

        if (effective == typeof(DateTime))
        {
            return DateTimeValue;
        }

        if (effective == typeof(DateTimeOffset))
        {
            return DateTimeOffsetValue;
        }

        if (effective == typeof(DateOnly))
        {
            return DateOnlyValue;
        }

        if (effective == typeof(TimeOnly))
        {
            return TimeOnlyValue;
        }

        if (effective == typeof(TimeSpan))
        {
            return TimeSpanValue;
        }

        if (effective == typeof(Uri))
        {
            return UriValue;
        }

        if (effective == typeof(Version))
        {
            return VersionValue;
        }

        if (effective == typeof(FileInfo))
        {
            return FilePathValue;
        }

        if (effective == typeof(AbsolutePath))
        {
            return AbsolutePathValue;
        }

        return $"Expected a valid {effective.Name}.";
    }
}
