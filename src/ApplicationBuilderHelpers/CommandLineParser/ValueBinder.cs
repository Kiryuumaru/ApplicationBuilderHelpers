using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Binds parsed values onto the command instance.
/// Moved verbatim from CommandLineParser (mechanical split, no behavior change).
/// </summary>
internal sealed class ValueBinder(ICommandTypeParserCollection typeParserCollection)
{
    /// <summary>
    /// Sets the parsed values on the command instance properties
    /// </summary>
    public void SetCommandValues(ParseResult result)
    {
        var command = result.TargetCommand.Command!;

        // First, check for environment variable fallback for all options with environment variables
        foreach (var option in result.TargetCommand.AllOptions.Where(o => !string.IsNullOrEmpty(o.EnvironmentVariable)))
        {
            EnvVarFallback.Apply(result, option, requiredOnly: false);
        }

        // Set option values (grouped by canonical key so global-copy
        // identities bind one logical option once with merged CLI-wins values)
        foreach (var group in result.OptionValues.GroupBy(
            entry => ParseResult.GetCanonicalOptionKey(entry.Key),
            StringComparer.OrdinalIgnoreCase))
        {
            var option = group.First().Key;
            var values = group.SelectMany(entry => entry.Value).ToList();
            if (values.Count == 0) continue;

            object? propertyValue;

            if (option.IsArray)
            {
                var elementType = option.ElementType!;
                var array = CreateTypedArray(elementType, values.Count);

                for (int i = 0; i < values.Count; i++)
                {
                    // Validate string value first if ValidValues are specified
                    if (option.ValidValues?.Length > 0)
                    {
                        ValidateStringValue(values[i], option);
                    }

                    var convertedValue = ConvertValue(values[i], elementType, option.IsCaseSensitive);
                    array.SetValue(convertedValue, i);
                }

                propertyValue = array;
            }
            else
            {
                // Validate string value first if ValidValues are specified
                if (option.ValidValues?.Length > 0)
                {
                    ValidateStringValue(values[0], option);
                }

                propertyValue = ConvertValue(values[0], option.PropertyType, option.IsCaseSensitive);
            }

            // Note: We don't call option.ValidateValue here anymore for ValidValues validation
            // since we already validated the string value above. ValidateValue is now only used
            // for required field validation in ValidateRequiredParameters.

            option.Property.SetValue(command, propertyValue);
        }

        // Set argument values
        foreach (var (argument, values) in result.ArgumentValues)
        {
            if (values.Count == 0) continue;

            object? propertyValue;

            if (argument.IsArray)
            {
                var elementType = argument.ElementType!;
                var array = CreateTypedArray(elementType, values.Count);

                for (int i = 0; i < values.Count; i++)
                {
                    var convertedValue = argument.ConvertValue(values[i], typeParserCollection);
                    array.SetValue(convertedValue, i);
                }

                propertyValue = array;
            }
            else
            {
                propertyValue = argument.ConvertValue(values[0], typeParserCollection);
            }

            argument.Property.SetValue(command, propertyValue);
        }
    }

    /// <summary>
    /// Validates a string value against the option's ValidValues before conversion
    /// </summary>
    private static void ValidateStringValue(string value, SubCommandOptionInfo option)
    {
        if (option.ValidValues?.Length > 0)
        {
            var comparisonType = option.IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            var isValid = option.ValidValues.Any(validValue =>
                string.Equals(validValue?.ToString(), value, comparisonType));

            if (!isValid)
            {
                var validValuesString = string.Join(", ", option.ValidValues.Select(v => v?.ToString()));
                throw new CommandException(
                    $"Value '{value}' is not valid for option '--{option.LongName ?? option.ShortName?.ToString()}'. " +
                    $"Must be one of: {validValuesString}", 1);
            }
        }
    }

    /// <summary>
    /// Converts a string value to the specified type
    /// </summary>
    private object? ConvertValue(string? value, Type targetType, bool isCaseSensitive)
    {
        if (value == null) return null;

        if (typeParserCollection.TypeParsers.TryGetValue(targetType, out var parser))
        {
            var result = parser.Parse(value, out var error);
            if (error != null)
                throw new CommandException(error, 1);
            return result;
        }

        if (targetType == typeof(string))
            return value;

        if (targetType.IsEnum && Enum.TryParse(targetType, value, !isCaseSensitive, out var typedVal))
            return typedVal;

        // Handle nullable types
        if (targetType.IsGenericType && targetType.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType)!;
            return ConvertValue(value, underlyingType, isCaseSensitive);
        }

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception)
        {
            throw new CommandException($"Invalid format for value '{value}' of type {targetType.FullName}", 1);
        }
    }

    /// <summary>
    /// Creates a typed array for AOT compatibility
    /// </summary>
    private Array CreateTypedArray(Type elementType, int length)
    {
        // First try to use the registered type parser to create the array
        if (typeParserCollection.TypeParsers.TryGetValue(elementType, out var parser))
        {
            try
            {
                return parser.CreateTypedArray(length);
            }
            catch
            {
                // If the type parser fails, fall back to manual creation
            }
        }

        // For other types, create a generic object array to maintain AOT compatibility
        return new object?[length];
    }
}
