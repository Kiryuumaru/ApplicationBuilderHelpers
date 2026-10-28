using System.Net;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process coverage for the documented custom web-host shape: a
/// <see cref="Command{T}"/> over <see cref="WebApplicationBuilder"/> maps endpoints in
/// <c>AddMappings</c> and waits for host shutdown in <c>Run</c>, while the library builds
/// and starts the host. Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point against a real loopback Kestrel endpoint.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class WebHostServeTests
{
    private const string ResponseBody = "Hello World";

    private static readonly TimeSpan StartTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan HttpTimeout = TimeSpan.FromSeconds(10);
    private static readonly TimeSpan ShutdownTimeout = TimeSpan.FromSeconds(10);

    // Four bounded stages (start, HTTP, exit, stopped) plus headroom, so the outer token
    // outlives the per-step diagnostics instead of pre-empting them.
    private static readonly TimeSpan RunBudget =
        StartTimeout + HttpTimeout + ShutdownTimeout + ShutdownTimeout + TimeSpan.FromSeconds(20);

    // Mirrors the docs/advanced.md web-host recipe: the library builds and starts the host;
    // endpoint mapping lives in AddMappings and Run only waits for shutdown.
    [Command("serve", "Serves HTTP traffic.")]
    private sealed class WebHostServeCommand : Command<WebApplicationBuilder>
    {
        private readonly WebHostProbe _probe;

        public WebHostServeCommand(WebHostProbe probe)
        {
            _probe = probe;
        }

        protected override ValueTask<WebApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
        {
            var builder = WebApplication.CreateBuilder();
            builder.WebHost.UseUrls("http://127.0.0.1:0");
            return new ValueTask<WebApplicationBuilder>(builder);
        }

        public override void AddMappings(ApplicationHost applicationHost, IHost host)
        {
            var app = (WebApplication)host;
            app.MapGet("/", () => ResponseBody);
        }

        protected override ValueTask Run(ApplicationHost<WebApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            _probe.Capture(applicationHost);
            return new ValueTask(applicationHost.Host.WaitForShutdownAsync(cancellationToken));
        }
    }

    private sealed class WebHostProbe
    {
        private readonly TaskCompletionSource<Uri> _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource<bool> _stopped = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public Task<Uri> Started => _started.Task;

        public Task<bool> Stopped => _stopped.Task;

        public IHost? Host { get; private set; }

        public void Capture(ApplicationHost<WebApplicationBuilder> applicationHost)
        {
            Host = applicationHost.Host;
            var lifetime = applicationHost.Services.GetRequiredService<IHostApplicationLifetime>();
            lifetime.ApplicationStarted.Register(OnApplicationStarted);
            lifetime.ApplicationStopped.Register(() => _stopped.TrySetResult(true));
        }

        public void RequestStop()
        {
            try
            {
                Host?.Services.GetRequiredService<IHostApplicationLifetime>().StopApplication();
            }
            catch (ObjectDisposedException)
            {
                // The run may already have shut down and disposed the host.
            }
        }

        private void OnApplicationStarted()
        {
            try
            {
                var addresses = Host?.Services.GetRequiredService<IServer>()
                    .Features.Get<IServerAddressesFeature>()?.Addresses;
                var address = addresses?.FirstOrDefault();
                if (address is null)
                {
                    _started.TrySetException(new InvalidOperationException("Kestrel did not report a listening address."));
                    return;
                }

                _started.TrySetResult(new Uri(address));
            }
            catch (Exception exception)
            {
                _started.TrySetException(exception);
            }
        }
    }

    [Fact]
    public async Task ServeCommand_DocumentedWebHostShape_ServesRootAndShutsDownCleanly()
    {
        var probe = new WebHostProbe();
        string[] args = ["serve"];
        using var runCancellation = new CancellationTokenSource(RunBudget);
        Task<int> runTask = CreateBuilder(new WebHostServeCommand(probe)).RunAsync(args, runCancellation.Token);
        try
        {
            Uri baseAddress = await WaitForHostStartAsync(probe, runTask);
            Assert.Equal("127.0.0.1", baseAddress.Host);
            Assert.InRange(baseAddress.Port, 1, 65535);

            using var handler = new SocketsHttpHandler { UseProxy = false };
            using var httpClient = new HttpClient(handler)
            {
                BaseAddress = baseAddress,
                Timeout = HttpTimeout,
            };

            using var response = await httpClient.GetAsync("/");
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            Assert.Equal(ResponseBody, await response.Content.ReadAsStringAsync());

            probe.RequestStop();

            int exitCode = await WaitForExitAsync(runTask);
            Assert.Equal(0, exitCode);
            await WaitForStoppedAsync(probe.Stopped);
        }
        finally
        {
            await CleanUpRunAsync(probe, runTask, runCancellation);
        }
    }

    private static async Task CleanUpRunAsync(
        WebHostProbe probe,
        Task<int> runTask,
        CancellationTokenSource runCancellation)
    {
        try
        {
            runCancellation.Cancel();
        }
        catch
        {
            // Cleanup only: cancellation callbacks must not replace the body's failure.
        }

        if (!runTask.IsCompleted)
        {
            try
            {
                probe.RequestStop();
            }
            catch
            {
                // Cleanup only: the host may already be shutting down or disposed.
            }
        }

        Task finished = await Task.WhenAny(runTask, Task.Delay(ShutdownTimeout));
        if (ReferenceEquals(finished, runTask))
        {
            _ = runTask.Exception;
            return;
        }

        // The run outlived its bound; observe a later fault so it cannot go unobserved.
        _ = runTask.ContinueWith(
            static task => _ = task.Exception,
            CancellationToken.None,
            TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
            TaskScheduler.Default);
    }

    private static async Task<Uri> WaitForHostStartAsync(WebHostProbe probe, Task<int> runTask)
    {
        Task finished = await Task.WhenAny(probe.Started, runTask, Task.Delay(StartTimeout));
        if (ReferenceEquals(finished, runTask))
        {
            Assert.Fail($"RunAsync returned exit code {await runTask} before the host reported ApplicationStarted.");
        }

        Assert.True(
            ReferenceEquals(finished, probe.Started),
            $"Timed out after {StartTimeout.TotalSeconds:0} seconds waiting for the host to start.");
        return await probe.Started;
    }

    private static async Task<int> WaitForExitAsync(Task<int> runTask)
    {
        Task finished = await Task.WhenAny(runTask, Task.Delay(ShutdownTimeout));
        Assert.True(
            ReferenceEquals(finished, runTask),
            $"Timed out after {ShutdownTimeout.TotalSeconds:0} seconds waiting for RunAsync to complete after shutdown.");
        return await runTask;
    }

    private static async Task WaitForStoppedAsync(Task stopped)
    {
        Task finished = await Task.WhenAny(stopped, Task.Delay(ShutdownTimeout));
        Assert.True(
            ReferenceEquals(finished, stopped),
            $"Timed out after {ShutdownTimeout.TotalSeconds:0} seconds waiting for ApplicationStopped to fire.");
        await stopped;
    }

    private static ApplicationBuilder CreateBuilder(ICommand command)
        => ApplicationBuilder.Create()
            .SetExecutableName("web-host-test")
            .SetExecutableTitle("Web Host Test")
            .SetExecutableDescription("Web host recipe verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ApplicationBuilder>(command);
}
