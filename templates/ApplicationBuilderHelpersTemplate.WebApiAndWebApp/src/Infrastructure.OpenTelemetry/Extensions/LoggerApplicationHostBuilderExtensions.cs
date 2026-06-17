using Application.Shared.Extensions;
using Application.Logger.Extensions;
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;
using Infrastructure.OpenTelemetry.Enrichers;
using Infrastructure.OpenTelemetry.Interfaces;
using Infrastructure.OpenTelemetry.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Logs;
using OpenTelemetry.Metrics;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using Serilog.Templates;
using Serilog.Templates.Themes;

namespace Infrastructure.OpenTelemetry.Extensions;

public static class LoggerApplicationHostBuilderExtensions
{
    static readonly TemplateTheme ConsoleTheme = new(new Dictionary<TemplateThemeStyle, string>
    {
        [TemplateThemeStyle.Text] = "\u001b[38;5;0253m",
        [TemplateThemeStyle.SecondaryText] = "\u001b[38;5;0246m",
        [TemplateThemeStyle.TertiaryText] = "\u001b[38;5;0242m",
        [TemplateThemeStyle.Invalid] = "\u001b[33;1m",
        [TemplateThemeStyle.Null] = "\u001b[38;5;0038m",
        [TemplateThemeStyle.Name] = "\u001b[38;5;0081m",
        [TemplateThemeStyle.String] = "\u001b[38;5;0216m",
        [TemplateThemeStyle.Number] = "\u001b[38;5;151m",
        [TemplateThemeStyle.Boolean] = "\u001b[38;5;0038m",
        [TemplateThemeStyle.Scalar] = "\u001b[38;5;0079m",
        [TemplateThemeStyle.LevelVerbose] = "\u001b[38;5;0242m",
        [TemplateThemeStyle.LevelDebug] = "\u001b[38;5;0246m",
        [TemplateThemeStyle.LevelInformation] = "\u001b[37;1m",
        [TemplateThemeStyle.LevelWarning] = "\u001b[38;5;0229m",
        [TemplateThemeStyle.LevelError] = "\u001b[38;5;0197m\u001b[48;5;0238m",
        [TemplateThemeStyle.LevelFatal] = "\u001b[37;1m\u001b[48;5;197m"
    });

    public static ApplicationHostBuilder AddLoggerConfiguration(this ApplicationHostBuilder applicationBuilder, Dictionary<string, object?>? scopeMap = null)
    {
        scopeMap ??= [];

        var configuredLogger = Configure(new LoggerConfiguration(), applicationBuilder, scopeMap).CreateLogger();

        Log.Logger = configuredLogger;

        applicationBuilder.Builder.Logging.ClearProviders();

        if (applicationBuilder.Builder is WebApplicationBuilder webApplicationBuilder)
        {
            webApplicationBuilder.Host.UseSerilog(configuredLogger, dispose: true);
        }
        else
        {
            applicationBuilder.Builder.Logging.AddSerilog(configuredLogger, dispose: true);
        }

        applicationBuilder.Builder.Logging.SetMinimumLevel(applicationBuilder.Configuration.GetLoggerLevel());

        applicationBuilder.Builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        return applicationBuilder;
    }

    public static ApplicationHostBuilder AddLoggerServices(this ApplicationHostBuilder applicationBuilder)
    {
        applicationBuilder.Builder.Services
            .AddOpenTelemetry()
            .WithLogging(logging =>
            {
                if (applicationBuilder.Builder is WebApplicationBuilder &&
                    applicationBuilder.Configuration.ContainsRefValue("OTEL_EXPORTER_OTLP_ENDPOINT"))
                {
                    logging.AddOtlpExporter();
                }
            })
            .WithMetrics(metrics =>
            {
                if (applicationBuilder.Configuration.ContainsRefValue("OTEL_EXPORTER_OTLP_ENDPOINT"))
                {
                    metrics.AddOtlpExporter();
                }

                metrics
                    .AddAspNetCoreInstrumentation()
                    .AddHttpClientInstrumentation()
                    .AddRuntimeInstrumentation()
                    .AddMeter("Application.Edge.ServiceWrapper");

            })
            .WithTracing(tracing =>
            {
                if (applicationBuilder.Configuration.ContainsRefValue("OTEL_EXPORTER_OTLP_ENDPOINT"))
                {
                    tracing.AddOtlpExporter();
                }
                if (applicationBuilder.Builder is WebApplicationBuilder)
                {
                    tracing.AddAspNetCoreInstrumentation(options =>
                    {
                        // Exclude health check requests from tracing
                        options.Filter = context =>
                            !context.Request.Path.StartsWithSegments("/health") &&
                            !context.Request.Path.StartsWithSegments("/alive");
                    });
                }

                tracing
                    .AddHttpClientInstrumentation()
                    .AddSource(applicationBuilder.Builder.Environment.ApplicationName)
                    .AddSource("Application.Edge.ServiceWrapper");
            });

        applicationBuilder.Services.AddTransient<ILoggerReader, SerilogLoggerReader>();

        return applicationBuilder;
    }

    public static void AddLoggerMiddlewares(this ApplicationHost applicationHost)
    {
        if (applicationHost.Host is WebApplication webApplication)
        {
            // Enable Serilog request logging for web applications
            webApplication.UseSerilogRequestLogging(options =>
            {
                // Exclude health check endpoints from request logging
                options.GetLevel = (httpContext, elapsed, ex) =>
                {
                    if (httpContext.Request.Path.StartsWithSegments("/health") ||
                        httpContext.Request.Path.StartsWithSegments("/alive"))
                    {
                        return LogEventLevel.Debug;
                    }

                    return ex != null ? LogEventLevel.Error : LogEventLevel.Information;
                };
            });
        }
    }

    private static LoggerConfiguration Configure(LoggerConfiguration loggerConfiguration, ApplicationHostBuilder applicationBuilder, Dictionary<string, object?>? scopeMap)
    {
        var hostApplicationBuilder = applicationBuilder.Builder;

        LogLevel logLevel = hostApplicationBuilder.Configuration.GetLoggerLevel();

        var defaultEnricher = new DefaultLogEnricher(hostApplicationBuilder.Configuration, scopeMap);

        loggerConfiguration = loggerConfiguration
            .Destructure.ToMaximumStringLength(1024)
            .Destructure.ToMaximumDepth(5)
            .Destructure.ToMaximumCollectionCount(10)
            .Enrich.FromLogContext()
            .Enrich.WithProperty("ApplicationName", hostApplicationBuilder.Environment.ApplicationName)
            .Enrich.WithProperty("Environment", hostApplicationBuilder.Environment.EnvironmentName)
            .Enrich.WithMachineName()
            .Enrich.WithEnvironmentUserName()
            .Enrich.WithProcessId()
            .Enrich.WithThreadId()
            .Enrich.With(defaultEnricher);

        if (logLevel != LogLevel.None)
        {
            LogEventLevel logEventLevel = MapLogLevel(logLevel);

            loggerConfiguration = loggerConfiguration
                .MinimumLevel.Is(logEventLevel);

            ConfigureMicrosoftLogLevels(loggerConfiguration, logEventLevel);

            loggerConfiguration = loggerConfiguration
                .WriteTo.Console(new ExpressionTemplate(
                    template: "[{@t:yyyy-MM-dd HH:mm:ss} {@l:u3}]{#if SourceContext is not null} [{Substring(SourceContext, LastIndexOf(SourceContext, '.') + 1)}{#if SourceContextAction is not null}:{SourceContextAction}{#end}]{#end} {@m} \n{@x}",
                    theme: ConsoleTheme,
                    applyThemeWhenOutputIsRedirected: applicationBuilder.Configuration.GetApplyThemeWhenOutputIsRedirected()),
                    restrictedToMinimumLevel: logEventLevel);

            if (hostApplicationBuilder.Configuration.ContainsRefValue("OTEL_EXPORTER_OTLP_ENDPOINT"))
            {
                loggerConfiguration = loggerConfiguration
                    .WriteTo.OpenTelemetry(
                        restrictedToMinimumLevel: logEventLevel);
            }
        }

        var logsDumpDir = hostApplicationBuilder.Configuration.GetLogsDumpDirectory();
        if (logsDumpDir != null)
        {
            loggerConfiguration = loggerConfiguration
                .MinimumLevel.Verbose()
                .WriteTo.File(
                    formatter: new CompactJsonFormatter(),
                    path: logsDumpDir / "log-.jsonl",
                    restrictedToMinimumLevel: LogEventLevel.Verbose,
                    rollingInterval: RollingInterval.Hour,
                    retainedFileCountLimit: 168, // Keep 7 days worth of hourly logs (7 * 24)
                    fileSizeLimitBytes: 100_000_000, // 100 MB per file
                    rollOnFileSizeLimit: true,
                    buffered: false); // Disable buffering to prevent memory accumulation
        }

        return loggerConfiguration;
    }

    private static LogEventLevel MapLogLevel(LogLevel logLevel)
    {
        return logLevel switch
        {
            LogLevel.Trace => LogEventLevel.Verbose,
            LogLevel.Debug => LogEventLevel.Debug,
            LogLevel.Information => LogEventLevel.Information,
            LogLevel.Warning => LogEventLevel.Warning,
            LogLevel.Error => LogEventLevel.Error,
            LogLevel.Critical => LogEventLevel.Fatal,
            LogLevel.None => LogEventLevel.Fatal,
            _ => LogEventLevel.Information
        };
    }

    private static LoggerConfiguration ConfigureMicrosoftLogLevels(LoggerConfiguration loggerConfiguration, LogEventLevel logEventLevel)
    {
        if (logEventLevel <= LogEventLevel.Information)
        {
            return loggerConfiguration
                .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("Polly", LogEventLevel.Warning);
        }
        else
        {
            return loggerConfiguration
                .MinimumLevel.Override("Microsoft", logEventLevel)
                .MinimumLevel.Override("Microsoft.AspNetCore", logEventLevel)
                .MinimumLevel.Override("Microsoft.Hosting.Lifetime", logEventLevel)
                .MinimumLevel.Override("System.Net.Http.HttpClient", logEventLevel)
                .MinimumLevel.Override("Polly", logEventLevel);
        }
    }
}
