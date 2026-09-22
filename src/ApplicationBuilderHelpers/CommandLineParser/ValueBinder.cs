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
    /// Validates every supplied value without binding (#496 aggregation dry run
    /// plus #483 help precedence). Runs the exact same conversion/materialization
    /// calls as <see cref="SetCommandValues"/>, discarding the converted results
    /// instead of assigning them, so each present value's conversion error is
    /// observed without stopping at the first. Env fallback runs first (same as
    /// the bind path), then each option/argument value is converted in
    /// deterministic canonical-key order. Skips bare-ledger keys when ShowHelp
    /// is set to preserve the --config --help carve-out (bare valued + help
    /// neighbor). Collected messages join the missing errors in the caller; the
    /// first message keeps the legacy single-error text byte-identical.
    /// </summary>
    public List<string> CollectBindingErrors(ParseResult result, bool skipBareWhenHelpRequested = false)
    {
        var errors = new List<string>();

        // Same env fallback as the bind path so env-supplied values are
        // validated (ValidValues/conversion) exactly like CLI-supplied ones.
        foreach (var option in result.TargetCommand.AllOptions.Where(o => !string.IsNullOrEmpty(o.EnvironmentVariable)))
        {
            EnvVarFallback.Apply(result, option, requiredOnly: false);
        }

        // Validate option values (grouped by canonical key so global-copy
        // identities validate one logical option once with merged CLI-wins values)
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

        // Validate argument values
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

        // First, check for environment variable fallback for all options with environment variables
        foreach (var option in result.TargetCommand.AllOptions.Where(o => !string.IsNullOrEmpty(o.EnvironmentVariable)))
        {
            EnvVarFallback.Apply(result, option, requiredOnly: false);
        }

        // Set option values (grouped by canonical key so global-copy
        // identities bind one logical option once with merged CLI-wins values)
        foreach (var group in result.OptionValues.GroupBy(
            entry => ParseResult.GetCanonicalOptionKey(entry.Key),
            StringComparer.Ordinal))
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

                propertyValue = CollectionShape.Create(option.PropertyType, elementType, converted, typeParserCollection, displayName, option.IsSecret);
            }
            else
            {
                propertyValue = TypeConversion.TypeConversion.Convert(values[0], option, displayName, typeParserCollection);
            }

            // Note: FromAmong (ValidValues) validation is applied inside
            // TypeConversion.Convert (convert-then-compare). ValidateValue is only
            // used for required field validation in the required-validation pass.

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
        $"option '--{option.LongName ?? option.ShortName?.ToString() ?? option.Property.Name}'";

    private static string GetArgumentDisplayName(SubCommandArgumentInfo argument) =>
        $"argument '{argument.DisplayName}'";
}
