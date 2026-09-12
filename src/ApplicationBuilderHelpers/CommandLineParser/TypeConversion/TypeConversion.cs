using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser.TypeConversion;

/// <summary>
/// Single scalar conversion pipeline shared by options and arguments:
/// raw CLI text in, converted value out, with convert-then-compare
/// <c>FromAmong</c> validation applied after conversion.
/// </summary>
internal static class TypeConversion
{
    /// <summary>
    /// Converts raw CLI text to <paramref name="targetType"/> and validates the
    /// converted value against <paramref name="fromAmong"/> when provided.
    /// Order: null passthrough, exact registry match, nullable unwrap, string,
    /// enum, then <see cref="System.Convert.ChangeType(object?, Type)"/> for
    /// unknown types only. Failures throw via <see cref="ConversionErrors"/>.
    /// </summary>
    /// <param name="raw">The raw CLI text, or null when no value was supplied.</param>
    /// <param name="targetType">The type to convert to.</param>
    /// <param name="isCaseSensitive">Whether enum parsing and allowed-values comparison are case sensitive.</param>
    /// <param name="fromAmong">Allowed values, compared after conversion; null or empty skips validation.</param>
    /// <param name="displayName">Display name including kind prefix, e.g. <c>"option '--mode'"</c> or <c>"argument 'level'"</c>.</param>
    /// <param name="typeParsers">The registered type parser collection.</param>
    internal static object? Convert(
        string? raw,
        Type targetType,
        bool isCaseSensitive,
        object[]? fromAmong,
        string displayName,
        ICommandTypeParserCollection typeParsers)
    {
        ArgumentNullException.ThrowIfNull(targetType);
        ArgumentNullException.ThrowIfNull(displayName);
        ArgumentNullException.ThrowIfNull(typeParsers);

        object? converted = ConvertCore(raw, targetType, isCaseSensitive, displayName, typeParsers);
        ValidateFromAmong(raw, converted, targetType, isCaseSensitive, fromAmong, displayName, typeParsers);
        return converted;
    }

    /// <summary>
    /// Option-shaped overload: pulls target type, case sensitivity, and allowed
    /// values from <paramref name="option"/> (<see cref="SubCommandOptionInfo.PropertyType"/>,
    /// <see cref="SubCommandOptionInfo.IsCaseSensitive"/>, <see cref="SubCommandOptionInfo.ValidValues"/>).
    /// </summary>
    /// <param name="raw">The raw CLI text, or null when no value was supplied.</param>
    /// <param name="option">The option providing conversion and validation settings.</param>
    /// <param name="displayName">Display name including kind prefix, e.g. <c>"option '--mode'"</c>.</param>
    /// <param name="typeParsers">The registered type parser collection.</param>
    internal static object? Convert(
        string? raw,
        SubCommandOptionInfo option,
        string displayName,
        ICommandTypeParserCollection typeParsers)
    {
        ArgumentNullException.ThrowIfNull(option);
        return Convert(raw, option.PropertyType, option.IsCaseSensitive, option.ValidValues, displayName, typeParsers);
    }

    private static object? ConvertCore(
        string? raw,
        Type targetType,
        bool isCaseSensitive,
        string displayName,
        ICommandTypeParserCollection typeParsers)
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
                throw ConversionErrors.InvalidValue(raw, displayName, error);
            }

            return parsed;
        }

        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            Type underlyingType = Nullable.GetUnderlyingType(targetType)!;
            return ConvertCore(raw, underlyingType, isCaseSensitive, displayName, typeParsers);
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

            throw ConversionErrors.InvalidValue(raw, displayName, null);
        }

        try
        {
            return System.Convert.ChangeType(raw, targetType);
        }
        catch (Exception)
        {
            throw ConversionErrors.InvalidValue(raw, displayName, $"Invalid format for value '{raw}' of type {targetType.FullName}");
        }
    }

    private static void ValidateFromAmong(
        string? raw,
        object? converted,
        Type targetType,
        bool isCaseSensitive,
        object[]? fromAmong,
        string displayName,
        ICommandTypeParserCollection typeParsers)
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
                        continue;
                    }
                }

                if (Equals(converted, candidate))
                {
                    return;
                }
            }
        }

        throw ConversionErrors.NotAmong(raw, displayName, string.Join(", ", fromAmong.Select(entry => entry?.ToString())));
    }
}
