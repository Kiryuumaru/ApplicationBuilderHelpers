using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

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
    /// Resolves the command instance for one <c>RunAsync</c> pass: the shared
    /// caller-supplied instance for instance registrations, otherwise a fresh
    /// instance carrying property-initializer defaults so unprovided options
    /// reset each run. <c>CommandPreparation</c> then runs on the per-run copy.
    /// </summary>
    public ICommand CreateRunInstance()
    {
        if (IsInstanceRegistration)
            return Command;

        return (ICommand)(Activator.CreateInstance(CommandType)
            ?? throw new InvalidOperationException($"Unable to create an instance of command type '{CommandType.FullName}'."));
    }
}
