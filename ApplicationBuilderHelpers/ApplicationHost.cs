using ApplicationBuilderHelpers.Exceptions;
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
            Console.WriteLine(ex.Message);
            return ex.ExitCode;
        }

        return 0;
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
