using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for a known flag in <c>=</c>-form with an invalid literal on the
/// abstract-root path: a CLI that registers only leaf
/// subcommands has no root implementation, so pre-separator tokens stay on
/// the abstract branch of <c>ArgumentParser</c>. A root-visible (globally
/// promoted) flag rejects <c>--verbose=banana</c> as <c>InvalidValue</c>
/// (exit 2) naming the option plus the valid literals, never as
/// <c>RequiresSubcommand</c>; valid literals, bare flags, valued options,
/// leaf-only bases, and post-separator tokens keep their existing paths.
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

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
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
        Assert.Contains("Invalid Boolean value 'banana' for option '--verbose'.", error);
        Assert.Contains("Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task EmptyLiteral_ReportsInvalidValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--verbose="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value '' for option '--verbose'.", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("false")]
    [InlineData("YES")]
    [InlineData("on")]
    [InlineData("1")]
    [InlineData("0")]
    public async Task ValidLiteral_FallsThroughToRequiresSubcommand(string literal)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, [$"--verbose={literal}"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Invalid Boolean value", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task SecretFlag_InvalidLiteral_OmitsLiteral()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--secure=s3cr3t-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value provided for option '--secure'.", error);
        Assert.Contains("Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
        Assert.DoesNotContain("s3cr3t-leak", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task ShortForm_InvalidLiteral_NamesLongOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["-v=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value 'banana' for option '--verbose'.", error);
        Assert.DoesNotContain("requires a subcommand", error);
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
        Assert.DoesNotContain("Invalid Boolean value", error);
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
        Assert.DoesNotContain("Invalid Boolean value", error);
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
        Assert.DoesNotContain("Invalid Boolean value", error);
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
        Assert.DoesNotContain("Invalid Boolean value", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task NoPrefixPath_Unchanged()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-verbose=yes"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'yes'. Use bare '--no-verbose' to set the flag to 'false'.", error);
        Assert.DoesNotContain("Invalid Boolean value", error);
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
        Assert.DoesNotContain("Invalid Boolean value", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task AbstractPrefix_InvalidLiteral_NamesHubCommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hub", "--verbose=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value 'banana' for option '--verbose'.", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'flag-literal-abstract-test hub --help' for more information on specific command options.", error);
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
