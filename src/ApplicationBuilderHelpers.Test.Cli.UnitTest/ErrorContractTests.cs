using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process CLI error contract tests.
/// Pins the exit-code contract through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// unexpected faults map to exit 1 with styled stderr, custom command exit codes
/// pass through untouched, and help stays exit 0 on stdout only.
/// Also pins <see cref="CommandErrorKind.UnknownCommand"/> footer parity across
/// the gateway, run-fault, and host paths: all three render the global footer
/// when no command name is supplied (the host path resolves the executable
/// name by auto-detection, so only the kind-specific suffix is pinned there).
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ErrorContractTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("contractfault", "Probes unexpected fault handling.")]
    public sealed class FaultContractCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            throw new InvalidOperationException("contract fault");
        }
    }

    [Command("contractcustom", "Probes custom exit-code passthrough.")]
    public sealed class CustomExitContractCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            throw new CommandException(42);
        }
    }

    [Command("contractrun", "Probes run-fault kind preservation.")]
    public sealed class RunFaultKindContractCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            throw new CommandException("no such command", 2, CommandErrorKind.UnknownCommand);
        }
    }

    [Command("contractslow", "Probes host-fault rendering while running.")]
    public sealed class SlowSuccessContractCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            await Task.Delay(100, cancellationTokenSource.Token);
        }
    }

    public sealed class UnknownCommandHostBoomService : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            throw new CommandException("no such command", 2, CommandErrorKind.UnknownCommand);
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }

    public sealed class UnknownCommandHostBoomDependency : ApplicationDependency
    {
        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            services.AddHostedService<UnknownCommandHostBoomService>();
        }
    }

    [Fact]
    public async Task UnexpectedFault_MapsToFaultExitCodeWithStyledError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<FaultContractCommand>(), ["contractfault"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: contract fault", error);
        Assert.Contains("Run 'contract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task CustomCommandExitCode_PassesThrough()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<CustomExitContractCommand>(), ["contractcustom"]);

        Assert.Equal(42, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error:", error);
        Assert.Contains("Run 'contract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task UnknownCommandGateway_RendersGlobalFooter()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<FaultContractCommand>(), ["boguscmd"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Run 'contract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task UnknownCommandRunFault_RendersGlobalFooter()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<RunFaultKindContractCommand>(), ["contractrun"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: no such command", error);
        Assert.Contains("Run 'contract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task UnknownCommandHostFault_RendersGlobalFooter()
    {
        var (exitCode, _, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SlowSuccessContractCommand>().AddApplication<UnknownCommandHostBoomDependency>(), ["contractslow"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Error: no such command", error);
        Assert.Contains("Run 'testhost --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task GlobalHelp_WritesToStdoutOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<FaultContractCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("contract-test")
            .SetExecutableTitle("Contract Test")
            .SetExecutableDescription("Error contract verification CLI.")
            .SetExecutableVersion("9.9.9");
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
