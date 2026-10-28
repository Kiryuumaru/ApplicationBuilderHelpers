using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Resolves the default value of an omitted option for binding and help.
/// </summary>
internal sealed class OptionDefaultResolver(
    ICommandBuilder commandBuilder,
    ICommandTypeParserCollection typeParserCollection)
{
    private static readonly object ParserDefaultAbsent = new();

    private readonly ICommandBuilder _commandBuilder = commandBuilder;
    private readonly ICommandTypeParserCollection _typeParserCollection = typeParserCollection;
    private readonly Dictionary<SubCommandOptionInfo, object?> _parserDefaults = [];

    /// <summary>Resolves the parser default to bind when an option is omitted.</summary>
    internal bool TryResolveOmittedValue(ICommand command, SubCommandOptionInfo option, out object? value)
    {
        value = null;
        if (option.IsRequired || option.IsFlag || option.IsCollection)
            return false;

        var holder = FindHolder(command.GetType());
        if (holder is null)
            return false;

        var holderProperty = CommandReflectionCache.ResolveHolderProperty(holder, option.Property);
        if (holderProperty is null)
            return false;

        object? registration;
        if (holder.IsInstanceRegistration)
        {
            if (!holder.TryGetRegistrationValue(holderProperty, out registration))
                return false;
            if (!IsClrDefault(option.PropertyType, registration))
                return false;
            if (holder.HasInstanceMutation(holderProperty))
                return false;
            if (!TryReadLiveValue(command, holderProperty, out var live))
                return false;
            if (!InitializerValueEquality.ValuesEqual(live, registration))
                return false;
        }
        else
        {
            if (!TryReadLiveValue(command, holderProperty, out registration))
                return false;
            if (!IsClrDefault(option.PropertyType, registration))
                return false;
        }

        if (!TryGetParserDefault(option, out var parserDefault))
            return false;
        if (IsNoOpDefault(option.PropertyType, parserDefault))
            return false;

        value = parserDefault;
        return true;
    }

    /// <summary>Resolves the default value shown in help for a single option.</summary>
    internal bool TryGetHelpDefault(SubCommandOptionInfo option, out object? value)
    {
        value = null;

        var found = TryGetRegistrationValue(
            option,
            out var registration,
            out var agreed,
            out var instanceHolder,
            out var instanceProperty);
        if (option.IsFlag || option.IsCollection)
        {
            value = registration;
            return found;
        }

        if (found)
        {
            if (!IsClrDefault(option.PropertyType, registration))
            {
                value = registration;
                return true;
            }

            // A parser-default-sourced instance shows explicit or later mutation, never the parser default again.
            if (instanceHolder is not null && instanceProperty is not null)
            {
                if (!TryReadProperty(instanceHolder.Command, instanceProperty, out var live, ref agreed))
                    return false;

                if (instanceHolder.HasInstanceMutation(instanceProperty)
                    || !InitializerValueEquality.ValuesEqual(live, registration))
                {
                    value = live;
                    return true;
                }
            }
        }
        else if (!agreed)
        {
            return false;
        }

        if (!TryGetParserDefault(option, out var parserDefault))
            return false;
        if (IsNoOpDefault(option.PropertyType, parserDefault))
            return false;

        value = parserDefault;
        return true;
    }

    /// <summary>Formats a default value with its registered parser when one exists.</summary>
    internal string? FormatDefault(Type type, object? value)
    {
        string? text;
        if (_typeParserCollection.TypeParsers.TryGetValue(type, out var parser))
        {
            try
            {
                text = parser.GetString(value);
            }
            catch
            {
                return null;
            }
        }
        else
        {
            try
            {
                text = value?.ToString();
            }
            catch
            {
                return null;
            }
        }

        return SanitizeDefaultDisplay(text);
    }

    private static string? SanitizeDefaultDisplay(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return null;

        // Prior-run CLI text reaches help as a default; keep terminal controls out of the renderer.
        var sanitized = new StringBuilder(text.Length);
        foreach (var ch in text)
            sanitized.Append(char.IsControl(ch) || ch is '\u2028' or '\u2029' ? ' ' : ch);

        var display = sanitized.ToString();
        return string.IsNullOrWhiteSpace(display) ? null : display;
    }

    /// <summary>Records an explicit or environment-bound value on an instance registration.</summary>
    internal void MarkOptionBound(ICommand command, SubCommandOptionInfo option)
    {
        var holder = FindHolder(command.GetType());
        if (holder is null || !holder.IsInstanceRegistration)
            return;

        var holderProperty = CommandReflectionCache.ResolveHolderProperty(holder, option.Property);
        if (holderProperty is not null)
            holder.RecordInstanceMutation(holderProperty);
    }

    /// <summary>Whether a default value has no displayable text form.</summary>
    internal static bool IsEmptyDefault(object? value) =>
        value switch
        {
            null => true,
            string text => text.Length == 0,
            Array array => array.Length == 0,
            ICollection collection => collection.Count == 0,
            _ => false,
        };

    private static bool TryReadLiveValue(ICommand command, PropertyInfo property, out object? value)
    {
        try
        {
            value = property.GetValue(command);
            return true;
        }
        catch
        {
            value = null;
            return false;
        }
    }

    private static bool IsCompatibleDefault(Type propertyType, object? candidate)
    {
        if (candidate is null)
            return !propertyType.IsValueType || Nullable.GetUnderlyingType(propertyType) is not null;

        // Boxing a value-typed T? produces T, so compare against the underlying type.
        var targetType = Nullable.GetUnderlyingType(propertyType) ?? propertyType;
        return targetType.IsInstanceOfType(candidate);
    }

    private bool TryGetParserDefault(SubCommandOptionInfo option, out object? value)
    {
        if (!_parserDefaults.TryGetValue(option, out var cached))
        {
            cached = ParserDefaultAbsent;
            if (_typeParserCollection.TypeParsers.TryGetValue(option.PropertyType, out var parser))
            {
                try
                {
                    var candidate = parser.GetDefaultValue();
                    // Wrong-typed defaults are ignored; allowed-value checks follow the type check.
                    if (IsCompatibleDefault(option.PropertyType, candidate)
                        && (candidate is null
                            || TypeConversion.TypeConversion.IsAllowedValue(
                                candidate,
                                option.PropertyType,
                                option.IsCaseSensitive,
                                option.ValidValues,
                                _typeParserCollection)))
                    {
                        cached = candidate;
                    }
                }
                catch
                {
                    cached = ParserDefaultAbsent;
                }
            }
            _parserDefaults[option] = cached;
        }

        if (ReferenceEquals(cached, ParserDefaultAbsent))
        {
            value = null;
            return false;
        }

        value = cached;
        return true;
    }

    private static bool IsNoOpDefault(Type type, object? value) =>
        value is null || IsClrDefault(type, value) || IsEmptyDefault(value);

    [UnconditionalSuppressMessage("Trimming", "IL2067", Justification = "Only value types reach this path, constructors are never invoked, and the zeroed instance is used for comparison only.")]
    private static bool IsClrDefault(Type type, object? value)
    {
        if (!type.IsValueType || Nullable.GetUnderlyingType(type) is not null)
            return value is null;
        if (value is null)
            return false;

        return Equals(value, GetClrDefault(type));
    }

    private static object GetClrDefault(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors)] Type type) =>
        // A zeroed instance is the CLR default; constructors must not run for user structs.
        RuntimeHelpers.GetUninitializedObject(type);

    private bool TryGetRegistrationValue(
        SubCommandOptionInfo option,
        out object? value,
        out bool agreed,
        out TypedCommandHolder? instanceHolder,
        out PropertyInfo? instanceProperty)
    {
        agreed = true;
        value = null;
        instanceHolder = null;
        instanceProperty = null;

        if (option.OwnerCommand?.Command != null)
        {
            var holder = FindHolder(option.OwnerCommand.Command.GetType());
            if (holder is not null)
            {
                return TryGetHolderRegistrationValue(
                    holder,
                    option.OwnerCommand.Command,
                    option,
                    out value,
                    out agreed,
                    out instanceHolder,
                    out instanceProperty);
            }

            return TryReadProperty(option.OwnerCommand.Command, option.Property, out value, ref agreed);
        }

        if (option.BindTarget?.Command != null && !ReferenceEquals(option.BindTarget, option.OwnerCommand))
        {
            var bindCommand = option.BindTarget.Command;
            var holder = FindHolder(bindCommand.GetType());
            if (holder is not null)
            {
                return TryGetHolderRegistrationValue(
                    holder,
                    bindCommand,
                    option,
                    out value,
                    out agreed,
                    out instanceHolder,
                    out instanceProperty);
            }

            return TryReadProperty(bindCommand, option.Property, out value, ref agreed);
        }

        var declaringType = option.Property.DeclaringType;
        if (declaringType is null)
            return false;

        var fallbackHolder = FindHolder(declaringType);
        if (fallbackHolder is not null)
        {
            return TryGetHolderRegistrationValue(
                fallbackHolder,
                fallbackHolder.Command,
                option,
                out value,
                out agreed,
                out instanceHolder,
                out instanceProperty);
        }

        return TryGetUnanimousDerivedValue(option, out value, out agreed);
    }

    private bool TryGetHolderRegistrationValue(
        TypedCommandHolder holder,
        ICommand command,
        SubCommandOptionInfo option,
        out object? value,
        out bool agreed,
        out TypedCommandHolder? instanceHolder,
        out PropertyInfo? instanceProperty)
    {
        agreed = true;
        value = null;
        instanceHolder = null;
        instanceProperty = null;

        var holderProperty = CommandReflectionCache.ResolveHolderProperty(holder, option.Property);
        if (holderProperty is null)
        {
            // A private base option cannot reach binding defaults, but help still shows a real initializer.
            object? initializer;
            if (holder.TryGetRegistrationValue(option.Property, out var fallbackSnapshot))
            {
                initializer = fallbackSnapshot;
            }
            else if (holder.IsInstanceRegistration)
            {
                agreed = false;
                return false;
            }
            else if (!TryReadProperty(command, option.Property, out initializer, ref agreed))
            {
                return false;
            }

            if (IsClrDefault(option.Property.PropertyType, initializer))
            {
                agreed = false;
                return false;
            }

            value = initializer;
            return true;
        }

        if (holder.TryGetRegistrationValue(holderProperty, out var snapshot))
        {
            value = snapshot;
            if (holder.IsInstanceRegistration)
            {
                instanceHolder = holder;
                instanceProperty = holderProperty;
            }
            return true;
        }

        if (holder.IsInstanceRegistration)
        {
            agreed = false;
            return false;
        }

        return TryReadProperty(command, holderProperty, out value, ref agreed);
    }

    private static bool TryReadProperty(ICommand command, PropertyInfo property, out object? value, ref bool agreed)
    {
        try
        {
            value = property.GetValue(command);
            return true;
        }
        catch
        {
            value = null;
            agreed = false;
            return false;
        }
    }

    private bool TryGetUnanimousDerivedValue(SubCommandOptionInfo option, out object? value, out bool agreed)
    {
        agreed = true;
        value = null;

        var declaringType = option.Property.DeclaringType;
        if (declaringType is null)
            return false;

        var seen = false;
        object? agreedValue = null;
        foreach (var holder in _commandBuilder.Commands)
        {
            if (!declaringType.IsAssignableFrom(holder.CommandType))
                continue;

            var holderProperty = CommandReflectionCache.ResolveHolderProperty(holder, option.Property);
            if (holderProperty is null)
            {
                agreed = false;
                return false;
            }

            object? candidate;
            if (holder.TryGetRegistrationValue(holderProperty, out var snapshot))
            {
                candidate = snapshot;
            }
            else if (holder.IsInstanceRegistration)
            {
                agreed = false;
                return false;
            }
            else
            {
                try
                {
                    candidate = holderProperty.GetValue(holder.Command);
                }
                catch
                {
                    agreed = false;
                    return false;
                }
            }

            if (!seen)
            {
                agreedValue = InitializerValueEquality.CloneIfArray(candidate);
                seen = true;
                continue;
            }

            if (!InitializerValueEquality.ValuesEqual(agreedValue, candidate))
            {
                agreed = false;
                return false;
            }
        }

        value = agreedValue;
        return seen;
    }

    private TypedCommandHolder? FindHolder(Type commandType)
    {
        foreach (var holder in _commandBuilder.Commands)
        {
            if (holder.CommandType == commandType)
                return holder;
        }

        return null;
    }

}
