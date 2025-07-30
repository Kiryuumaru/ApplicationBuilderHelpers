using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Interfaces;

/// <summary>
/// Represents a command that can be executed within the application.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public interface ICommandBuilder : ICommandTypeParserCollection, IApplicationDependencyCollection
{
    internal List<ICommand> Commands { get; }

    internal string? ExecutableName { get; set; }

    internal string? ExecutableTitle { get; set; }

    internal string? ExecutableDescription { get; set; }
}
