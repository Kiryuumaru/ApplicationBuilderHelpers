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
/// Built host wrapper that runs dependency hooks then starts the <see cref="IHost"/>.
/// </summary>
public abstract class ApplicationHost(IHostApplicationBuilder builder, IHost host) : ApplicationHostBuilderBase(builder)
{
    /// <summary>
    /// Gets the built <see cref="IHost"/> started by <c>Run</c>.
    /// </summary>
    public IHost Host { get; protected set; } = host;

    /// <summary>
    /// Gets the service provider of the built <see cref="Host"/>.
    /// </summary>
    public new IServiceProvider Services => Host.Services;

    internal ConsoleOutput ConsoleOutput { get; set; } = new ConsoleOutput();

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

    private void ShowErrorMessage(string message, CommandErrorKind kind, string? commandName)
    {
        var executableName = AssemblyHelpers.GetAutoDetectedExecutableName();

        ConsoleOutput.WriteLineError($"Error: {message}", ConsoleColor.Red);

        ConsoleOutput.WriteLineError();

        ConsoleOutput.WriteLineError(CommandErrorFooter.Resolve(kind, executableName, commandName));
    }
}

/// <summary>
/// Built host wrapper typed to one host-builder type.
/// </summary>
/// <typeparam name="THostApplicationBuilder">The host-builder type this host was built from.</typeparam>
public class ApplicationHost<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder>(THostApplicationBuilder builder, IHost host) : ApplicationHost(builder, host)
    where THostApplicationBuilder : IHostApplicationBuilder
{
    /// <summary>
    /// Gets the underlying builder cast to <typeparamref name="THostApplicationBuilder"/>.
    /// </summary>
    public new THostApplicationBuilder Builder => (THostApplicationBuilder)base.Builder;
}
