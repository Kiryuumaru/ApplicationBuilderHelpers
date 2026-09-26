using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process duplicate-short tests for the CLI hierarchy gate.
/// Each command's effective scope (own options plus inherited globals) must
/// not hold two options with the same short under different canonical keys
/// (long name, then short, then property name): the parser would otherwise
/// first-win silently. Same-key copies (one logical option under several
/// copy identities) stay legal. Joins the non-parallel
/// <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class DuplicateShortNameTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("dupcol alpha", "Probes distinct options sharing a short on one leaf.")]
    public sealed class SameShortAlphaCommand : Command
    {
        [CommandOption('l', "level", Description = "Level value.")]
        public string Level { get; set; } = "information";

        [CommandOption('l', "local", Description = "Local only.")]
        public bool LocalOnly { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"LocalOnly: {LocalOnly}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dupcol beta", "Clean sibling keeping the shared-short pair local.")]
    public sealed class SameShortBetaCommand : Command
    {
        [CommandOption('q', "quiet", Description = "Suppress output except errors.")]
        public bool Quiet { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Quiet: {Quiet}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dupok alpha", "First leaf holding the shared level option.")]
    public sealed class SharedLevelAlphaCommand : Command
    {
        [CommandOption('l', "level", Description = "Level value.")]
        public string Level { get; set; } = "information";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dupok beta", "Second leaf holding the identical level option.")]
    public sealed class SharedLevelBetaCommand : Command
    {
        [CommandOption('l', "level", Description = "Level value.")]
        public string Level { get; set; } = "information";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dupin alpha", "Probes a local short colliding with an inherited global.")]
    public sealed class InheritedCollisionAlphaCommand : Command
    {
        [CommandOption('l', "log-level", Description = "Logging level.")]
        public string LogLevel { get; set; } = "information";

        [CommandOption('l', "local", Description = "Local only.")]
        public bool LocalOnly { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"LogLevel: {LogLevel}");
            Console.WriteLine($"LocalOnly: {LocalOnly}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dupin beta", "Sibling sharing the log-level option for global promotion.")]
    public sealed class InheritedSiblingBetaCommand : Command
    {
        [CommandOption('l', "log-level", Description = "Logging level.")]
        public string LogLevel { get; set; } = "information";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"LogLevel: {LogLevel}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("duplong", "Probes long-only binding next to a short option.")]
    public sealed class LongOnlyProbeCommand : Command
    {
        [CommandOption('l', "level", Description = "Level value.")]
        public string Level { get; set; } = "information";

        [CommandOption("local", Description = "Local value.")]
        public string Local { get; set; } = "default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"Local: {Local}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task DistinctKeys_SharingShortOnOneLeaf_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SameShortAlphaCommand>().AddCommand<SameShortBetaCommand>(),
            ["dupcol", "alpha"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate short name conflict", error);
        Assert.Contains("'-l'", error);
        Assert.Contains("-l, --level", error);
        Assert.Contains("-l, --local", error);
        Assert.Contains("dupcol alpha", error);
    }

    [Fact]
    public async Task SameKey_GlobalCopies_BindWithoutFault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SharedLevelAlphaCommand>().AddCommand<SharedLevelBetaCommand>(),
            ["dupok", "alpha", "--level", "debug"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Level: debug", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task InheritedGlobal_LeafCollision_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<InheritedCollisionAlphaCommand>().AddCommand<InheritedSiblingBetaCommand>(),
            ["dupin", "alpha"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate short name conflict", error);
        Assert.Contains("'-l'", error);
        Assert.Contains("-l, --log-level", error);
        Assert.Contains("-l, --local", error);
        Assert.Contains("dupin alpha", error);
    }

    [Fact]
    public async Task LongOnlyOption_BesideShort_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<LongOnlyProbeCommand>(),
            ["duplong", "--level", "debug", "--local", "central"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Level: debug", output);
        Assert.Contains("Local: central", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("duplicate-short-test")
            .SetExecutableTitle("Duplicate Short Test")
            .SetExecutableDescription("Duplicate short name verification CLI.")
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
