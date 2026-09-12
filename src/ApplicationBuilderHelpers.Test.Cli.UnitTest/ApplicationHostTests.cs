using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.Metrics;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process application host tests.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point to cover the host run pipeline: generic host accessors, preparation
/// hook ordering, sync/async preparation markers and failures, hosted-service
/// exit codes, cancellation, and host-builder construction errors.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// Note: ApplicationHostBuilder&lt;T&gt;.AddApplication overloads mutate the
/// dependency list and therefore cannot be invoked from inside the build hooks
/// (which enumerate that list) without throwing; they are documented as an
/// accepted gap.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ApplicationHostTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private const int CanceledExitCode = 130;

    [Command("hostprobe", "Probes host accessor properties.")]
    public sealed class HostProbeCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine(applicationHost.Builder is not null ? "host builder present" : "host builder missing");
            Console.WriteLine(applicationHost.Services is not null ? "host services present" : "host services missing");
            Console.WriteLine(applicationHost.Host is not null ? "host present" : "host missing");
            await Task.Delay(100);
            Console.WriteLine("probe done");
            cancellationTokenSource.Cancel();
        }
    }

    [Command("hostwait", "Waits until the execution token is cancelled.")]
    public sealed class HostWaitCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("waiting on host");
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);
        }
    }

    public sealed class OrderRecordingDependency : ApplicationDependency
    {
        public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
        {
            var count = applicationBuilder.Count();
            var config = applicationBuilder.Configuration;
            Console.WriteLine($"builder saw {count} deps (config {(config is null ? "missing" : "present")})");
        }

        public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
        {
            Console.WriteLine("middleware marker");
        }

        public override void AddMappings(ApplicationHost applicationHost, IHost host)
        {
            Console.WriteLine("mappings marker");
        }

        public override void RunPreparation(ApplicationHost applicationHost)
        {
            Console.WriteLine("run preparation marker");
        }

        public override ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("async run preparation marker");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class SyncPreparationFailureDependency : ApplicationDependency
    {
        public override void RunPreparation(ApplicationHost applicationHost)
        {
            throw new CommandException("sync preparation failure", 9);
        }
    }

    public sealed class AsyncPreparationFailureDependency : ApplicationDependency
    {
        public override ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
        {
            return ValueTask.FromException(new CommandException("async preparation failure", 11));
        }
    }

    public sealed class FirstMarkerDependency : ApplicationDependency
    {
        public override void RunPreparation(ApplicationHost applicationHost)
        {
            Console.WriteLine("first prepared");
        }
    }

    public sealed class SecondMarkerDependency : ApplicationDependency
    {
        public override void RunPreparation(ApplicationHost applicationHost)
        {
            Console.WriteLine("second prepared");
        }
    }

    public sealed class FirstAsyncMarkerDependency : ApplicationDependency
    {
        public override ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("first async prepared");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class SecondAsyncMarkerDependency : ApplicationDependency
    {
        public override ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("second async prepared");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class HostFailureService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new CommandException("hosted service failure", 4);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class HostFailureDependency : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddHostedService<HostFailureService>();
        }
    }

    public sealed class FakeHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Test";

        public string ApplicationName { get; set; } = "TestApp";

        public string ContentRootPath { get; set; } = AppContext.BaseDirectory;

        public IFileProvider ContentRootFileProvider { get; set; } = null!;
    }

    public sealed class FakeLoggingBuilder(IServiceCollection services) : ILoggingBuilder
    {
        public IServiceCollection Services { get; } = services;
    }

    public sealed class FakeMetricsBuilder(IServiceCollection services) : IMetricsBuilder
    {
        public IServiceCollection Services { get; } = services;
    }

    public sealed class BuilderWithoutBuildMethod : IHostApplicationBuilder
    {
        public IConfigurationManager Configuration { get; } = new ConfigurationManager();

        public IHostEnvironment Environment { get; } = new FakeHostEnvironment();

        public ILoggingBuilder Logging { get; }

        public IMetricsBuilder Metrics { get; }

        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        public IServiceCollection Services { get; } = new ServiceCollection();

        public BuilderWithoutBuildMethod()
        {
            Logging = new FakeLoggingBuilder(Services);
            Metrics = new FakeMetricsBuilder(Services);
        }

        public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
            where TContainerBuilder : notnull
        {
        }
    }

    public sealed class BuilderWithNonHostResult : IHostApplicationBuilder
    {
        public IConfigurationManager Configuration { get; } = new ConfigurationManager();

        public IHostEnvironment Environment { get; } = new FakeHostEnvironment();

        public ILoggingBuilder Logging { get; }

        public IMetricsBuilder Metrics { get; }

        public IDictionary<object, object> Properties { get; } = new Dictionary<object, object>();

        public IServiceCollection Services { get; } = new ServiceCollection();

        public BuilderWithNonHostResult()
        {
            Logging = new FakeLoggingBuilder(Services);
            Metrics = new FakeMetricsBuilder(Services);
        }

        public void ConfigureContainer<TContainerBuilder>(IServiceProviderFactory<TContainerBuilder> factory, Action<TContainerBuilder>? configure = null)
            where TContainerBuilder : notnull
        {
        }

        public string Build() => "not-a-host";
    }

    [Command("nobuild", "Uses a builder without a build method.")]
    public sealed class NoBuildMethodCommand : Command<BuilderWithoutBuildMethod>
    {
        protected override ValueTask<BuilderWithoutBuildMethod> ApplicationBuilder(CancellationToken stoppingToken)
        {
            return new ValueTask<BuilderWithoutBuildMethod>(new BuilderWithoutBuildMethod());
        }

        protected override ValueTask Run(ApplicationHost<BuilderWithoutBuildMethod> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("nonhost", "Uses a builder whose build method returns a non-host.")]
    public sealed class NonHostResultCommand : Command<BuilderWithNonHostResult>
    {
        protected override ValueTask<BuilderWithNonHostResult> ApplicationBuilder(CancellationToken stoppingToken)
        {
            return new ValueTask<BuilderWithNonHostResult>(new BuilderWithNonHostResult());
        }

        protected override ValueTask Run(ApplicationHost<BuilderWithNonHostResult> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task GenericHostAccessors_ExposeBuilderServicesAndHost()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<HostProbeCommand>(), ["hostprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("host builder present", output);
        Assert.Contains("host services present", output);
        Assert.Contains("host present", output);
        Assert.Contains("probe done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task PreparationPipeline_ExecutesInDocumentedOrder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<HostProbeCommand>().AddApplication<OrderRecordingDependency>(), ["hostprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("builder saw 2 deps (config present)", output);
        Assert.Contains("middleware marker", output);
        Assert.Contains("mappings marker", output);
        Assert.Contains("run preparation marker", output);
        Assert.Contains("async run preparation marker", output);
        Assert.Contains("probe done", output);
        Assert.True(
            output.IndexOf("builder saw", StringComparison.Ordinal) < output.IndexOf("middleware marker", StringComparison.Ordinal),
            $"Expected builder preparation before middlewares but got: {output}");
        Assert.True(
            output.IndexOf("middleware marker", StringComparison.Ordinal) < output.IndexOf("mappings marker", StringComparison.Ordinal),
            $"Expected middlewares before mappings but got: {output}");
        Assert.True(
            output.IndexOf("mappings marker", StringComparison.Ordinal) < output.IndexOf("run preparation marker", StringComparison.Ordinal),
            $"Expected mappings before run preparation but got: {output}");
        Assert.True(
            output.IndexOf("run preparation marker", StringComparison.Ordinal) < output.IndexOf("async run preparation marker", StringComparison.Ordinal),
            $"Expected sync preparation before async preparation but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SyncPreparationFailure_MapsToExitCode()
    {
        var (exitCode, _, error) = await RunCapturedAsync(
            () => CreateBuilder<HostProbeCommand>().AddApplication<SyncPreparationFailureDependency>(), ["hostprobe"]);

        Assert.Equal(9, exitCode);
        Assert.Contains("sync preparation failure", error);
    }

    [Fact]
    public async Task AsyncPreparationFailure_MapsToExitCode()
    {
        var (exitCode, _, error) = await RunCapturedAsync(
            () => CreateBuilder<HostProbeCommand>().AddApplication<AsyncPreparationFailureDependency>(), ["hostprobe"]);

        Assert.Equal(11, exitCode);
        Assert.Contains("async preparation failure", error);
    }

    [Fact]
    public async Task MultipleSyncPreparations_ExecuteInRegistrationOrder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<HostProbeCommand>()
                .AddApplication<FirstMarkerDependency>()
                .AddApplication<SecondMarkerDependency>(), ["hostprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("first prepared", output);
        Assert.Contains("second prepared", output);
        Assert.Contains("probe done", output);
        Assert.True(
            output.IndexOf("first prepared", StringComparison.Ordinal) < output.IndexOf("second prepared", StringComparison.Ordinal),
            $"Expected registration order but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task MultipleAsyncPreparations_AllComplete()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<HostProbeCommand>()
                .AddApplication<FirstAsyncMarkerDependency>()
                .AddApplication<SecondAsyncMarkerDependency>(), ["hostprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("first async prepared", output);
        Assert.Contains("second async prepared", output);
        Assert.Contains("probe done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task HostedServiceFailure_MapsToExitCode()
    {
        var (exitCode, _, error) = await RunCapturedAsync(
            () => CreateBuilder<HostProbeCommand>().AddApplication<HostFailureDependency>(), ["hostprobe"]);

        Assert.Equal(4, exitCode);
        Assert.Contains("hosted service failure", error);
        Assert.Contains("exited with code 4", error);
        Assert.Contains("hostprobe", error);
    }

    [Fact]
    public async Task DeferredRun_WithExternalCancellation_MapsToCanceledExitCode()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(500);

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<HostWaitCommand>(), ["hostwait"], cts.Token);

        Assert.Equal(CanceledExitCode, exitCode);
        Assert.Contains("waiting on host", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task BuilderWithoutBuildMethod_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<NoBuildMethodCommand>(), ["nobuild"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: Builder does not have a build method.", error);
        Assert.Contains("Run 'host-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task BuilderWithNonHostResult_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<NonHostResultCommand>(), ["nonhost"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: App does not support type", error);
        Assert.Contains("System.String", error);
        Assert.Contains("Run 'host-test --help' for more information on available commands and options.", error);
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("host-test")
            .SetExecutableTitle("Host Test")
            .SetExecutableDescription("Application host verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<TCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args, CancellationToken cancellationToken = default)
    {
        await ConsoleGate.WaitAsync();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await builderFactory().RunAsync(args, cancellationToken);
            outWriter.Flush();
            errorWriter.Flush();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleGate.Release();
        }
    }
}
