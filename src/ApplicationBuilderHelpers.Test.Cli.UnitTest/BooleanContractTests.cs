using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Boolean contract tests for the CLI parser.
/// Pins the agreed boolean surface through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// positional booleans bind the 8 spellings (case-insensitive) and reject
/// anything else with the 8-spelling text; bool flags are bare-only so bare
/// <c>--verbose</c> binds true and bare <c>--no-verbose</c> binds false while
/// every <c>=</c>-form and boolean-word space form fails (exit 2) without
/// binding or leaking; the short flag agrees; and <c>--no-</c> negation only
/// ever applies to bool flags.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class BooleanContractTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("contractbool", "Probes positional boolean binding.")]
    public sealed class ContractBoolCommand : Command
    {
        [CommandArgument("flag", Description = "Flag.", Position = 0)]
        public bool Flag { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Flag: {Flag}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("contractflag", "Probes bare-only bool flag handling.")]
    public sealed class ContractFlagCommand : Command
    {
        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption("data", Description = "Data value.")]
        public string? Data { get; set; }

        [CommandArgument("name", Description = "Name value.", Position = 0, Required = false)]
        public string? Name { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Name: {Name ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("Yes")]
    [InlineData("YES")]
    [InlineData("on")]
    [InlineData("On")]
    [InlineData("ON")]
    [InlineData("1")]
    public async Task Positional_TrueSpellings_BindTrue(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractbool", input]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Flag: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("no")]
    [InlineData("No")]
    [InlineData("NO")]
    [InlineData("off")]
    [InlineData("Off")]
    [InlineData("OFF")]
    [InlineData("0")]
    public async Task Positional_FalseSpellings_BindFalse(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractbool", input]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Flag: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("maybe")]
    [InlineData("2")]
    public async Task Positional_OtherWords_Reject(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractbool", input]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains($"Invalid value '{input}' for argument 'flag'", error);
        Assert.Contains("Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
    }

    [Fact]
    public async Task Flag_Bare_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Flag_BareNegation_BindsFalse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", "--no-verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("--verbose=true", "Option '--verbose' does not accept a value 'true'. Use bare '--verbose'")]
    [InlineData("--verbose=off", "Option '--verbose' does not accept a value 'off'. Use bare '--verbose'")]
    [InlineData("--verbose=maybe", "Option '--verbose' does not accept a value 'maybe'. Use bare '--verbose'")]
    [InlineData("--no-verbose=x", "Option '--no-verbose' does not accept a value 'x'. Use bare '--no-verbose' to set the flag to 'false'.")]
    [InlineData("--no-verbose=true", "Option '--no-verbose' does not accept a value 'true'. Use bare '--no-verbose' to set the flag to 'false'.")]
    public async Task Flag_EqualsForm_Rejects(string token, string expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", token]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains(expected, error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("true")]
    public async Task Flag_SpaceWord_Rejects(string word)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", "--verbose", word]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.DoesNotContain("Verbose:", output);
        Assert.Contains($"Option '--verbose' does not accept a value '{word}'. Use bare '--verbose'", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task Flag_SpaceNonBoolWord_BindsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", "--verbose", "Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.Contains("Name: Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Short_Bare_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", "-v"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("-v=true")]
    [InlineData("-v=off")]
    public async Task Short_EqualsForm_Rejects(string token)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", token]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.DoesNotContain("Verbose:", output);
        Assert.Contains("Option '--verbose' does not accept a value", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task Short_SpaceWord_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", "-v", "off"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.DoesNotContain("Verbose:", output);
        Assert.Contains("Option '--verbose' does not accept a value 'off'. Use bare '--verbose'", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task NonBool_NegatedEquals_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["contractflag", "--no-data=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-data' does not accept a value 'x'. Negation applies to boolean flags only; omit '--no-data' or use '--data=<value>'.", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("bool-contract-test")
            .SetExecutableTitle("Bool Contract Test")
            .SetExecutableDescription("Boolean contract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ContractBoolCommand>()
            .AddCommand<ContractFlagCommand>();
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
