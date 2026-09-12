using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Binds parsed values onto the command instance via the shared
/// <see cref="TypeConversion.TypeConversion"/> scalar pipeline and <see cref="CollectionShape"/>
/// collection materialization (per-element conversion for collections,
/// scalar conversion otherwise).
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

            var displayName = GetOptionDisplayName(option);
            object? propertyValue;

            if (CollectionShape.IsCollection(option.PropertyType)
                && CollectionShape.TryGetElementType(option.PropertyType, out var elementType)
                && elementType is not null)
            {
                var converted = new List<object?>(values.Count);
                foreach (var raw in values)
                {
                    converted.Add(TypeConversion.TypeConversion.Convert(
                        raw, elementType, option.IsCaseSensitive, option.ValidValues, displayName, typeParserCollection, option.IsSecret, isArgument: false));
                }

                propertyValue = CollectionShape.Create(option.PropertyType, elementType, converted, typeParserCollection);
            }
            else
            {
                propertyValue = TypeConversion.TypeConversion.Convert(values[0], option, displayName, typeParserCollection);
            }

            // Note: FromAmong (ValidValues) validation is applied inside
            // TypeConversion.Convert (convert-then-compare). ValidateValue is only
            // used for required field validation in ValidateRequiredParameters.

            option.Property.SetValue(command, propertyValue);
        }

        // Set argument values
        foreach (var (argument, values) in result.ArgumentValues)
        {
            if (values.Count == 0) continue;

            var displayName = GetArgumentDisplayName(argument);
            object? propertyValue;

            if (CollectionShape.IsCollection(argument.PropertyType)
                && CollectionShape.TryGetElementType(argument.PropertyType, out var elementType)
                && elementType is not null)
            {
                var converted = new List<object?>(values.Count);
                foreach (var raw in values)
                {
                    string? normalized = raw.Length == 0 ? null : raw;
                    converted.Add(TypeConversion.TypeConversion.Convert(
                        normalized, elementType, argument.IsCaseSensitive, argument.ValidValues, displayName, typeParserCollection, argument.IsSecret, isArgument: true));
                }

                propertyValue = CollectionShape.Create(argument.PropertyType, elementType, converted, typeParserCollection);
            }
            else
            {
                string? raw = values[0];
                string? normalized = raw.Length == 0 ? null : raw;
                propertyValue = TypeConversion.TypeConversion.Convert(
                    normalized, argument, displayName, typeParserCollection);
            }

            argument.Property.SetValue(command, propertyValue);
        }
    }

    private static string GetOptionDisplayName(SubCommandOptionInfo option) =>
        $"option '--{option.LongName ?? option.ShortName?.ToString() ?? option.Property.Name}'";

    private static string GetArgumentDisplayName(SubCommandArgumentInfo argument) =>
        $"argument '{argument.DisplayName}'";
}
