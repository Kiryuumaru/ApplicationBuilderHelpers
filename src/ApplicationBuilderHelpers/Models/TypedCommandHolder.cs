using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Models;

internal class TypedCommandHolder([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, ICommand command)
{
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
    public Type CommandType { get; init; } = commandType ?? throw new ArgumentNullException(nameof(commandType));

    public ICommand Command { get; init; } = command ?? throw new ArgumentNullException(nameof(command));
}
