using Application.Authorization.Interfaces.Outbound;
using Infrastructure.Identity.ConfigureOptions;
using Infrastructure.Identity.Models;
using Infrastructure.Identity.Interfaces;
using Infrastructure.Identity.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Infrastructure.Identity.Extensions;

internal static class JwtServiceCollectionExtensions
{
    public static IServiceCollection AddJwtTokenServices(
        this IServiceCollection services,
        Func<IServiceProvider, CancellationToken, Task<JwtConfiguration>> jwtConfigurationFactory,
        object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jwtConfigurationFactory);

        if (serviceKey is null)
        {
            return AddDefaultJwtTokenServices(services, jwtConfigurationFactory);
        }
        else
        {
            return AddKeyedJwtTokenServices(services, jwtConfigurationFactory, serviceKey);
        }
    }

    public static IServiceCollection AddJwtTokenServices(
        this IServiceCollection services,
        Func<IServiceProvider, Task<JwtConfiguration>> jwtConfigurationFactory,
        object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jwtConfigurationFactory);

        return services.AddJwtTokenServices(
            (sp, _) => jwtConfigurationFactory(sp),
            serviceKey);
    }

    public static IServiceCollection AddJwtTokenServices(
        this IServiceCollection services,
        Func<IServiceProvider, JwtConfiguration> jwtConfigurationFactory,
        object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jwtConfigurationFactory);

        return services.AddJwtTokenServices(
            (sp, _) => Task.FromResult(jwtConfigurationFactory(sp)),
            serviceKey);
    }

    public static IServiceCollection AddJwtTokenServices(
        this IServiceCollection services,
        JwtConfiguration jwtConfiguration,
        object? serviceKey = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(jwtConfiguration);

        return services.AddJwtTokenServices(
            (_, _) => Task.FromResult(jwtConfiguration),
            serviceKey);
    }

    private static IServiceCollection AddDefaultJwtTokenServices(
        IServiceCollection services,
        Func<IServiceProvider, CancellationToken, Task<JwtConfiguration>> jwtConfigurationFactory)
    {
        // Infrastructure.Identity internal service
        services.AddScoped<IJwtTokenService>(sp =>
        {
            var lazyFactory = new Lazy<Func<CancellationToken, Task<JwtConfiguration>>>(
                () => ct => jwtConfigurationFactory(sp, ct));
            return new JwtTokenService(lazyFactory);
        });

        // Infrastructure implementation of Application's ITokenProvider interface
        services.AddScoped<ITokenProvider>(sp =>
        {
            var jwtTokenService = sp.GetRequiredService<IJwtTokenService>();
            return new TokenProvider(jwtTokenService);
        });

        return services;
    }

    private static IServiceCollection AddKeyedJwtTokenServices(
        IServiceCollection services,
        Func<IServiceProvider, CancellationToken, Task<JwtConfiguration>> jwtConfigurationFactory,
        object serviceKey)
    {
        // Infrastructure.Identity internal service (keyed)
        services.AddKeyedScoped<IJwtTokenService>(serviceKey, (sp, _) =>
        {
            var lazyFactory = new Lazy<Func<CancellationToken, Task<JwtConfiguration>>>(
                () => ct => jwtConfigurationFactory(sp, ct));
            return new JwtTokenService(lazyFactory);
        });

        // Infrastructure implementation of Application's ITokenProvider interface (keyed)
        services.AddKeyedScoped<ITokenProvider>(serviceKey, (sp, key) =>
        {
            var jwtTokenService = sp.GetRequiredKeyedService<IJwtTokenService>(key);
            return new TokenProvider(jwtTokenService);
        });

        return services;
    }

    public static IServiceCollection AddJwtBearerConfiguration(this IServiceCollection services)
    {
        services.AddSingleton<IConfigureOptions<JwtBearerOptions>, ConfigureJwtBearerOptions>();
        return services;
    }
}
