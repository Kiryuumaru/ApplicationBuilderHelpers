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

    [Command("instident alpha", "First leaf with identical initializer-backed shared option.")]
    public sealed class InstanceIdenticalAlphaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "same-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"instident alpha:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("instident beta", "Second leaf with identical initializer-backed shared option.")]
    public sealed class InstanceIdenticalBetaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "same-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"instident beta:{Shared}");
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

    [Fact]
    public async Task InstanceRegistrations_SecondRunLeafHelpShowsRegistrationDefaults()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InstanceSharedAlphaCommand());
        builder.AddCommand(new InstanceSharedBetaCommand());

        // Bind an explicit value through a real run: this mutates the shared
        // alpha instance to beta's default, so a live-value help read on the
        // next run would report the bound value as the default.
        var mutate = await RunCapturedAsync(builder, ["instshared", "alpha", "--shared=beta-default"]);

        Assert.Equal(0, mutate.ExitCode);
        Assert.Contains("instshared alpha:beta-default", mutate.Output);

        // Registration defaults diverge (alpha-default vs beta-default), so
        // each leaf help must still report its own registration default.
        var (alphaCode, alphaOutput, alphaError) = await RunCapturedAsync(builder, ["instshared", "alpha", "--help"]);

        Assert.Equal(0, alphaCode);
        Assert.Contains("--shared", alphaOutput);
        Assert.Contains("Default: alpha-default", alphaOutput);
        Assert.DoesNotContain("Default: beta-default", alphaOutput);
        Assert.True(string.IsNullOrWhiteSpace(alphaError), $"Expected empty stderr but got: {alphaError}");

        var (betaCode, betaOutput, betaError) = await RunCapturedAsync(builder, ["instshared", "beta", "--help"]);

        Assert.Equal(0, betaCode);
        Assert.Contains("--shared", betaOutput);
        Assert.Contains("Default: beta-default", betaOutput);
        Assert.DoesNotContain("Default: alpha-default", betaOutput);
        Assert.True(string.IsNullOrWhiteSpace(betaError), $"Expected empty stderr but got: {betaError}");
    }

    [Fact]
    public async Task IdenticalInstanceRegistrations_SecondRunRootHelpShowsRegistrationDefault()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InstanceIdenticalAlphaCommand());
        builder.AddCommand(new InstanceIdenticalBetaCommand());

        // Bind an explicit value through a real run: this mutates the shared
        // alpha instance away from the registration default, so a live-value
        // help read on the next run would report the bound value as the
        // promoted global default.
        var mutate = await RunCapturedAsync(builder, ["instident", "alpha", "--shared=mutated"]);

        Assert.Equal(0, mutate.ExitCode);
        Assert.Contains("instident alpha:mutated", mutate.Output);

        // Registration defaults still match (same-default), so the option
        // stays promoted and root help reports the registration default.
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.Contains("--shared", output);
        Assert.Contains("Default: same-default", output);
        Assert.DoesNotContain("Default: mutated", output);
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
