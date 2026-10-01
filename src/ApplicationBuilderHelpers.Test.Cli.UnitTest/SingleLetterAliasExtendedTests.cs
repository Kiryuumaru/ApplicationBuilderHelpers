using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Extended single-letter promotion guards: a single-character long name
/// answers its single-dash alias through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point.
/// Pins equals forms, bool binding plus negation, clusters with a promoted
/// short, runtime duplicate faults, digit promotion against numeric
/// precedence, and help listing the promoted short alongside the long.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class SingleLetterAliasExtendedTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("single", "Probes promoted valued single-letter long with an explicit flag sibling.")]
    public sealed class PromotedValuedProbeCommand : Command
    {
        [CommandOption("a", Description = "Alpha value.")]
        public string? Alpha { get; set; }

        [CommandOption('b', "beta", Description = "Beta flag.")]
        public bool Beta { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Alpha: {Alpha ?? "null"}");
            Console.WriteLine($"Beta: {Beta}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("flag", "Probes promoted bool single-letter long and its negation.")]
    public sealed class PromotedFlagProbeCommand : Command
    {
        [CommandOption("a", Description = "Active flag.")]
        public bool Active { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Active: {Active}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("digit", "Probes promoted digit single-letter long against a positional.")]
    public sealed class PromotedDigitProbeCommand : Command
    {
        [CommandOption("1", Description = "Digit valued option.")]
        public string? OneData { get; set; }

        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"OneData: {OneData ?? "null"}");
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("promdup", "Probes promoted single-letter colliding with an explicit short.")]
    public sealed class PromotedDuplicateProbeCommand : Command
    {
        [CommandOption("a", Description = "Alpha value.")]
        public string? Alpha { get; set; }

        [CommandOption('a', "arc", Description = "Arc value.")]
        public string? Arc { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Alpha: {Alpha ?? "null"}");
            Console.WriteLine($"Arc: {Arc ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task EqualsShortForm_BindsPromotedValued()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "-a=v"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: v", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EqualsLongForm_BindsPromotedValued()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "--a=v"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: v", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FlagShortForm_BindsPromotedFlag()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["flag", "-a"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Active: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FlagLongForm_BindsPromotedFlag()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["flag", "--a"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Active: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegatedLongForm_SetsPromotedFlagFalse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["flag", "--no-a"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Active: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Help_ListsNegationForPromotedFlag()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["flag", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--no-a", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ClusterFlagThenValued_BindsNextToken()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "-ba", "value"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Beta: True", output);
        Assert.Contains("Alpha: value", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ClusterCompactRemainder_BindsPromotedValued()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "-abvalue"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: bvalue", output);
        Assert.Contains("Beta: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task PromotedDuplicate_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<PromotedDuplicateProbeCommand>(),
            ["promdup"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate short name conflict", error);
        Assert.Contains("'-a'", error);
        Assert.Contains("-a, --a", error);
        Assert.Contains("-a, --arc", error);
        Assert.Contains("promdup", error);
    }

    [Fact]
    public async Task DigitPromotion_BareToken_BindsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["digit", "-1"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -1", output);
        Assert.Contains("OneData: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DigitPromotion_EqualsForm_BindsDigitOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["digit", "-1=v"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("OneData: v", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DigitPromotion_CompactForm_BindsDigitOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["digit", "-1x"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("OneData: x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Help_ShowsPromotedShortAlongsideLong()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["single", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("-a, --a", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("single-letter-extended-test")
            .SetExecutableTitle("Single Letter Extended Test")
            .SetExecutableDescription("Single letter alias extended verification CLI.")
            .SetExecutableVersion("9.9.9")
            .SetHelpWidth(120)
            .AddCommand<PromotedValuedProbeCommand>()
            .AddCommand<PromotedFlagProbeCommand>()
            .AddCommand<PromotedDigitProbeCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
    {
        return await RunCapturedAsync(CreateBuilder, args);
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
