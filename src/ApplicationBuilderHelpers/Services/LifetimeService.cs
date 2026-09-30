using Microsoft.Extensions.DependencyInjection;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Services;

/// <summary>Scoped facade over the run's <see cref="LifetimeGlobalService"/>.</summary>
/// <param name="serviceProvider">The service provider.</param>
public class LifetimeService(IServiceProvider serviceProvider)
{
    /// <summary>Derives a linked source from the run root; the caller owns disposal.</summary>
    /// <returns>A new <see cref="CancellationTokenSource"/> instance.</returns>
    public CancellationTokenSource CreateCancellationTokenSource()
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        return lifetimeGlobalService.CreateCancellationTokenSource();
    }

    /// <summary>Exposes the run-root token.</summary>
    /// <returns>A new <see cref="CancellationToken"/> instance.</returns>
    public CancellationToken CreateCancellationToken()
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        return lifetimeGlobalService.CreateCancellationToken();
    }

    /// <summary>Registers a sync callback for the exiting phase.</summary>
    /// <param name="callback">The action to execute when exiting.</param>
    public void ApplicationExitingCallback(Action callback)
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        lifetimeGlobalService.ApplicationExitingCallback(callback);
    }

    /// <summary>Registers an async callback for the exiting phase.</summary>
    /// <param name="callback">The function to execute when exiting.</param>
    public void ApplicationExitingCallback(Func<Task> callback)
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        lifetimeGlobalService.ApplicationExitingCallback(callback);
    }

    /// <summary>Registers a sync callback for the exited phase.</summary>
    /// <param name="callback">The action to execute when exited.</param>
    public void ApplicationExitedCallback(Action callback)
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        lifetimeGlobalService.ApplicationExitedCallback(callback);
    }

    /// <summary>Registers an async callback for the exited phase.</summary>
    /// <param name="callback">The function to execute when exited.</param>
    public void ApplicationExitedCallback(Func<Task> callback)
    {
        using var scope = serviceProvider.CreateScope();
        var lifetimeGlobalService = scope.ServiceProvider.GetRequiredService<LifetimeGlobalService>();
        lifetimeGlobalService.ApplicationExitedCallback(callback);
    }
}
