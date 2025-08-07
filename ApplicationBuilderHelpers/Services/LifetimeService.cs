using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Services;

/// <summary>
/// Provides lifetime management services for creating cancellation tokens and token sources.
/// </summary>
/// <param name="serviceProvider">The service provider used to resolve dependencies.</param>
public class LifetimeService(IServiceProvider serviceProvider)
{
    /// <summary>
    /// Creates a new cancellation token source using the lifetime global service.
    /// </summary>
    /// <returns>A new <see cref="CancellationTokenSource"/> instance.</returns>
    public CancellationTokenSource CreateCancellationTokenSource()
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        return lifetimeGlobalService.CreateCancellationTokenSource();
    }

    /// <summary>
    /// Creates a new cancellation token using the lifetime global service.
    /// </summary>
    /// <returns>A new <see cref="CancellationToken"/> instance.</returns>
    public CancellationToken CreateCancellationToken()
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        return lifetimeGlobalService.CreateCancellationToken();
    }

    /// <summary>
    /// Registers a synchronous callback to be invoked when the application is exiting.
    /// </summary>
    /// <param name="callback">The action to be executed when the application is exiting.</param>
    public void ApplicationExitingCallback(Action callback)
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        lifetimeGlobalService.ApplicationExitingCallback(callback);
    }

    /// <summary>
    /// Registers an asynchronous callback to be invoked when the application is exiting.
    /// </summary>
    /// <param name="callback">The asynchronous function to be executed when the application is exiting.</param>
    public void ApplicationExitingCallback(Func<Task> callback)
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        lifetimeGlobalService.ApplicationExitingCallback(callback);
    }

    /// <summary>
    /// Registers a synchronous callback to be invoked when the application is exited.
    /// </summary>
    /// <param name="callback">The action to be executed when the application is exited.</param>
    public void ApplicationExitedCallback(Action callback)
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        lifetimeGlobalService.ApplicationExitedCallback(callback);
    }

    /// <summary>
    /// Registers an asynchronous callback to be invoked when the application is exited.
    /// </summary>
    /// <param name="callback">The asynchronous function to be executed when the application is exited.</param>
    public void ApplicationExitedCallback(Func<Task> callback)
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        lifetimeGlobalService.ApplicationExitedCallback(callback);
    }
}
