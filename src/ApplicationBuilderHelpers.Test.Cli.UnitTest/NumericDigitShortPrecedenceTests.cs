using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Bare numeric tokens take precedence over digit short names through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// <c>-1</c> binds a positional even when a digit short exists, never silently
/// binds the digit flag; in-token forms (<c>-1=value</c>, compact <c>-1x</c>)
/// still reach the digit option.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class NumericDigitShortPrecedenceTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("numflag", "Probes digit flag short against a numeric positional.")]
    public sealed class DigitFlagProbeCommand : Command
    {
        [CommandOption('1', "oneflag", Description = "Digit flag.")]
        public bool OneFlag { get; set; }

        [CommandArgument("num", Description = "Numeric value.", Position = 0)]
        public int Num { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"OneFlag: {OneFlag}");
            Console.WriteLine($"Num: {Num}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("numvalued", "Probes digit valued short against a positional.")]
    public sealed class DigitValuedProbeCommand : Command
    {
        [CommandOption('1', "onedata", Description = "Digit valued option.")]
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

    [Command("numcontrol", "Probes numeric positional without any digit short.")]
    public sealed class NoDigitShortProbeCommand : Command
    {
        [CommandArgument("num", Description = "Numeric value.", Position = 0)]
        public int Num { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Num: {Num}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("numitems", "Probes digit flag short against string positionals.")]
    public sealed class DigitFlagItemsProbeCommand : Command
    {
        [CommandOption('1', "oneflag", Description = "Digit flag.")]
        public bool OneFlag { get; set; }

        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"OneFlag: {OneFlag}");
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("nopos", "Probes digit flag short with no positional declared.")]
    public sealed class DigitFlagNoPositionalProbeCommand : Command
    {
        [CommandOption('1', "oneflag", Description = "Digit flag.")]
        public bool OneFlag { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"OneFlag: {OneFlag}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("clusternum", "Probes two digit flag shorts against a positional.")]
    public sealed class DigitClusterProbeCommand : Command
    {
        [CommandOption('1', "oneflag", Description = "Digit flag one.")]
        public bool OneFlag { get; set; }

        [CommandOption('2', "twoflag", Description = "Digit flag two.")]
        public bool TwoFlag { get; set; }

        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"OneFlag: {OneFlag}");
            Console.WriteLine($"TwoFlag: {TwoFlag}");
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("compactnum", "Probes letter valued short compact form.")]
    public sealed class CompactLetterProbeCommand : Command
    {
        [CommandOption('n', "named", Description = "Named value.")]
        public string? Named { get; set; }

        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Named: {Named ?? "null"}");
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task FlagShortWithNegativeToken_BindsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numflag", "-1"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Num: -1", output);
        Assert.Contains("OneFlag: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ValuedShortWithNegativeToken_BindsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numvalued", "-1"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -1", output);
        Assert.Contains("OneData: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ControlWithoutDigitShort_BindsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numcontrol", "-1"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Num: -1", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task MultiDigitNegativeToken_BindsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numitems", "-10"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -10", output);
        Assert.Contains("OneFlag: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegativeDecimalToken_BindsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numitems", "-1.5"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -1.5", output);
        Assert.Contains("OneFlag: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SeparatorWithDigitShort_BindsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numflag", "--", "-1"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Num: -1", output);
        Assert.Contains("OneFlag: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CompactLetterForm_StillBindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["compactnum", "-n-1"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Named: -1", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegativeTokenWithoutPositional_ErrorsLoudly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["nopos", "-1"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unexpected argument", error);
        Assert.Contains("-1", error);
    }

    [Fact]
    public async Task MultiCharDigitCluster_DoesNotBindFlags()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["clusternum", "-12"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -12", output);
        Assert.Contains("OneFlag: False", output);
        Assert.Contains("TwoFlag: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EqualsForm_StillBindsDigitShort()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numvalued", "-1=v"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("OneData: v", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CompactDigitForm_StillBindsDigitShort()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numvalued", "-1x"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("OneData: x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("numeric-digit-test")
            .SetExecutableTitle("Numeric Digit Test")
            .SetExecutableDescription("Numeric digit-short precedence verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DigitFlagProbeCommand>()
            .AddCommand<DigitValuedProbeCommand>()
            .AddCommand<NoDigitShortProbeCommand>()
            .AddCommand<DigitFlagItemsProbeCommand>()
            .AddCommand<DigitFlagNoPositionalProbeCommand>()
            .AddCommand<DigitClusterProbeCommand>()
            .AddCommand<CompactLetterProbeCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
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
                var exitCode = await CreateBuilder().RunAsync(args);
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
