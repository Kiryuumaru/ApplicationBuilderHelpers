using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Covers command-last dependency ordering across the shared setup pipeline:
/// steps 2-7 run each shared module before the command's own entry, and the
/// command's same-key writes (configuration source, <c>BuilderPreparation</c>)
/// win because the command runs last.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CommandDependencyOrderingTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private static readonly List<string> InterleavingLog = [];

    public sealed class StepRecordingModule : ApplicationDependency
    {
        public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
        {
            InterleavingLog.Add("module:BuilderPreparation");
        }

        public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
        {
            InterleavingLog.Add("module:AddConfigurations");
        }

        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            InterleavingLog.Add("module:AddServices");
        }

        public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
        {
            InterleavingLog.Add("module:AddMiddlewares");
        }

        public override void AddMappings(ApplicationHost applicationHost, IHost host)
        {
            InterleavingLog.Add("module:AddMappings");
        }

        public override void RunPreparation(ApplicationHost applicationHost)
        {
            InterleavingLog.Add("module:RunPreparation");
        }
    }

    [Command("orderprobe", "Records the per-step module and command interleaving.")]
    public sealed class StepRecordingCommand : Command
    {
        public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
        {
            InterleavingLog.Add("command:BuilderPreparation");
        }

        public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
        {
            InterleavingLog.Add("command:AddConfigurations");
        }

        public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
        {
            InterleavingLog.Add("command:AddServices");
        }

        public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
        {
            InterleavingLog.Add("command:AddMiddlewares");
        }

        public override void AddMappings(ApplicationHost applicationHost, IHost host)
        {
            InterleavingLog.Add("command:AddMappings");
        }

        public override void RunPreparation(ApplicationHost applicationHost)
        {
            InterleavingLog.Add("command:RunPreparation");
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("order probe done");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ConfigSourceModule : ApplicationDependency
    {
        public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
        {
            ((IConfigurationManager)configuration).AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CommandConfigProbe"] = "module"
            });
        }
    }

    [Command("configprobe", "Reads the key both the command and a module contribute a configuration source for.")]
    public sealed class ConfigSourceCommand : Command
    {
        public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
        {
            ((IConfigurationManager)configuration).AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["CommandConfigProbe"] = "command"
            });
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"config:{applicationHost.Builder.Configuration["CommandConfigProbe"]}");
            return ValueTask.CompletedTask;
        }
    }

    public sealed class PreparationWriteModule : ApplicationDependency
    {
        public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
        {
            applicationBuilder.Builder.Configuration["BuilderPreparationProbe"] = "module";
        }
    }

    [Command("prepprobe", "Reads the key both the command and a module write during BuilderPreparation.")]
    public sealed class PreparationWriteCommand : Command
    {
        public override void BuilderPreparation(ApplicationHostBuilder applicationBuilder)
        {
            applicationBuilder.Builder.Configuration["BuilderPreparationProbe"] = "command";
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"prep:{applicationHost.Builder.Configuration["BuilderPreparationProbe"]}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task PreparationStepsTwoThroughSeven_InterleaveModuleBeforeCommand()
    {
        InterleavingLog.Clear();

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<StepRecordingCommand>().AddApplication<StepRecordingModule>(), ["orderprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("order probe done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        string[] expected =
        [
            "module:BuilderPreparation",
            "command:BuilderPreparation",
            "module:AddConfigurations",
            "command:AddConfigurations",
            "module:AddServices",
            "command:AddServices",
            "module:AddMiddlewares",
            "command:AddMiddlewares",
            "module:AddMappings",
            "command:AddMappings",
            "module:RunPreparation",
            "command:RunPreparation"
        ];
        Assert.Equal(expected, InterleavingLog);
    }

    [Fact]
    public async Task CommandConfigurationSource_WinsSameKey()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<ConfigSourceCommand>().AddApplication<ConfigSourceModule>(), ["configprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("config:command", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task BuilderPreparation_CommandWriteWinsSameKey()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder<PreparationWriteCommand>().AddApplication<PreparationWriteModule>(), ["prepprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("prep:command", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder<TCommand>()
        where TCommand : ICommand
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("order-test")
            .SetExecutableTitle("Order Test")
            .SetExecutableDescription("Command dependency ordering verification CLI.")
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
