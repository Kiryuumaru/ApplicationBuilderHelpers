using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Represents the built application host wrapping the configured <see cref="IHost"/>.
/// </summary>
public abstract class ApplicationHost(IHostApplicationBuilder builder, IHost host) : ApplicationHostBuilderBase(builder)
{
    /// <summary>
    /// Gets the <see cref="IHost"/> created from the ApplicationHostBuilder Build method.
    /// </summary>
    public IHost Host { get; protected set; } = host;

    /// <summary>
    /// Gets the <see cref="IServiceProvider"/> associated with the <see cref="Host"/>.
    /// </summary>
    public new IServiceProvider Services => Host.Services;

    internal ConsoleOutput ConsoleOutput { get; set; } = new ConsoleOutput();

    /// <summary>
    /// Runs the configured application by running <c>AddMiddlewares</c>,
    /// <c>AddMappings</c>, <c>RunPreparation</c>, and
    /// <c>RunPreparationAsync</c>.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. Returns 0 on success or the <see cref="Exceptions.CommandException"/> exit code when the host run reports a command error.</returns>
    internal async Task<int> Run(CancellationToken cancellationToken = default)
    {
        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddMiddlewares(this, Host);
        }

        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.AddMappings(this, Host);
        }

        foreach (var applicationDependency in ApplicationDependencies)
        {
            applicationDependency.RunPreparation(this);
        }

        await Task.WhenAll(ApplicationDependencies.Select(ad => Task.Run(async () => await ad.RunPreparationAsync(this, cancellationToken), cancellationToken)));

        try
        {
            await HostingAbstractionsHostExtensions.RunAsync(Host, cancellationToken);
        }
        catch (CommandException ex)
        {
            ShowErrorMessage(ex.Message, ex.Kind, ex.CommandName);
            return ex.ExitCode;
        }

        return 0;
    }

    /// <summary>
    /// Shows an error message with footer information.
    /// Uses the detected executable name. Footer selection depends on
    /// <see cref="CommandErrorKind"/>.
    /// </summary>
    private void ShowErrorMessage(string message, CommandErrorKind kind, string? commandName)
    {
        var executableName = AssemblyHelpers.GetAutoDetectedExecutableName();

        ConsoleOutput.WriteLineError($"Error: {message}", ConsoleColor.Red);

        ConsoleOutput.WriteLineError();

        ConsoleOutput.WriteLineError(CommandErrorFooter.Resolve(kind, executableName, commandName));
    }
}

/// <summary>
/// Represents the built application host for a specific host application builder type.
/// </summary>
/// <typeparam name="THostApplicationBuilder">The type of the host application builder.</typeparam>
public class ApplicationHost<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder>(THostApplicationBuilder builder, IHost host) : ApplicationHost(builder, host)
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Gets the underlying <see cref="IHostApplicationBuilder"/>.
    /// </summary>
    public new THostApplicationBuilder Builder => (THostApplicationBuilder)base.Builder;
}
