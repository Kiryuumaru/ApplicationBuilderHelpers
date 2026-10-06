using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for a known flag in <c>=</c>-form on the
/// abstract-root path: a CLI that registers only leaf
/// subcommands has no root implementation, so pre-separator tokens stay on
/// the abstract branch of <c>ArgumentParser</c>. A root-visible (globally
/// promoted) flag rejects every <c>--verbose=literal</c> form as
/// <c>InvalidValue</c> (exit 2) with the bare-only text, never as
/// <c>RequiresSubcommand</c>; bare flags followed by a boolean-looking
/// word also reject as <c>InvalidValue</c>, while non-boolean neighbors,
/// valued options, leaf-only bases, and post-separator tokens keep their
/// existing paths.
/// Error kinds are pinned via their distinct help footers.
/// Error kinds are pinned via their distinct help footers.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AbstractRootFlagLiteralTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets.")]
    public sealed class FlagLiteralGreetCommand : Command
    {
        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption('a', "alpha", Description = "Alpha flag.")]
        public bool Alpha { get; set; }

        [CommandOption('b', "beta", Description = "Beta flag.")]
        public bool Beta { get; set; }

        [CommandOption("secure", Description = "Secure flag.", Secret = true)]
        public bool Secure { get; set; }

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

    [Command("farewell", "Farewells.")]
    public sealed class FlagLiteralFarewellCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("bye");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub", "Abstract hub.")]
    public abstract class FlagLiteralHubBase : Command
    {
    }

    [Command("hub get", "Gets a value.")]
    public sealed class FlagLiteralHubGetCommand : FlagLiteralHubBase
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub set", "Sets a value.")]
    public sealed class FlagLiteralHubSetCommand : FlagLiteralHubBase
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub set");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task InvalidLiteral_ReportsInvalidValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--verbose=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'banana'. Use bare '--verbose'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task EmptyLiteral_ReportsInvalidValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--verbose="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value. Use bare '--verbose'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("YES")]
    [InlineData("on")]
    [InlineData("off")]
    [InlineData("no")]
    [InlineData("1")]
    [InlineData("0")]
    public async Task ValidLiteral_ReportsInvalidValue(string literal)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, [$"--verbose={literal}"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains($"Option '--verbose' does not accept a value '{literal}'. Use bare '--verbose'", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Theory]
    [InlineData("off")]
    [InlineData("no")]
    [InlineData("Off")]
    [InlineData("false")]
    public async Task SpaceSeparatedLiteral_ReportsInvalidValue(string literal)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["greet", "--verbose", literal]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains($"Option '--verbose' does not accept a value '{literal}'. Use bare '--verbose'", error);
        Assert.Contains("Run 'flag-literal-abstract-test greet --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task SpaceSeparatedLiteral_AbstractPath_ReportsInvalidValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--verbose", "off"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'off'. Use bare '--verbose'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task SpaceSeparatedNonLiteral_StaysPositionalControl()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["greet", "--verbose", "Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SpaceSeparatedNonLiteral_AbstractPath_StaysRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--verbose", "Alice"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("does not accept a value", error);
    }

    [Fact]
    public async Task NullableBool_EqualsForm_ReportsInvalidValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateNullableBuilder, ["nullable", "--verbose=true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'true'. Use bare '--verbose'", error);
    }

    [Fact]
    public async Task PostSeparator_EqualsForm_StaysSilent()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--", "--verbose=true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("does not accept a value", error);
    }

    [Fact]
    public async Task SecretFlag_InvalidLiteral_ReportsBareOnlyRedacted()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--secure=s3cr3t-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--secure' does not accept a value. Use bare '--secure'", error);
        Assert.DoesNotContain("s3cr3t-leak", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task ShortForm_InvalidLiteral_ReportsBareOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["-v=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'banana'. Use bare '--verbose'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task NegatedFlag_EqualsForm_ReportsBareOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-verbose=false"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'false'. Use bare '--no-verbose' to set the flag to 'false'.", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task ValuedOption_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--data=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("does not accept a value", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task BareFlag_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("does not accept a value", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task KnownCluster_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["-ab"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("does not accept a value", error);
        Assert.DoesNotContain("Unknown option", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task PostSeparator_Token_StaysSilent()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--", "--verbose=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("does not accept a value", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task NoPrefixPath_Unchanged()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-verbose=yes"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'yes'. Use bare '--no-verbose' to set the flag to 'false'.", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task LeafOnlyBase_ReportsUnknownOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwoLeafBuilder, ["--verbose=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --verbose", error);
        Assert.DoesNotContain("banana", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task AbstractPrefix_InvalidLiteral_ReportsBareOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hub", "--verbose=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'banana'. Use bare '--verbose'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test hub --help' for more information on specific command options.", error);
    }

    [Command("nullable", "Probes nullable flag equals handling.")]
    public sealed class NullableFlagCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool? Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    private static ApplicationBuilder CreateNullableBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("flag-literal-abstract-test")
            .SetExecutableTitle("Flag Literal Abstract Test")
            .SetExecutableDescription("Flag literal abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<NullableFlagCommand>();
    }

    private static ApplicationBuilder CreateSingleLeafBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("flag-literal-abstract-test")
            .SetExecutableTitle("Flag Literal Abstract Test")
            .SetExecutableDescription("Flag literal abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<FlagLiteralGreetCommand>();
    }

    private static ApplicationBuilder CreateTwoLeafBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("flag-literal-abstract-test")
            .SetExecutableTitle("Flag Literal Abstract Test")
            .SetExecutableDescription("Flag literal abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<FlagLiteralGreetCommand>()
            .AddCommand<FlagLiteralFarewellCommand>();
    }

    private static ApplicationBuilder CreateHubBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("flag-literal-abstract-test")
            .SetExecutableTitle("Flag Literal Abstract Test")
            .SetExecutableDescription("Flag literal abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<FlagLiteralHubGetCommand>()
            .AddCommand<FlagLiteralHubSetCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(Func<ApplicationBuilder> create, string[] args)
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
                var exitCode = await create().RunAsync(args);
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
