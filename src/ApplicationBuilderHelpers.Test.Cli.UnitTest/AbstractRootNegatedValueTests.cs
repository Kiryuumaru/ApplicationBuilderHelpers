using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for <c>--no-&lt;name&gt;=value</c> handling on the abstract-root path:
/// a CLI that registers only leaf subcommands has no root implementation, so
/// pre-separator tokens stay on the abstract branch of <c>ArgumentParser</c>.
/// A root-visible (globally promoted) base rejects the valued negation as
/// <c>InvalidValue</c> (exit 2) with the secret-aware no-value text, never as
/// <c>RequiresSubcommand</c>; bare negations still fall through to
/// <c>RequiresSubcommand</c>, leaf-only bases still report
/// <c>UnknownOption</c>, and post-separator tokens stay silent.
/// Error kinds are pinned via their distinct help footers.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AbstractRootNegatedValueTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets.")]
    public sealed class NegatedGreetCommand : Command
    {
        [CommandOption('a', "alpha", Description = "Alpha flag.")]
        public bool Alpha { get; set; }

        [CommandOption('b', "beta", Description = "Beta flag.")]
        public bool Beta { get; set; }

        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption("data", Description = "Data value.")]
        public string? Data { get; set; }

        [CommandOption("secure", Description = "Secure flag.", Secret = true)]
        public bool Secure { get; set; }

        [CommandOption("secret-token", Description = "Secret token.", Secret = true)]
        public string? SecretToken { get; set; }

        [CommandOption("scores", Description = "Scores.")]
        public int[]? Scores { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("farewell", "Farewells.")]
    public sealed class NegatedFarewellCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("bye");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub", "Abstract hub.")]
    public abstract class NegatedHubBase : Command
    {
    }

    [Command("hub get", "Gets a value.")]
    public sealed class NegatedHubGetCommand : NegatedHubBase
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
    public sealed class NegatedHubSetCommand : NegatedHubBase
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
    public async Task RootVisibleFlag_WithValue_RejectsWithoutAcceptingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-verbose=yes"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'yes'. Use bare '--no-verbose' to set the flag to 'false'.", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task RootVisibleFlag_EmptyValue_RejectsWithoutAcceptingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-verbose="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value ''. Use bare '--no-verbose' to set the flag to 'false'.", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task BareNegation_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("does not accept a value", error);
        Assert.Contains("Run 'negated-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task LeafOnlyFlag_WithValue_ReportsUnknownOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwoLeafBuilder, ["--no-verbose=yes"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --no-verbose", error);
        Assert.DoesNotContain("yes", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task ValuedSibling_WithValue_RejectsWithoutBareRemedy()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-data=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-data' does not accept a value 'x'. Negation applies to boolean flags only; omit '--no-data' or use '--data=<value>'.", error);
        Assert.DoesNotContain("Use bare '--no-data'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task SecretValued_WithValue_OmitsRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-secret-token=s3cr3t-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-secret-token' does not accept a value. Negation applies to boolean flags only; omit '--no-secret-token' or use '--secret-token=<value>'.", error);
        Assert.DoesNotContain("s3cr3t-leak", error);
        Assert.DoesNotContain("Use bare '--no-secret-token'", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task SecretFlag_WithValue_OmitsRejectedValueKeepsBareRemedy()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-secure=s3cr3t-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-secure' does not accept a value. Use bare '--no-secure' to set the flag to 'false'.", error);
        Assert.DoesNotContain("s3cr3t-leak", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task PlainCollection_WithValue_EchoesRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-scores=777"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-scores' does not accept a value '777'. Negation applies to boolean flags only; omit '--no-scores' or use '--scores=<value>'.", error);
        Assert.DoesNotContain("Use bare '--no-scores'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task EmptyBase_WithValue_FailsClosedRedacted()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-=hidden"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-' does not accept a value.", error);
        Assert.DoesNotContain("hidden", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task CaseVariantBase_WithValue_ReportsUnknownOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-Verbose=yes"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --no-Verbose", error);
        Assert.DoesNotContain("yes", error);
        Assert.DoesNotContain("does not accept", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task UnknownBase_WithValue_ReportsUnknownOptionWithoutValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-bogus=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --no-bogus", error);
        Assert.DoesNotContain("--no-bogus=x", error);
        Assert.DoesNotContain("'x'", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task PostCommand_LeafDispatch_RejectsWithoutAcceptingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["greet", "--no-verbose=yes"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'yes'. Use bare '--no-verbose' to set the flag to 'false'.", error);
        Assert.Contains("Run 'negated-abstract-test greet --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task PostSeparator_Token_StaysSilent()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--", "--no-verbose=yes"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("does not accept a value", error);
        Assert.Contains("Run 'negated-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task ReservedHelpNegation_WithValue_KeepsReservedError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-help=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-help' does not accept a value 'x'", error);
        Assert.DoesNotContain("Unknown option", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task ReservedHelpNegation_Bare_KeepsReservedError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["--no-help"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-help' is not valid. Use '--help' to show help.", error);
        Assert.DoesNotContain("requires a subcommand", error);
    }

    [Fact]
    public async Task AbstractPrefix_FlagWithValue_RejectsWithoutAcceptingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hub", "--no-verbose=yes"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'yes'. Use bare '--no-verbose' to set the flag to 'false'.", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'negated-abstract-test hub --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task KnownCluster_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["-ab"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Unknown option", error);
        Assert.Contains("Run 'negated-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task NumericToken_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingleLeafBuilder, ["-5"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Unknown option", error);
        Assert.Contains("Run 'negated-abstract-test --help' to see available commands and options.", error);
    }

    private static ApplicationBuilder CreateSingleLeafBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("negated-abstract-test")
            .SetExecutableTitle("Negated Abstract Test")
            .SetExecutableDescription("Negated abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<NegatedGreetCommand>();
    }

    private static ApplicationBuilder CreateTwoLeafBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("negated-abstract-test")
            .SetExecutableTitle("Negated Abstract Test")
            .SetExecutableDescription("Negated abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<NegatedGreetCommand>()
            .AddCommand<NegatedFarewellCommand>();
    }

    private static ApplicationBuilder CreateHubBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("negated-abstract-test")
            .SetExecutableTitle("Negated Abstract Test")
            .SetExecutableDescription("Negated abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<NegatedHubGetCommand>()
            .AddCommand<NegatedHubSetCommand>();
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
