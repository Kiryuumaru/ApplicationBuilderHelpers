using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process tokenizer truth-table tests for the CLI parser.
/// Pins the frozen surface through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// bare flags, <c>=</c>-form boolean literals, no space-consumption for flags,
/// the <c>--</c> separator, negative positionals, combined short flags,
/// <c>--no-</c> negation, and bare-flag repetition.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class TokenizerTruthTableTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("tok", "Probes tokenizer truth table.")]
    public sealed class TokenizerProbeCommand : Command
    {
        [CommandOption('a', "alpha", Description = "Alpha flag.")]
        public bool Alpha { get; set; }

        [CommandOption('b', "beta", Description = "Beta flag.")]
        public bool Beta { get; set; }

        [CommandOption('c', "charlie", Description = "Charlie flag.")]
        public bool Charlie { get; set; }

        [CommandOption('d', "data", Description = "Data value.")]
        public string? Data { get; set; }

        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Alpha: {Alpha}");
            Console.WriteLine($"Beta: {Beta}");
            Console.WriteLine($"Charlie: {Charlie}");
            Console.WriteLine($"Data: {Data ?? "null"}");
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task BareFlag_SetsVerboseTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("YES")]
    [InlineData("on")]
    [InlineData("ON")]
    [InlineData("1")]
    public async Task EqualsForm_TrueLiterals_SetVerboseTrue(string literal)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", $"--verbose={literal}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData("no")]
    [InlineData("NO")]
    [InlineData("off")]
    [InlineData("OFF")]
    [InlineData("0")]
    public async Task EqualsForm_FalseLiterals_SetVerboseFalse(string literal)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", $"--verbose={literal}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EqualsForm_InvalidLiteral_ReportsValueError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--verbose=maybe"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task SpaceSeparatedBooleanWord_StaysPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--verbose", "off"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.Contains("Items: off", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Separator_DashValue_IsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--", "-5"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -5", output);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Separator_DashPath_IsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--", "-report.txt"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -report.txt", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Separator_OptionLikeToken_IsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.Contains("Items: --verbose", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Separator_HelpToken_IsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: --help", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Separator_EmptyTail_Succeeds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.Contains("Items: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegativeInteger_IsPositionalWithoutSeparator()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "-5"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -5", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegativeDouble_IsPositionalWithoutSeparator()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "-1.5"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: -1.5", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CombinedShortFlags_SetAllFlags()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "-abc"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: True", output);
        Assert.Contains("Beta: True", output);
        Assert.Contains("Charlie: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CombinedShortFlags_LastOptionTakesAttachedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "-abdvalue"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: True", output);
        Assert.Contains("Beta: True", output);
        Assert.Contains("Charlie: False", output);
        Assert.Contains("Data: value", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegatedFlag_SetsVerboseFalse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--no-verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegatedFlag_WithValue_IsRejected()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--no-verbose=true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("--no-verbose", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task NegatedFlag_UnknownName_IsRejected()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--no-frobnicate"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option", error);
        Assert.Contains("--no-frobnicate", error);
    }

    [Fact]
    public async Task RepeatedBareFlag_IsIdempotent()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["tok", "--verbose", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("tokenizer-test")
            .SetExecutableTitle("Tokenizer Test")
            .SetExecutableDescription("Tokenizer truth-table verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<TokenizerProbeCommand>();
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
