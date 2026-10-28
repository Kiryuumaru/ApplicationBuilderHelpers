using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ApplicationBuilderHelpers.Models;

/// <summary>
/// Registration record pairing a command type with its instance and run-resolution policy.
/// </summary>
internal class TypedCommandHolder
{
    private static readonly object UnreadableDefault = new();

    private readonly Dictionary<PropertyInfo, object?>? _registrationValues;
    // Instance mutation history: properties written after registration. Not a registration fact.
    private readonly HashSet<PropertyInfo>? _instanceMutationHistory;

    internal TypedCommandHolder(
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType,
        ICommand command,
        bool isInstanceRegistration = false)
    {
        CommandType = commandType ?? throw new ArgumentNullException(nameof(commandType));
        Command = command ?? throw new ArgumentNullException(nameof(command));
        IsInstanceRegistration = isInstanceRegistration;
        if (isInstanceRegistration)
        {
            _registrationValues = CaptureRegistrationValues();
            _instanceMutationHistory = [];
        }
    }

    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type CommandType { get; }

    public ICommand Command { get; }

    public bool IsInstanceRegistration { get; }

    internal bool TryGetRegistrationValue(PropertyInfo property, out object? value)
    {
        value = null;
        if (_registrationValues is null || !_registrationValues.TryGetValue(property, out value))
            return false;
        return !ReferenceEquals(value, UnreadableDefault);
    }

    internal bool HasInstanceMutation(PropertyInfo property) =>
        _instanceMutationHistory?.Contains(property) == true;

    internal void RecordInstanceMutation(PropertyInfo property)
    {
        _instanceMutationHistory?.Add(property);
    }

    private Dictionary<PropertyInfo, object?> CaptureRegistrationValues()
    {
        var snapshot = new Dictionary<PropertyInfo, object?>();
        foreach (var property in CommandReflectionCache.Walk(CommandType))
        {
            if (!property.IsDefined(typeof(CommandOptionAttribute), inherit: true))
                continue;
            object? defaultValue;
            try
            {
                defaultValue = property.GetValue(Command);
                defaultValue = InitializerValueEquality.CloneIfArray(defaultValue);
            }
            catch
            {
                defaultValue = UnreadableDefault;
            }
            snapshot[property] = defaultValue;
        }
        return snapshot;
    }

    public ICommand CreateRunInstance()
    {
        if (IsInstanceRegistration)
            return Command;

        return (ICommand)(Activator.CreateInstance(CommandType)
            ?? throw new InvalidOperationException($"Unable to create an instance of command type '{CommandType.FullName}'."));
    }
}
