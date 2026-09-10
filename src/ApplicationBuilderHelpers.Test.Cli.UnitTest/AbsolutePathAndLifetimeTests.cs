using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process coverage for Brunel targets 4-5: the default-registered
/// <c>AbsolutePath</c> type parser (internal, reached through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point
/// via <c>AbsolutePath</c>-typed options/arguments) and
/// <see cref="LifetimeService"/> resolved through the normal public host path
/// (<c>ApplicationHost.Services</c> inside <c>Command.Run</c>), including the
/// <c>Func&lt;Task&gt;</c> callback overloads that drive the
/// <c>LifetimeGlobalService.Invoke*</c> task tails.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AbsolutePathAndLifetimeTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("abspathopt", "Probes AbsolutePath option binding.")]
    public sealed class AbsolutePathOptionCommand : Command
    {
        [CommandOption("path", Description = "Path value.")]
        public AbsolutePath? Path { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Path: {Path}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("abspatharg", "Probes AbsolutePath argument binding.")]
    public sealed class AbsolutePathArgumentCommand : Command
    {
        [CommandArgument("path", Description = "Path value.", Position = 0)]
        public AbsolutePath? Path { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Path: {Path}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("lifetimetokens", "Probes lifetime token creation through host services.")]
    public sealed class LifetimeTokenCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            using var scope = applicationHost.Services.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<LifetimeService>();
            using var linkedSource = lifetime.CreateCancellationTokenSource();
            var token = lifetime.CreateCancellationToken();
            Console.WriteLine($"CtsCanBeCanceled: {linkedSource.Token.CanBeCanceled}");
            Console.WriteLine($"TokenCanBeCanceled: {token.CanBeCanceled}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("lifetimeall", "Registers every lifetime callback kind before finishing deferred.")]
    public sealed class LifetimeAllCallbacksCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            RegisterAllCallbackKinds(applicationHost);
            await Task.Delay(100);
            Console.WriteLine("lifetime done");
            cancellationTokenSource.Cancel();
        }
    }

    [Command("lifetimesyncall", "Registers every lifetime callback kind before finishing synchronously.")]
    public sealed class LifetimeSyncAllCallbacksCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            RegisterAllCallbackKinds(applicationHost);
            Console.WriteLine("sync lifetime done");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    private static void RegisterAllCallbackKinds(ApplicationHost<HostApplicationBuilder> applicationHost)
    {
        using var scope = applicationHost.Services.CreateScope();
        var lifetime = scope.ServiceProvider.GetRequiredService<LifetimeService>();
        lifetime.ApplicationExitingCallback(() => Console.WriteLine("exiting action"));
        lifetime.ApplicationExitingCallback(async () =>
        {
            await Task.Yield();
            Console.WriteLine("exiting task");
        });
        lifetime.ApplicationExitedCallback(() => Console.WriteLine("exited action"));
        lifetime.ApplicationExitedCallback(async () =>
        {
            await Task.Yield();
            Console.WriteLine("exited task");
        });
    }

    [Fact]
    public async Task AbsolutePathOption_ValidPath_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<AbsolutePathOptionCommand>(), ["abspathopt", "--path=/tmp"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Path: /tmp", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AbsolutePathOption_InvalidValue_ReportsValidateError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<AbsolutePathOptionCommand>(), ["abspathopt", "--path="]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid AbsolutePath value: ''.", error);
    }

    [Fact]
    public async Task AbsolutePathArgument_ValidPath_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<AbsolutePathArgumentCommand>(), ["abspatharg", "/tmp"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Path: /tmp", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AbsolutePathArgument_InvalidValue_ReportsValidateError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<AbsolutePathArgumentCommand>(), ["abspatharg", "   "]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid AbsolutePath value: '   '.", error);
    }

    [Fact]
    public async Task AbsolutePathOption_HelpRendersSuccessfully()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<AbsolutePathOptionCommand>(), ["abspathopt", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--path", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LifetimeService_TokenCreation_ViaHostServices()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<LifetimeTokenCommand>(), ["lifetimetokens"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("CtsCanBeCanceled: True", output);
        Assert.Contains("TokenCanBeCanceled: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LifetimeService_AllCallbackKinds_InvokedOnJointRun()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<LifetimeAllCallbacksCommand>(), ["lifetimeall"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("lifetime done", output);
        Assert.Contains("exiting action", output);
        Assert.Contains("exiting task", output);
        Assert.Contains("exited action", output);
        Assert.Contains("exited task", output);
        Assert.True(
            output.IndexOf("lifetime done", StringComparison.Ordinal) < output.IndexOf("exiting action", StringComparison.Ordinal),
            $"Expected run output before exiting callbacks but got: {output}");
        Assert.True(
            output.IndexOf("exiting task", StringComparison.Ordinal) < output.IndexOf("exited action", StringComparison.Ordinal),
            $"Expected exiting callbacks before exited callbacks but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LifetimeService_AllCallbackKinds_InvokedOnEarlyReturn()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<LifetimeSyncAllCallbacksCommand>(), ["lifetimesyncall"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("sync lifetime done", output);
        Assert.Contains("exiting action", output);
        Assert.Contains("exiting task", output);
        Assert.Contains("exited action", output);
        Assert.Contains("exited task", output);
        Assert.True(
            output.IndexOf("exiting task", StringComparison.Ordinal) < output.IndexOf("exited task", StringComparison.Ordinal),
            $"Expected exiting callbacks before exited callbacks but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("abspath-lifetime-test")
            .SetExecutableTitle("AbsPath Lifetime Test")
            .SetExecutableDescription("AbsolutePath and lifetime verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<TCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args)
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
            var exitCode = await builderFactory().RunAsync(args);
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
