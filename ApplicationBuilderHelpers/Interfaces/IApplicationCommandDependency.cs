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
public interface IApplicationCommandDependency : IApplicationCommand, IApplicationDependency
{
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
