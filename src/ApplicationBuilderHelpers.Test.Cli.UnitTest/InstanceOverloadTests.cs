using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process instance-overload tests for the application builder.
/// Exercises the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// instance-based application dependencies, instance-based command type parsers,
/// command preparation forwarding through <see cref="ICommand"/>, and short-only
/// option help rendering.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class InstanceOverloadTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("instanceprobe", "Probes instance dependency registration.")]
    public sealed class InstanceProbeCommand : Command
    {
        protected override async ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            await Task.Delay(100);
            Console.WriteLine("instance done");
            cancellationTokenSource.Cancel();
        }
    }

    public sealed class MarkerDependency : ApplicationDependency
    {
        public override void RunPreparation(ApplicationHost applicationHost)
        {
            Console.WriteLine("marker prepared");
        }
    }

    [Command("timespanprobe", "Probes instance parser registration.")]
    public sealed class TimeSpanProbeCommand : Command
    {
        [CommandOption("delay", Description = "Delay value.")]
        public TimeSpan Delay { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Delay: {Delay}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ProbeTimeSpanParser : CommandTypeParser<TimeSpan>
    {
        public override TimeSpan ParseValue(string? value, out string? validateError)
        {
            if (TimeSpan.TryParse(value, out var result))
            {
                validateError = null;
                return result;
            }

            validateError = $"Invalid TimeSpan value: '{value}'.";
            return default;
        }
    }

    [Command("prepprobe", "Probes command preparation forwarding.")]
    public sealed class PreparationProbeCommand : Command
    {
        public static int PreparationCalls;

        public bool PreparationRan { get; private set; }

        public override void CommandPreparation(ApplicationBuilder applicationBuilder)
        {
            PreparationRan = true;
            Interlocked.Increment(ref PreparationCalls);
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("run done");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("shortprobe", "Probes short-only option help.")]
    public sealed class ShortOnlyProbeCommand : Command
    {
        [CommandOption('x', Description = "Short-only value.")]
        public string Value { get; set; } = string.Empty;

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Value: {Value}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task AddApplicationInstance_RegistersDependencyPreparation()
    {
        var builder = CreateBuilder().AddCommand<InstanceProbeCommand>();
        var result = builder.AddApplication(new MarkerDependency());

        Assert.Same(builder, result);

        var (exitCode, output, error) = await RunCapturedAsync(() => builder, ["instanceprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("marker prepared", output);
        Assert.Contains("instance done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AddCommandTypeParserInstance_RegistersCustomParser()
    {
        var builder = CreateBuilder().AddCommand<TimeSpanProbeCommand>();
        var result = builder.AddCommandTypeParser(new ProbeTimeSpanParser());

        Assert.Same(builder, result);

        var (exitCode, output, error) = await RunCapturedAsync(() => builder, ["timespanprobe", "--delay=00:01:30"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Delay: 00:01:30", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CommandPreparation_ForwardsThroughInterfaceAndRun()
    {
        PreparationProbeCommand.PreparationCalls = 0;

        var direct = new PreparationProbeCommand();
        ((ICommand)direct).CommandPreparation(ApplicationBuilder.Create());

        Assert.True(direct.PreparationRan);
        Assert.Equal(1, PreparationProbeCommand.PreparationCalls);

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<PreparationProbeCommand>(), ["prepprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Equal(2, PreparationProbeCommand.PreparationCalls);
        Assert.Contains("run done", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ShortOnlyOption_RendersShortNameInHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ShortOnlyProbeCommand>(), ["shortprobe", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("-x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("overload-test")
            .SetExecutableTitle("Overload Test")
            .SetExecutableDescription("Instance overload verification CLI.")
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
