using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process host run preparation tests.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point through the <c>ApplicationHost</c> run preparation path: sync preparation
/// markers, async preparation failures, hosted-service exit codes, sequential runs,
/// and pre-cancelled execution.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HostRunTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("hostrun", "Probes the host run preparation path.")]
    public sealed class HostRunCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            await Task.Delay(100);
            Console.WriteLine("hostrun done");
            cancellationTokenSource.Cancel();
        }
    }

    [Command("hostrunsync", "Probes the host run path synchronously.")]
    public sealed class SyncHostRunCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("sync hostrun done");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    public sealed class HostRunMarker : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddSingleton(new object());
        }

        public override void RunPreparation(ApplicationHost applicationHost)
        {
            Console.WriteLine("hostrun prepared");
        }
    }

    public sealed class HostRunInvalidPreparation : ApplicationDependency
    {
        public override ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("hostrun preparation invalid");
        }
    }

    public sealed class HostRunBoomService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new CommandException("hostrun host boom", 6);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class HostRunBoomDependency : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddHostedService<HostRunBoomService>();
        }
    }

    public sealed class PrecanceledMarker : ApplicationDependency
    {
        public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
        {
            Console.WriteLine("precancel prepared");
        }
    }

    [Fact]
    public async Task PreparedMarker_WithRegisteredServices_WritesMarkerOnStdout()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<HostRunCommand>().AddApplication<HostRunMarker>(), ["hostrun"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("hostrun prepared", output);
        Assert.Contains("hostrun done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AsyncPreparationThrowingInvalidOperation_PropagatesToCaller()
    {
        var (exception, _, _) = await RunCapturedThrowsAsync<InvalidOperationException>(
            () => CreateBuilder<HostRunCommand>().AddApplication<HostRunInvalidPreparation>(), ["hostrun"]);

        Assert.Contains("hostrun preparation invalid", exception.Message);
    }

    [Fact]
    public async Task HostedServiceThrowingCommandException_MapsToExitCode()
    {
        var (exitCode, _, error) = await RunCapturedAsync(
            () => CreateBuilder<HostRunCommand>().AddApplication<HostRunBoomDependency>(), ["hostrun"]);

        Assert.Equal(6, exitCode);
        Assert.Contains("hostrun host boom", error);
        Assert.Contains("exited with code 6", error);
    }

    [Fact]
    public async Task SequentialRuns_BothSucceedWithMarkers()
    {
        var first = await RunCapturedAsync(
            () => CreateBuilder<HostRunCommand>().AddApplication<HostRunMarker>(), ["hostrun"]);
        var second = await RunCapturedAsync(
            () => CreateBuilder<HostRunCommand>().AddApplication<HostRunMarker>(), ["hostrun"]);

        Assert.Equal(0, first.ExitCode);
        Assert.Contains("hostrun prepared", first.Output);
        Assert.Contains("hostrun done", first.Output);
        Assert.Equal(0, second.ExitCode);
        Assert.Contains("hostrun prepared", second.Output);
        Assert.Contains("hostrun done", second.Output);
    }

    [Fact]
    public async Task PrecanceledToken_StillRunsEarlyPreparationMarker()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<SyncHostRunCommand>().AddApplication<PrecanceledMarker>(), ["hostrunsync"], cts.Token);

        Assert.Equal(0, exitCode);
        Assert.Contains("precancel prepared", output);
        Assert.Contains("sync hostrun done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("host-test")
            .SetExecutableTitle("Host Test")
            .SetExecutableDescription("Host run verification CLI.")
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

    private static async Task<(TException Exception, string Output, string Error)> RunCapturedThrowsAsync<TException>(
        Func<ApplicationBuilder> builderFactory, string[] args, CancellationToken cancellationToken = default)
        where TException : Exception
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
            var exception = await Assert.ThrowsAsync<TException>(
                () => builderFactory().RunAsync(args, cancellationToken));
            outWriter.Flush();
            errorWriter.Flush();
            return (exception, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleGate.Release();
        }
    }
}
