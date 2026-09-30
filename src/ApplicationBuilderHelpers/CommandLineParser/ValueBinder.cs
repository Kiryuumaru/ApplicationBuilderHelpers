using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

internal sealed class ValueBinder(ICommandTypeParserCollection typeParserCollection)
{
    public List<string> CollectBindingErrors(ParseResult result, bool skipBareWhenHelpRequested = false)
    {
        var errors = new List<string>();

        foreach (var option in result.TargetCommand.AllOptions.Where(o => !string.IsNullOrEmpty(o.EnvironmentVariable)))
        {
            EnvVarFallback.Apply(result, option, requiredOnly: false);
        }

        foreach (var group in result.OptionValues
            .OrderBy(entry => ParseResult.GetCanonicalOptionKey(entry.Key), StringComparer.Ordinal)
            .GroupBy(entry => ParseResult.GetCanonicalOptionKey(entry.Key), StringComparer.Ordinal))
        {
            if (skipBareWhenHelpRequested && result.ShowHelp && result.BareOptionOccurrences.Contains(group.Key))
                continue;

            var option = group.First().Key;
            var values = group.SelectMany(entry => entry.Value).ToList();
            if (values.Count == 0) continue;

            ValidateOptionGroup(option, values, errors);
        }

        foreach (var (argument, values) in result.ArgumentValues.OrderBy(entry => entry.Key.DisplayName, StringComparer.Ordinal))
        {
            if (values.Count == 0) continue;

            ValidateArgument(argument, values, errors);
        }

        return errors;
    }

    public void SetCommandValues(ParseResult result)
    {
        var command = result.TargetCommand.Command!;

        foreach (var option in result.TargetCommand.AllOptions.Where(o => !string.IsNullOrEmpty(o.EnvironmentVariable)))
        {
            EnvVarFallback.Apply(result, option, requiredOnly: false);
        }

        foreach (var group in result.OptionValues.GroupBy(
            entry => ParseResult.GetCanonicalOptionKey(entry.Key),
            StringComparer.Ordinal))
        {
            var canonicalKey = group.Key;
            if (!result.TryGetCanonicalIdentityOption(canonicalKey, out var option))
                continue;
            var values = group.SelectMany(entry => entry.Value).ToList();
            if (values.Count == 0) continue;

            var displayName = GetOptionDisplayName(option);
            if (IsRequiredEmptyString(option, values[0]))
                throw RequiredEmptyStringError(option, values[0], displayName);

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

                propertyValue = CollectionShape.Create(option.PropertyType, elementType, converted, typeParserCollection, displayName, option.IsSecret);
            }
            else
            {
                propertyValue = TypeConversion.TypeConversion.Convert(values[0], option, displayName, typeParserCollection);
            }


            option.Property.SetValue(command, propertyValue);
        }

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
                    converted.Add(TypeConversion.TypeConversion.Convert(
                        raw, elementType, argument.IsCaseSensitive, argument.ValidValues, displayName, typeParserCollection, argument.IsSecret, isArgument: true));
                }

                propertyValue = CollectionShape.Create(argument.PropertyType, elementType, converted, typeParserCollection, displayName, argument.IsSecret);
            }
            else
            {
                propertyValue = TypeConversion.TypeConversion.Convert(values[0], argument, displayName, typeParserCollection);
            }

            argument.Property.SetValue(command, propertyValue);
        }
    }

    private void ValidateOptionGroup(SubCommandOptionInfo option, List<string> values, List<string> errors)
    {
        var displayName = GetOptionDisplayName(option);

        if (IsRequiredEmptyString(option, values[0]))
        {
            errors.Add(NormalizeBindingError(RequiredEmptyStringError(option, values[0], displayName)));
            return;
        }

        if (CollectionShape.IsCollection(option.PropertyType)
            && CollectionShape.TryGetElementType(option.PropertyType, out var elementType)
            && elementType is not null)
        {
            var converted = new List<object?>(values.Count);
            foreach (var raw in values)
            {
                try
                {
                    converted.Add(TypeConversion.TypeConversion.Convert(
                        raw, elementType, option.IsCaseSensitive, option.ValidValues, displayName, typeParserCollection, option.IsSecret, isArgument: false));
                }
                catch (Exceptions.CommandException ex)
                {
                    // Probe only: first element failure records one message and stops; anything else is a fault.
                    errors.Add(NormalizeBindingError(ex));
                    return;
                }
            }

            try
            {
                CollectionShape.Create(option.PropertyType, elementType, converted, typeParserCollection, displayName, option.IsSecret);
            }
            catch (Exceptions.CommandException ex)
            {
                // Probe only: first element failure records one message and stops; anything else is a fault.
                errors.Add(NormalizeBindingError(ex));
            }

            return;
        }

        try
        {
            TypeConversion.TypeConversion.Convert(values[0], option, displayName, typeParserCollection);
        }
        catch (Exceptions.CommandException ex)
        {
            // Probe only: first element failure records one message and stops; anything else is a fault.
            errors.Add(NormalizeBindingError(ex));
        }
    }

    private void ValidateArgument(SubCommandArgumentInfo argument, List<string> values, List<string> errors)
    {
        var displayName = GetArgumentDisplayName(argument);

        if (CollectionShape.IsCollection(argument.PropertyType)
            && CollectionShape.TryGetElementType(argument.PropertyType, out var elementType)
            && elementType is not null)
        {
            var converted = new List<object?>(values.Count);
            foreach (var raw in values)
            {
                try
                {
                    converted.Add(TypeConversion.TypeConversion.Convert(
                        raw, elementType, argument.IsCaseSensitive, argument.ValidValues, displayName, typeParserCollection, argument.IsSecret, isArgument: true));
                }
                catch (Exceptions.CommandException ex)
                {
                    // Probe only: first element failure records one message and stops; anything else is a fault.
                    errors.Add(NormalizeBindingError(ex));
                    return;
                }
            }

            try
            {
                CollectionShape.Create(argument.PropertyType, elementType, converted, typeParserCollection, displayName, argument.IsSecret);
            }
            catch (Exceptions.CommandException ex)
            {
                // Probe only: first element failure records one message and stops; anything else is a fault.
                errors.Add(NormalizeBindingError(ex));
            }

            return;
        }

        try
        {
            TypeConversion.TypeConversion.Convert(values[0], argument, displayName, typeParserCollection);
        }
        catch (Exceptions.CommandException ex)
        {
            // Probe only: first element failure records one message and stops; anything else is a fault.
            errors.Add(NormalizeBindingError(ex));
        }
    }

    private static string NormalizeBindingError(Exceptions.CommandException ex) =>
        ex.Message ?? string.Empty;

    private static bool IsRequiredEmptyString(SubCommandOptionInfo option, string? raw) =>
        option.IsRequired
        && !CollectionShape.IsCollection(option.PropertyType)
        && option.PropertyType == typeof(string)
        && raw is not null
        && raw.Length == 0;

    private static Exceptions.CommandException RequiredEmptyStringError(SubCommandOptionInfo option, string raw, string displayName) =>
        TypeConversion.ConversionErrors.InvalidValue(raw, displayName, "Required text must not be empty.", option.IsSecret, "String");

    private static string GetOptionDisplayName(SubCommandOptionInfo option) =>
        $"option '--{ParseResult.GetCanonicalOptionKey(option)}'";

    private static string GetArgumentDisplayName(SubCommandArgumentInfo argument) =>
        $"argument '{argument.DisplayName}'";
}
