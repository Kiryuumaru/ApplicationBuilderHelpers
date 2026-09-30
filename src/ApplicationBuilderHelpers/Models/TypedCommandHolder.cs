using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ApplicationBuilderHelpers.Models;

/// <summary>
/// Registration record pairing a command type with its instance and run-resolution policy.
/// </summary>
internal class TypedCommandHolder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, ICommand command, bool isInstanceRegistration = false)
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type CommandType { get; init; } = commandType ?? throw new ArgumentNullException(nameof(commandType));

    public ICommand Command { get; init; } = command ?? throw new ArgumentNullException(nameof(command));

    public bool IsInstanceRegistration { get; init; } = isInstanceRegistration;

    private static readonly object UnreadableDefault = new();

    private readonly object _snapshotLock = new();
    private Dictionary<PropertyInfo, object?>? _initializerDefaults;

    internal bool TryGetInitializerDefault(PropertyInfo property, out object? value)
    {
        value = null;
        if (!IsInstanceRegistration)
            return false;

        lock (_snapshotLock)
        {
            _initializerDefaults ??= CaptureInitializerDefaults();
            if (!_initializerDefaults.TryGetValue(property, out value))
                return false;
            return !ReferenceEquals(value, UnreadableDefault);
        }
    }

    private Dictionary<PropertyInfo, object?> CaptureInitializerDefaults()
    {
        var snapshot = new Dictionary<PropertyInfo, object?>();
        foreach (var property in CommandType.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
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
