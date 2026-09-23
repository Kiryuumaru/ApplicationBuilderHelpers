using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Binds parsed values onto the command instance with the shared
/// <see cref="TypeConversion.TypeConversion"/> scalar pipeline and <see cref="CollectionShape"/>
/// collection materialization (per-element conversion for collections,
/// scalar conversion otherwise).
/// </summary>
internal sealed class ValueBinder(ICommandTypeParserCollection typeParserCollection)
{
    /// <summary>
    /// Validates every supplied value without binding. Applies
    /// environment-variable fallback, converts each option/argument value in
    /// canonical-key order, and skips bare-occurrence keys when help is
    /// requested. Error messages join the missing errors in the caller; the
    /// first message matches the single-error text.
    /// </summary>
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

    /// <summary>
    /// Sets the parsed values on the command instance properties
    /// </summary>
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
            errors.Add(NormalizeBindingError(ex));
        }
    }

    private static string NormalizeBindingError(Exceptions.CommandException ex) =>
        ex.Message ?? string.Empty;

    private static string GetOptionDisplayName(SubCommandOptionInfo option) =>
        $"option '--{ParseResult.GetCanonicalOptionKey(option)}'";

    private static string GetArgumentDisplayName(SubCommandArgumentInfo argument) =>
        $"argument '{argument.DisplayName}'";
}
