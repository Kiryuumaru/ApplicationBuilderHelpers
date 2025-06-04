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
public interface IApplicationCommand : IApplicationDependency
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

    /// <summary>
    /// Prepares the command for execution internally.
    /// </summary>
    /// <param name="stoppingToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal ValueTask CommandPreparationInternal(CancellationToken stoppingToken);

    /// <summary>
    /// Builds the application builder internally.
    /// </summary>
    /// <param name="stoppingToken">A token to cancel the operation.</param>
    /// <returns>An instance of <see cref="ApplicationHostBuilder{THostApplicationBuilder}"/>.</returns>
    internal ValueTask<ApplicationHostBuilder> ApplicationBuilderInternal(CancellationToken stoppingToken);

    /// <summary>
    /// Runs the application internally.
    /// </summary>
    /// <param name="applicationHost">The application host.</param>
    /// <param name="stoppingToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation.</returns>
    internal ValueTask RunInternal(ApplicationHost applicationHost, CancellationToken stoppingToken);
}
