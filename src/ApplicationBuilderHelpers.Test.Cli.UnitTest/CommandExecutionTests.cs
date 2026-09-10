using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process command execution tests for the CLI command executor.
/// Exercises execution through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// synchronous and deferred cancellation, joint command/host runs, exception to
/// exit-code mapping, cancellation propagation, builder dependency propagation,
/// and application lifetime callbacks.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// Note: the two <c>Console.CancelKeyPress</c> subscription failure branches
/// (<c>PlatformNotSupportedException</c> / <c>IOException</c>) are platform-specific
/// and unreachable in-process on Linux, so they are intentionally not covered here.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CommandExecutionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("execsync", "Probes synchronous command completion.")]
    public sealed class SyncCompleteCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("sync done");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("execdeferred", "Probes deferred command completion through the joint run.")]
    public sealed class DeferredCompleteCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            await Task.Delay(100);
            Console.WriteLine("deferred done");
            cancellationTokenSource.Cancel();
        }
    }

    [Command("execwait", "Waits until the execution token is cancelled.")]
    public sealed class WaitForCancellationCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("waiting");
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationTokenSource.Token);
            Console.WriteLine("resumed");
        }
    }

    [Command("execsyncboom", "Throws a command exception synchronously.")]
    public sealed class SyncCommandExceptionCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            throw new CommandException("sync boom", 5);
        }
    }

    [Command("execasyncboom", "Fails the run task with a command exception.")]
    public sealed class AsyncCommandExceptionCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.FromException(new CommandException("async boom", 7));
        }
    }

    [Command("execsyncinvalid", "Throws a generic exception synchronously.")]
    public sealed class SyncInvalidCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            throw new InvalidOperationException("sync invalid");
        }
    }

    [Command("execasyncinvalid", "Fails the run task with a generic exception.")]
    public sealed class AsyncInvalidCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.FromException(new InvalidOperationException("async invalid"));
        }
    }

    [Command("execocebare", "Fails with cancellation that nobody requested.")]
    public sealed class BareOperationCancelledCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            return ValueTask.FromException(new OperationCanceledException("bare cancel"));
        }
    }

    [Command("execoceaftercancel", "Cancels first and then reports cancellation.")]
    public sealed class CancelThenOperationCancelledCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            await Task.Delay(20);
            Console.WriteLine("cancelled run");
            cancellationTokenSource.Cancel();
            throw new OperationCanceledException("cancel after cancel");
        }
    }

    [Command("execguarded", "Refuses to build when the token is already cancelled.")]
    public sealed class GuardedBuilderCommand : Command
    {
        protected override ValueTask<HostApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
        {
            stoppingToken.ThrowIfCancellationRequested();
            return new ValueTask<HostApplicationBuilder>(Host.CreateApplicationBuilder());
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("guarded ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("execlifetime", "Registers lifetime callbacks before finishing deferred.")]
    public sealed class DeferredLifetimeCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            using var scope = applicationHost.Services.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<LifetimeService>();
            lifetime.ApplicationExitingCallback(() => Console.WriteLine("lifetime exiting"));
            lifetime.ApplicationExitedCallback(() => Console.WriteLine("lifetime exited"));
            await Task.Delay(100);
            Console.WriteLine("lifetime done");
            cancellationTokenSource.Cancel();
        }
    }

    [Command("execsynclifetime", "Registers lifetime callbacks before finishing synchronously.")]
    public sealed class SyncLifetimeCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            using var scope = applicationHost.Services.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<LifetimeService>();
            lifetime.ApplicationExitingCallback(() => Console.WriteLine("lifetime exiting"));
            lifetime.ApplicationExitedCallback(() => Console.WriteLine("lifetime exited"));
            Console.WriteLine("sync lifetime done");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    public sealed class MarkerDependency : ApplicationDependency
    {
        public override void RunPreparation(ApplicationHost applicationHost)
        {
            Console.WriteLine("marker prepared");
        }
    }

    public sealed class BoomHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new CommandException("host boom", 3);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class BoomDependency : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddHostedService<BoomHostedService>();
        }
    }

    public sealed class InvalidHostedService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("host invalid");
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class InvalidHostDependency : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddHostedService<InvalidHostedService>();
        }
    }

    public sealed class InvalidPreparationDependency : ApplicationDependency
    {
        public override ValueTask RunPreparationAsync(ApplicationHost applicationHost, CancellationToken cancellationToken)
        {
            throw new InvalidOperationException("preparation invalid");
        }
    }

    [Fact]
    public async Task SynchronousCompletion_ExitsZeroAndWritesOutput()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<SyncCompleteCommand>(), ["execsync"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("sync done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DeferredCompletion_RunsJointHostAndExitsZero()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<DeferredCompleteCommand>(), ["execdeferred"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("deferred done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AlreadyCancelledToken_ExitsZeroWithoutFailure()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<SyncCompleteCommand>(), ["execsync"], cts.Token);

        Assert.Equal(0, exitCode);
        Assert.Contains("sync done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SynchronousCommandException_MapsToExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<SyncCommandExceptionCommand>(), ["execsyncboom"]);

        Assert.Equal(5, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("sync boom", error);
    }

    [Fact]
    public async Task AsynchronousCommandException_MapsToExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<AsyncCommandExceptionCommand>(), ["execasyncboom"]);

        Assert.Equal(7, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("async boom", error);
    }

    [Fact]
    public async Task SynchronousGenericException_PropagatesToCaller()
    {
        var (exception, output, error) = await RunCapturedThrowsAsync<InvalidOperationException>(
            () => CreateBuilder<SyncInvalidCommand>(), ["execsyncinvalid"]);

        Assert.Contains("sync invalid", exception.Message);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AsynchronousGenericException_PropagatesToCaller()
    {
        var (exception, output, error) = await RunCapturedThrowsAsync<InvalidOperationException>(
            () => CreateBuilder<AsyncInvalidCommand>(), ["execasyncinvalid"]);

        Assert.Contains("async invalid", exception.Message);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task HostNonZeroExit_MapsToCommandExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<DeferredCompleteCommand>().AddApplication<BoomDependency>(), ["execdeferred"]);

        Assert.Equal(3, exitCode);
        Assert.Contains("exited with code 3", error);
        Assert.Contains("execdeferred", error);
    }

    [Fact]
    public async Task HostGenericException_PropagatesToCaller()
    {
        var (exception, _, _) = await RunCapturedThrowsAsync<InvalidOperationException>(
            () => CreateBuilder<DeferredCompleteCommand>().AddApplication<InvalidHostDependency>(), ["execdeferred"]);

        Assert.Contains("host invalid", exception.Message);
    }

    [Fact]
    public async Task HostPreparationException_PropagatesToCaller()
    {
        var (exception, _, _) = await RunCapturedThrowsAsync<InvalidOperationException>(
            () => CreateBuilder<DeferredCompleteCommand>().AddApplication<InvalidPreparationDependency>(), ["execdeferred"]);

        Assert.Contains("preparation invalid", exception.Message);
    }

    [Fact]
    public async Task UnrequestedCancellation_RethrowsToCaller()
    {
        var (exception, output, _) = await RunCapturedThrowsAsync<OperationCanceledException>(
            () => CreateBuilder<BareOperationCancelledCommand>(), ["execocebare"]);

        Assert.Contains("bare cancel", exception.Message);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
    }

    [Fact]
    public async Task CancellationAfterRequest_ExitsZero()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<CancelThenOperationCancelledCommand>(), ["execoceaftercancel"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("cancelled run", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ExternalCancellation_ExitsZero()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(500);

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<WaitForCancellationCommand>(), ["execwait"], cts.Token);

        Assert.Equal(0, exitCode);
        Assert.Contains("waiting", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task GuardedBuilderWithCancelledToken_PropagatesCancellation()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var (exception, _, _) = await RunCapturedThrowsAsync<OperationCanceledException>(
            () => CreateBuilder<GuardedBuilderCommand>(), ["execguarded"], cts.Token);

        Assert.IsType<OperationCanceledException>(exception);
    }

    [Fact]
    public async Task BuilderDependencies_ArePropagatedToHostRun()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<DeferredCompleteCommand>().AddApplication<MarkerDependency>(), ["execdeferred"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("marker prepared", output);
        Assert.Contains("deferred done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LifetimeCallbacks_InvokedOnJointRun()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<DeferredLifetimeCommand>(), ["execlifetime"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("lifetime done", output);
        Assert.Contains("lifetime exiting", output);
        Assert.Contains("lifetime exited", output);
        Assert.True(
            output.IndexOf("lifetime exiting", StringComparison.Ordinal) < output.IndexOf("lifetime exited", StringComparison.Ordinal),
            $"Expected exiting callback before exited callback but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LifetimeCallbacks_InvokedOnEarlyReturn()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<SyncLifetimeCommand>(), ["execsynclifetime"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("sync lifetime done", output);
        Assert.Contains("lifetime exiting", output);
        Assert.Contains("lifetime exited", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("exec-test")
            .SetExecutableTitle("Exec Test")
            .SetExecutableDescription("Command execution verification CLI.")
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
            var exception = await Assert.ThrowsAnyAsync<TException>(
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
