using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ApplicationBuilderHelpers.Models;

internal class TypedCommandHolder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, ICommand command, bool isInstanceRegistration = false)
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type CommandType { get; init; } = commandType ?? throw new ArgumentNullException(nameof(commandType));

    public ICommand Command { get; init; } = command ?? throw new ArgumentNullException(nameof(command));

    /// <summary>
    /// True when the holder wraps a caller-supplied instance (<c>AddCommand(ICommand)</c>),
    /// which keeps identity across runs. Type registrations (<c>AddCommand{T}()</c>)
    /// resolve a fresh instance per run so bound values cannot leak across repeated
    /// <c>RunAsync</c> calls on one builder.
    /// </summary>
    public bool IsInstanceRegistration { get; init; } = isInstanceRegistration;

    /// <summary>
    /// Marker stored when a registration-time default cannot be read.
    /// Comparisons involving it fail closed (stay local).
    /// </summary>
    private static readonly object UnreadableDefault = new();

    private readonly object _snapshotLock = new();
    private Dictionary<PropertyInfo, object?>? _initializerDefaults;

    /// <summary>
    /// Registration-time property-initializer default for an option property.
    /// For caller-supplied instance registrations the snapshot is captured once
    /// on first read, which always happens during the first hierarchy build
    /// before value binding can mutate the shared instance; later runs compare
    /// the frozen copy instead of live (possibly already-bound) values. Returns
    /// false for type registrations, unknown properties, or unreadable defaults —
    /// all fail closed at the promotion gate. Array defaults are cloned so later
    /// replacement cannot alias the copy.
    /// </summary>
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
                if (defaultValue is Array array)
                    defaultValue = (Array)array.Clone();
            }
            catch
            {
                defaultValue = UnreadableDefault;
            }
            snapshot[property] = defaultValue;
        }
        return snapshot;
    }

    /// <summary>
    /// Resolves the command instance for one <c>RunAsync</c> pass: the shared
    /// caller-supplied instance for instance registrations, otherwise a fresh
    /// instance carrying property-initializer defaults so unprovided options
    /// reset each run. <c>CommandPreparation</c> then runs on the per-run copy.
    /// Snapshotting has moved into <c>TryGetInitializerDefault</c> (lazy, one-shot
    /// at first gate read); this stays the shared-instance pass-through.
    /// </summary>
    public ICommand CreateRunInstance()
    {
        if (IsInstanceRegistration)
            return Command;

        return (ICommand)(Activator.CreateInstance(CommandType)
            ?? throw new InvalidOperationException($"Unable to create an instance of command type '{CommandType.FullName}'."));
    }
}
