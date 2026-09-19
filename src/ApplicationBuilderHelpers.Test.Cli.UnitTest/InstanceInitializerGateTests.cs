using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process regression tests for the global-option initializer gate with
/// caller-supplied instance registrations.
/// <see cref="ApplicationBuilder.AddCommand(ICommand)"/>-style registrations keep
/// identity across runs, so value binding mutates the shared instance. The
/// promotion gate must compare registration-time initializer defaults, not the
/// live (possibly already-bound) property values, on every run.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class InstanceInitializerGateTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("instshared alpha", "First leaf with initializer-backed shared option.")]
    public sealed class InstanceSharedAlphaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "alpha-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"instshared alpha:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("instshared beta", "Second leaf with initializer-backed shared option.")]
    public sealed class InstanceSharedBetaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "beta-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"instshared beta:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task InstanceRegistrations_SecondRunStillComparesRegistrationDefaults()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InstanceSharedAlphaCommand());
        builder.AddCommand(new InstanceSharedBetaCommand());

        // Baseline: before any binding, registration defaults diverge, so the
        // option must stay local.
        var baseline = await RunCapturedAsync(builder, ["--help"]);

        Assert.Equal(0, baseline.ExitCode);
        Assert.DoesNotContain("--shared", baseline.Output);

        // Bind an explicit value through a real run: this mutates the shared
        // alpha instance to beta's default, so a live-value comparison on the
        // next run would wrongly see identical initializers.
        var mutate = await RunCapturedAsync(builder, ["instshared", "alpha", "--shared=beta-default"]);

        Assert.Equal(0, mutate.ExitCode);
        Assert.Contains("instshared alpha:beta-default", mutate.Output);

        // Registration defaults still diverge (alpha-default vs beta-default),
        // so the option must stay local instead of promoting to global.
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--shared", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("instancegate-test")
            .SetExecutableTitle("InstanceGate Test")
            .SetExecutableDescription("Instance initializer gate verification CLI.")
            .SetExecutableVersion("9.9.9");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(ApplicationBuilder builder, string[] args)
    {
        await ConsoleGate.WaitAsync();
        try
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var outWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            try
            {
                var exitCode = await builder.RunAsync(args);
                outWriter.Flush();
                errorWriter.Flush();
                return (exitCode, outWriter.ToString(), errorWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
        finally
        {
            ConsoleGate.Release();
        }
    }
}
