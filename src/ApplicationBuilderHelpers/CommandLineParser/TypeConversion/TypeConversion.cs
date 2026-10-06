using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.ParserTypes;
using System;
using System.Globalization;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser.TypeConversion;

internal static class TypeConversion
{
    internal static object? Convert(
        string? raw,
        Type targetType,
        bool isCaseSensitive,
        object[]? fromAmong,
        string displayName,
        ICommandTypeParserCollection typeParsers,
        bool isSecret = false,
        bool isArgument = false)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(typeParsers);

        object? converted;
        try
        {
            converted = ConvertCore(raw, targetType, isCaseSensitive, displayName, typeParsers, isSecret);
        }
        catch (CommandException)
        {
            // Textual FromAmong hit still reports NotAmong (exit 2) so equivalent representations name the allowed set.
            if (raw is not null && fromAmong is not null && fromAmong.Length > 0 && !RawMatchesAllowed(raw, isCaseSensitive, fromAmong))
            {
                throw ConversionErrors.NotAmong(raw, displayName, string.Join(", ", fromAmong.Select(entry => entry?.ToString())), isSecret, isArgument);
            }

            throw;
        }

        ValidateFromAmong(raw, converted, targetType, isCaseSensitive, fromAmong, displayName, typeParsers, isSecret, isArgument);
        if (raw is not null && fromAmong is not null && fromAmong.Length > 0
            && (Nullable.GetUnderlyingType(targetType) ?? targetType) == typeof(string))
        {
            string? canonical = FindCanonicalStringValue(raw, isCaseSensitive, fromAmong);
            if (canonical is not null)
            {
                return canonical;
            }
        }

        return converted;
    }

    internal static object? Convert(
        string? raw,
        SubCommandOptionInfo option,
        string displayName,
        ICommandTypeParserCollection typeParsers)
    {
        ArgumentNullException.ThrowIfNull(option);
        return Convert(raw, option.PropertyType, option.IsCaseSensitive, option.ValidValues, displayName, typeParsers, option.IsSecret, isArgument: false);
    }

    internal static object? Convert(
        string? raw,
        SubCommandArgumentInfo argument,
        string displayName,
        ICommandTypeParserCollection typeParsers)
    {
        ArgumentNullException.ThrowIfNull(argument);
        return Convert(raw, argument.PropertyType, argument.IsCaseSensitive, argument.ValidValues, displayName, typeParsers, argument.IsSecret, isArgument: true);
    }

    private static object? ConvertCore(
        string? raw,
        Type targetType,
        bool isCaseSensitive,
        string displayName,
        ICommandTypeParserCollection typeParsers,
        bool isSecret = false)
    {
        if (raw is null)
        {
            return null;
        }

        if (typeParsers.TypeParsers.TryGetValue(targetType, out var parser))
        {
            object? parsed = parser.Parse(raw, out string? error);
            if (error is not null)
            {
                throw ConversionErrors.InvalidValue(raw, displayName, error, isSecret, targetType.Name);
            }

            return parsed;
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            Type underlyingType = Nullable.GetUnderlyingType(targetType)!;
            return ConvertCore(raw, underlyingType, isCaseSensitive, displayName, typeParsers, isSecret);
        }

        if (targetType == typeof(string))
        {
            return raw;
        }

        if (targetType.IsEnum)
        {
            if (Enum.TryParse(targetType, raw, !isCaseSensitive, out object? enumValue))
            {
                return enumValue;
            }

            throw ConversionErrors.InvalidValue(raw, displayName, ParserErrorHints.EnumAllowedValues(targetType), isSecret, targetType.Name);
        }

        try
        {
            return System.Convert.ChangeType(raw, targetType, CultureInfo.InvariantCulture);
        }
        catch (Exception)
        {
            // ChangeType throws across numeric/format/overflow shapes: normalize to InvalidValue (exit 2).
            throw ConversionErrors.InvalidValue(raw, displayName, $"Invalid format for value '{raw}' of type {targetType.FullName}", isSecret, targetType.Name);
        }
    }

    private static string? FindCanonicalStringValue(string raw, bool isCaseSensitive, object[] fromAmong)
    {
        foreach (object? entry in fromAmong)
        {
            if (string.Equals(entry?.ToString(), raw, StringComparison.Ordinal))
            {
                return entry?.ToString();
            }
        }

        if (isCaseSensitive)
        {
            return null;
        }

        foreach (object? entry in fromAmong)
        {
            if (string.Equals(entry?.ToString(), raw, StringComparison.OrdinalIgnoreCase))
            {
                return entry?.ToString();
            }
        }

        return null;
    }

    private static bool RawMatchesAllowed(string raw, bool isCaseSensitive, object[] fromAmong)
    {
        StringComparison comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
        foreach (object? entry in fromAmong)
        {
            if (string.Equals(entry?.ToString(), raw, comparison))
            {
                return true;
            }
        }

        return false;
    }

    private static void ValidateFromAmong(
        string? raw,
        object? converted,
        Type targetType,
        bool isCaseSensitive,
        object[]? fromAmong,
        string displayName,
        ICommandTypeParserCollection typeParsers,
        bool isSecret = false,
        bool isArgument = false)
    {
        if (fromAmong is null || fromAmong.Length == 0)
        {
            return;
        }

        if (raw is null || converted is null)
        {
            return;
        }

        Type effectiveType = Nullable.GetUnderlyingType(targetType) ?? targetType;

        if (effectiveType == typeof(string))
        {
            StringComparison comparison = isCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            foreach (object? entry in fromAmong)
            {
                if (string.Equals(entry?.ToString(), raw, comparison))
                {
                    return;
                }
            }
        }
        else
        {
            bool parserOwnsEnumTarget = effectiveType.IsEnum
                && (typeParsers.TypeParsers.ContainsKey(targetType)
                    || typeParsers.TypeParsers.ContainsKey(effectiveType));
            bool enumStrict = effectiveType.IsEnum
                && !parserOwnsEnumTarget
                && !effectiveType.IsDefined(typeof(FlagsAttribute), inherit: false);

            if (enumStrict && !Enum.IsDefined(effectiveType, converted))
            {
                throw ConversionErrors.NotAmong(raw, displayName, string.Join(", ", fromAmong.Select(entry => entry?.ToString())), isSecret, isArgument);
            }

            foreach (object? entry in fromAmong)
            {
                object? candidate = entry;
                if (entry is string entryText)
                {
                    try
                    {
                        candidate = ConvertCore(entryText, targetType, isCaseSensitive, displayName, typeParsers);
                    }
                    catch (Exception)
                    {
                        // Unconvertible allowed entry cannot match: skip it rather than failing the whole check.
                        continue;
                    }
                }
                else if (entry is not null && !parserOwnsEnumTarget)
                {
                    if (effectiveType.IsEnum)
                    {
                        object normalized;
                        if (entry.GetType() == effectiveType)
                        {
                            normalized = entry;
                        }
                        else
                        {
                            try
                            {
                                normalized = Enum.ToObject(effectiveType, entry);
                            }
                            catch (Exception)
                            {
                                // Numeric entry outside the enum range cannot match: skip it.
                                continue;
                            }
                        }

                        if (enumStrict && !Enum.IsDefined(effectiveType, normalized))
                        {
                            continue;
                        }

                        candidate = normalized;
                    }
                    else if (entry.GetType() != effectiveType)
                    {
                        object normalized;
                        try
                        {
                            normalized = System.Convert.ChangeType(entry, effectiveType, CultureInfo.InvariantCulture);
                        }
                        catch (Exception)
                        {
                            // Allowed entry that cannot convert to the target type cannot match: skip it.
                            continue;
                        }

                        object roundTrip;
                        try
                        {
                            roundTrip = System.Convert.ChangeType(normalized, entry.GetType(), CultureInfo.InvariantCulture);
                        }
                        catch (Exception)
                        {
                            // Lossy narrowing would compare unequal anyway: skip it.
                            continue;
                        }

                        if (!Equals(roundTrip, entry))
                        {
                            continue;
                        }

                        candidate = normalized;
                    }
                }

                if (Equals(converted, candidate))
                {
                    return;
                }
            }
        }

        throw ConversionErrors.NotAmong(raw, displayName, string.Join(", ", fromAmong.Select(entry => entry?.ToString())), isSecret, isArgument);
    }
}
