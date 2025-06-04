using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
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
public interface IApplicationCommand
{
    /// <summary>
    /// Gets the name of the command.
    /// </summary>
    string? Name { get; }

    /// <summary>
    /// Gets the description of the command.
    /// </summary>
    string? Description { get; }

    /// <summary>
    /// Gets a value indicating whether the application should exit after the <see cref="Run(ApplicationHost{THostApplicationBuilder}, CancellationToken)"/> method is complete.
    /// </summary>
    bool ExitOnRunComplete { get; }
}
