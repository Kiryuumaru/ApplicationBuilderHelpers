using AbsolutePathHelpers;
using Application.AppEnvironment.Interfaces.Inbound;
using Application.Shared.Extensions;
using Application.Shared.Interfaces.Inbound;
using Application.Shared.Interfaces.Outbound;
using Application.Credential.Extensions;
using Application.Logger.Extensions;
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

    [CommandOption(
        "logs-dump",
        EnvironmentVariable = "LOGS_DUMP",
        Description = "Logs dump to directory.")]
    public AbsolutePath? LogsDumpDirectory { get; set; }

    [CommandOption(
        "credentials-override",
        EnvironmentVariable = "CREDENTIALS_OVERRIDE",
        Description = "Credentials override")]
    public string? CredentialsOverrideBase64 { get; set; }

    [CommandOption(
        "home-path-override",
        EnvironmentVariable = "HOME_PATH_OVERRIDE",
        Description = "Home path override")]
    public AbsolutePath HomePathOverride { get; set; } = AbsolutePath.Create(Environment.CurrentDirectory);

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfigurations(applicationBuilder, configuration);

        configuration.SetServiceName(ApplicationConstants.AppName);

        configuration.SetCredentials(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(ApplicationConstants.BuildPayload)), true);

        if (!string.IsNullOrEmpty(CredentialsOverrideBase64))
        {
            configuration.SetCredentials(System.Text.Encoding.UTF8.GetString(Convert.FromBase64String(CredentialsOverrideBase64)), true);
        }

        configuration.SetLoggerLevel(LogLevel);

        configuration.SetLogsDumpDirectory(LogsDumpDirectory);

        configuration.SetHomePath(HomePathOverride);
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddSingleton(sp => ApplicationConstants);

        services.Configure<ConsoleLifetimeOptions>(opts => opts.SuppressStatusMessages = true);
    }

    /// <inheritdoc/>
    protected override async ValueTask Run(ApplicationHost<THostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
    {
        using var scope = applicationHost.Services.CreateScope();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<BaseCommand<THostApplicationBuilder>>>();
        var appEnvironmentService = scope.ServiceProvider.GetRequiredService<IAppEnvironmentService>();

        using var _ = logger.BeginScopeMap();

        var appEnv = await appEnvironmentService.GetEnvironment(cancellationTokenSource.Token);

        Console.WriteLine();
        Console.WriteLine(BuildAppBanner());
        Console.WriteLine();

        logger.LogInformation("Running on: AppEnvironment={AppEnvironment}", appEnv.Environment);
    }

    private string BuildAppBanner()
    {
        return $"""
                  /$$$$$$   /$$$$$$   /$$$$$$  /$$$$$$$$
                 /$$__  $$ /$$__  $$ /$$__  $$|__  $$__/
                | $$  \__/| $$  \ $$| $$  \ $$   | $$   
                | $$ /$$$$| $$  | $$| $$$$$$$$   | $$   
                | $$|_  $$| $$  | $$| $$__  $$   | $$   
                | $$  \ $$| $$  | $$| $$  | $$   | $$   
                |  $$$$$$/|  $$$$$$/| $$  | $$   | $$   
                 \______/  \______/ |__/  |__/   |__/    
                                           by Kiryuumaru

                {ApplicationConstants.AppTitle}
                v{ApplicationConstants.Version}
            """;
    }
}
