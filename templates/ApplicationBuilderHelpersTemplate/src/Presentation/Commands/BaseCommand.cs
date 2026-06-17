using Application.EmbeddedConfig.Extensions;
using Application.Logger.Extensions;
using Application.Shared.Interfaces.Inbound;
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics.CodeAnalysis;

namespace Presentation.Commands;

public abstract class BaseCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] THostApplicationBuilder> : Command<THostApplicationBuilder>
    where THostApplicationBuilder : IHostApplicationBuilder
{
    public abstract IApplicationConstants ApplicationConstants { get; }

    [CommandOption(
        'l', "log-level",
        EnvironmentVariable = "LOG_LEVEL",
        Description = "Level of logs to show.")]
    public LogLevel LogLevel { get; set; } = LogLevel.Information;

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfigurations(applicationBuilder, configuration);

        configuration.LoadEncryptedEmbeddedConfig(ApplicationConstants);
        configuration.LoggerLevel = LogLevel;
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        services.AddSingleton(ApplicationConstants);

        services.Configure<ConsoleLifetimeOptions>(opts => opts.SuppressStatusMessages = true);

        // Default fallback logging - Infrastructure can ClearProviders() and replace
        services.AddLogging(builder =>
        {
            builder.SetMinimumLevel(applicationBuilder.Configuration.LoggerLevel);
            builder.AddConsole();
        });

        base.AddServices(applicationBuilder, services);
    }

    protected override async ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        using var scope = applicationHost.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BaseCommand<THostApplicationBuilder>>>();

        Console.WriteLine();
        Console.WriteLine(BuildAppBanner());
        Console.WriteLine();

        logger.LogInformation("Application started: {AppName} v{Version}", ApplicationConstants.AppName, ApplicationConstants.Version);
    }

    private string BuildAppBanner()
    {
        return $"""
                  ____  _       _         ____ _     ___
                 |  _ \| | __ _(_)_ __   / ___| |   |_ _|
                 | |_) | |/ _` | | '_ \ | |   | |    | |
                 |  __/| | (_| | | | | || |___| |___ | |
                 |_|   |_|\__,_|_|_| |_| \____|_____|___|

                  {ApplicationConstants.AppTitle} v{ApplicationConstants.Version}
                """;
    }
}
