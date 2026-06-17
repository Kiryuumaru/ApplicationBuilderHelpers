using Application.Shared.Interfaces.Inbound;
using Application.Shared.Extensions;
using Application.Logger.Extensions;
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using DisposableHelpers.Attributes;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Nuke.Common.IO;
using NukeBuildHelpers.RunContext.Interfaces;
using NukeBuildHelpers.RunContext.Models;
using Presentation.Commands;
using Semver;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Common;

partial class Build
{
    public static int Main() => Execute<Build>(x => x.Interactive);

    private async Task<ApplicationRuntime> StartApplicationRuntime(IRunContext runContext)
    {
        if (string.IsNullOrEmpty(applicationCredentials) && (RootDirectory / "creds.json").Exists())
        {
            var credsContent = (RootDirectory / "creds.json").ReadAllText();
            applicationCredentials = Convert.ToBase64String(Encoding.UTF8.GetBytes(credsContent));
        }
        if (string.IsNullOrEmpty(applicationCredentials))
        {
            throw new ArgumentException("applicationCredentials is not set.");
        }
        try
        {
            byte[] credBytes = Convert.FromBase64String(applicationCredentials);
            string credString = Encoding.UTF8.GetString(credBytes);
            var envCreds = JsonNode.Parse(credString)?.AsObject()
                ?? throw new ArgumentException("applicationCredentials is not a valid JSON object.");
            (RootDirectory / "creds.json").WriteAllText(envCreds.ToJsonString());
        }
        catch (Exception ex)
        {
            throw new ArgumentException("applicationCredentials is not a valid value.", ex);
        }
        string buildPayload;
        try
        {
            string credString = Encoding.UTF8.GetString(Convert.FromBase64String(applicationCredentials));
            var envCreds = JsonNode.Parse(credString)?.AsObject()
                ?? throw new ArgumentException("applicationCredentials is not a valid JSON object.");
            buildPayload = Convert.ToBase64String(Encoding.UTF8.GetBytes(envCreds.ToJsonString()));

        }
        catch (Exception ex)
        {
            throw new ArgumentException("applicationCredentials is not a valid value.", ex);
        }

        CancellationTokenSource cts = new();
        ApplicationRuntime? applicationRuntime = null;

        Task.Run(async () =>
        {
            var (app, env, binRuntime, buildId, projectVersion, notes) = ExpandRunContext(runContext);
            int ret = await ApplicationBuilder.Create()
                .AddCommand(new ApplicationBuilderCommand(env, buildPayload, async (sp, ct) =>
                {
                    applicationRuntime = new ApplicationRuntime(runContext, sp, cts);
                    await cts.Token.WhenCanceled();
                }))
                .AddApplication<global::Application.Application>()
                .RunAsync([], cts.Token);

            if (ret != 0)
            {
                Log.Error("ApplicationBuilder failed with code {ReturnCode}.", ret);
            }
        }).Forget();

        while (applicationRuntime == null && !cts.IsCancellationRequested)
        {
            await Task.Delay(100);
        }

        if (applicationRuntime == null)
        {
            throw new Exception("Failed to start ApplicationBuilder.");
        }

        return applicationRuntime;
    }

    static (AppRunContext app, string env, string binRuntime, string buildId, SemVersion projectVersion, string notes) ExpandRunContext(IRunContext context)
    {
        var app = context.Apps.First().Value;
        string env;
        string binRuntime;
        string buildId;
        string notes = "";
        SemVersion projectVersion;
        if (app.PullRequestVersion != null)
        {
            buildId = app.PullRequestVersion.BuildId.ToString();
            binRuntime = $"pr.{app.PullRequestVersion.PullRequestNumber}";
            projectVersion = app.PullRequestVersion.Version;
            env = app.PullRequestVersion.Environment.ToLowerInvariant();
        }
        else if (app.BumpVersion != null)
        {
            buildId = app.BumpVersion.BuildId.ToString();
            binRuntime = $"{app.BumpVersion.Version}";
            projectVersion = app.BumpVersion.Version;
            env = app.BumpVersion.Environment.ToLowerInvariant();
            notes = app.BumpVersion.ReleaseNotes;
        }
        else
        {
            buildId = app.AppVersion.BuildId.ToString();
            binRuntime = $"{app.AppVersion.Environment.ToLower()}.preview";
            projectVersion = app.AppVersion.Version;
            env = app.AppVersion.Environment.ToLowerInvariant();
        }

        //if (app.RunType == NukeBuildHelpers.Common.Enums.RunType.Local)
        //{
        //    env = "alpha";
        //    binRuntime = "alpha";
        //}

        return (app, env, binRuntime, buildId, projectVersion, notes);
    }

    class ApplicationBuilderCommand(string appTag, string buildPayload, Func<IServiceProvider, CancellationToken, Task> callback) : BaseCommand<HostApplicationBuilder>
    {
        public override IApplicationConstants ApplicationConstants { get; } = new ApplicationConstants(appTag, buildPayload);

        protected override ValueTask<HostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
        {
            return new ValueTask<HostApplicationBuilder>(Microsoft.Extensions.Hosting.Host.CreateApplicationBuilder());
        }

        public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, Microsoft.Extensions.Configuration.IConfiguration configuration)
        {
            base.AddConfigurations(applicationBuilder, configuration);
            configuration.SetLoggerLevel(LogLevel.Debug);
            configuration.SetApplyThemeWhenOutputIsRedirected(true);
        }

        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            base.AddServices(applicationBuilder, services);
            services.Configure<ConsoleLifetimeOptions>(opts => opts.SuppressStatusMessages = true);
        }

        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            var logger = applicationHost.Services.GetRequiredService<ILogger<ApplicationBuilderCommand>>();

            using var _ = logger.BeginScopeMap();

            try
            {
                await callback(applicationHost.Services, cancellationTokenSource.Token);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ApplicationBuilderCommand failed.");
                throw new CommandException($"ApplicationBuilderCommand failed: {ex.Message}", -1);
            }
            finally
            {
                cancellationTokenSource.Cancel();
            }
        }
    }

    class ApplicationConstants(string appTag, string buildPayload) : IApplicationConstants
    {
        public string AppName => "_build";

        public string AppTitle => "_build";

        public string AppDescription => "Application builder";

        public string Version => "0.0.0";

        public string AppTag { get; } = appTag;

        public string BuildPayload { get; } = buildPayload;
    }

    [Disposable]
    partial class ApplicationRuntime : IDisposable
    {
        public IRunContext RunContext { get; }
        public AppRunContext App { get; }
        public string Env { get; }
        public string BinRuntime { get; }
        public string BuildId { get; }
        public SemVersion ProjectVersion { get; }
        public string Notes { get; }
        public IServiceProvider ServiceProvider { get; }
        public CancellationToken CancellationToken { get; }
        private CancellationTokenSource cancellationTokenSource { get; }

        public ApplicationRuntime(IRunContext runContext, IServiceProvider serviceProvider, CancellationTokenSource cancellationTokenSource)
        {
            RunContext = runContext;
            ServiceProvider = serviceProvider;
            CancellationToken = cancellationTokenSource.Token;
            this.cancellationTokenSource = cancellationTokenSource;

            var (app, env, binRuntime, buildId, projectVersion, notes) = ExpandRunContext(runContext);

            App = app;
            Env = env;
            BinRuntime = binRuntime;
            BuildId = buildId;
            ProjectVersion = projectVersion;
            Notes = notes;
        }

        protected void Dispose(bool disposing)
        {
            if (disposing)
            {
                cancellationTokenSource.Cancel();
            }
        }
    }
}
