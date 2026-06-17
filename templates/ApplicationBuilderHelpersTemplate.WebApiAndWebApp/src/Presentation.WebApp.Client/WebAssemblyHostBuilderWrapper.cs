using Application.Shared.Extensions;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Primitives;
using System.Diagnostics.CodeAnalysis;

namespace Presentation.WebApp.Client;

internal class WebAssemblyHostBuilderWrapper : IHostApplicationBuilder
{
    public WebAssemblyHostBuilderWrapper(WebAssemblyHostBuilder webAssemblyHostBuilder)
    {
        WebAssemblyHostBuilder = webAssemblyHostBuilder;

        webAssemblyHostBuilder.Configuration.AddEnvironmentVariables();
    }

    public WebAssemblyHostBuilder WebAssemblyHostBuilder { get; private set; }

    public IDictionary<object, object> Properties => new Dictionary<object, object>();

    public IConfigurationManager Configuration => new WrappedConfigurationManager(WebAssemblyHostBuilder);

    public IHostEnvironment Environment => new WrappedHostEnvironment(WebAssemblyHostBuilder);

    public ILoggingBuilder Logging => WebAssemblyHostBuilder.Logging;

    public IMetricsBuilder Metrics => new WrappedMetricsBuilder(WebAssemblyHostBuilder.Services);

    public IServiceCollection Services => WebAssemblyHostBuilder.Services;

    public string BaseAddress => WebAssemblyHostBuilder.HostEnvironment.BaseAddress;

    public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
        where TContainerBuilder : notnull
        => WebAssemblyHostBuilder.ConfigureContainer(factory, configure);

    public IHost Build()
    {
        WrappedHost? webAssemblyWrappedHost = null;
        WrappedHostApplicationLifetime wrappedHostApplicationLifetime = new(() => webAssemblyWrappedHost?.StopAsync());
        WebAssemblyHostBuilder.Services.AddSingleton<IHostApplicationLifetime>(wrappedHostApplicationLifetime);
        webAssemblyWrappedHost = new WrappedHost(WebAssemblyHostBuilder.Build(), wrappedHostApplicationLifetime);
        return webAssemblyWrappedHost;
    }

    private sealed class WrappedHostEnvironment(WebAssemblyHostBuilder webAssemblyHostBuilder) : IHostEnvironment
    {
        public IServiceCollection Services { get; } = webAssemblyHostBuilder.Services;
        public string EnvironmentName { get => webAssemblyHostBuilder.HostEnvironment.Environment; set => throw new NotImplementedException(); }
        public string ApplicationName { get => global::Build.ApplicationConstants.Instance.AppName; set => throw new NotImplementedException(); }
        public string ContentRootPath { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
        public IFileProvider ContentRootFileProvider { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }
    }

    private sealed class WrappedMetricsBuilder(IServiceCollection services) : IMetricsBuilder
    {
        public IServiceCollection Services { get; } = services;
    }

    private sealed class WrappedConfigurationManager(WebAssemblyHostBuilder webAssemblyHostBuilder) : IConfigurationManager
    {
        public string? this[string key] { get => webAssemblyHostBuilder.Configuration[key]; set => webAssemblyHostBuilder.Configuration[key] = value; }

        public IServiceCollection Services { get; } = webAssemblyHostBuilder.Services;

        public IDictionary<string, object> Properties => (webAssemblyHostBuilder.Configuration as IConfigurationBuilder).Properties;

        public IList<IConfigurationSource> Sources => (webAssemblyHostBuilder.Configuration as IConfigurationBuilder).Sources;

        public IConfigurationBuilder Add(IConfigurationSource source)
            => webAssemblyHostBuilder.Configuration.Add(source);

        public IConfigurationRoot Build()
            => webAssemblyHostBuilder.Configuration.Build();

        public IEnumerable<IConfigurationSection> GetChildren()
            => (webAssemblyHostBuilder.Configuration as IConfiguration).GetChildren();

        public IChangeToken GetReloadToken()
            => webAssemblyHostBuilder.Configuration.GetReloadToken();

        public IConfigurationSection GetSection(string key)
            => webAssemblyHostBuilder.Configuration.GetSection(key);
    }

    private sealed class WrappedHost(WebAssemblyHost webAssemblyHost, WrappedHostApplicationLifetime wrappedHostApplicationLifetime) : IHost
    {
        public WebAssemblyHost WebAssemblyHost { get; } = webAssemblyHost;

        public IServiceProvider Services => WebAssemblyHost.Services;

        public void Dispose()
            => WebAssemblyHost.DisposeAsync().AsTask().GetAwaiter().GetResult();

        private CancellationTokenSource? cts = null;

        public Task StartAsync(CancellationToken cancellationToken = default)
        {
            cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            Task.Run(async () =>
            {
                await WebAssemblyHost.RunAsync();
            }, cts.Token);

            wrappedHostApplicationLifetime.ApplicationStartedTrigger();

            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken = default)
        {
            wrappedHostApplicationLifetime.ApplicationStoppingTrigger();
            if (cts != null)
                await cts.CancelAsync();
            wrappedHostApplicationLifetime.ApplicationStoppedTrigger();
        }
    }

    private sealed class WrappedHostApplicationLifetime(Action stopCallback) : IHostApplicationLifetime
    {
        public CancellationToken ApplicationStarted => applicationStartedCts.Token;

        public CancellationToken ApplicationStopping => applicationStoppingCts.Token;

        public CancellationToken ApplicationStopped => applicationStoppedCts.Token;

        public void StopApplication()
        {
            stopCallback();
        }

        private readonly CancellationTokenSource applicationStartedCts = new();
        private readonly CancellationTokenSource applicationStoppingCts = new();
        private readonly CancellationTokenSource applicationStoppedCts = new();

        internal void ApplicationStartedTrigger()
            => applicationStartedCts.Cancel();

        internal void ApplicationStoppingTrigger()
            => applicationStoppingCts.Cancel();

        internal void ApplicationStoppedTrigger()
            => applicationStoppedCts.Cancel();
    }
}
