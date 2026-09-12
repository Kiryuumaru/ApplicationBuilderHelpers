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
/// Represents a builder for managing application dependencies and running the configured application.
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
    /// Runs the configured application.
    /// </summary>
    /// <param name="cancellationToken">A token to cancel the operation.</param>
    /// <returns>A task that represents the asynchronous operation. Returns an integer exit code.</returns>
    /// <exception cref="Exception">Thrown if there is an error during application startup.</exception>
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
    /// Shows a styled error message with helpful footer information, mirroring the
    /// command-line gateway path. Uses the auto-detected executable name and the
    /// default error color since no <see cref="Interfaces.ICommandBuilder"/> theme
    /// is reachable here without new coupling.
    /// Footer selection dispatches on <see cref="CommandErrorKind"/>, never on message text.
    /// </summary>
    private void ShowErrorMessage(string message, CommandErrorKind kind, string? commandName)
    {
        var executableName = AssemblyHelpers.GetAutoDetectedExecutableName();

        ConsoleOutput.WriteLineError($"Error: {message}", ConsoleColor.Red);

        ConsoleOutput.WriteLineError();

        switch (kind)
        {
            case CommandErrorKind.RequiresSubcommand:
                if (!string.IsNullOrEmpty(commandName))
                {
                    ConsoleOutput.WriteLineError($"Run '{executableName} {commandName} --help' to see available subcommands and options.");
                }
                else
                {
                    ConsoleOutput.WriteLineError($"Run '{executableName} --help' to see available commands and options.");
                }
                break;
            case CommandErrorKind.UnknownOption:
            case CommandErrorKind.MissingRequired:
                ConsoleOutput.WriteLineError($"Run '{executableName} <command> --help' for more information on specific command options.");
                break;
            default:
                ConsoleOutput.WriteLineError($"Run '{executableName} --help' for more information on available commands and options.");
                break;
        }
    }
}

/// <summary>
/// Represents a builder for managing application dependencies and running the configured application.
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
