using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for starved valued options on the abstract-root path: a CLI
/// that registers only leaf subcommands has no root implementation, so
/// pre-separator tokens stay on the abstract branch of <c>ArgumentParser</c>.
/// A root-visible (globally promoted) required valued option left bare — with
/// a flag-looking neighbor or trailing — reports <c>MissingRequired</c>
/// (exit 2) naming the option, never <c>RequiresSubcommand</c>; an optional
/// valued option left bare the same way reports <c>Missing value for
/// option</c> (exit 2), even with its environment variable set; in-token
/// <c>=</c>-forms, numeric neighbors,
/// unknown-first, consumable value neighbors, and help precedence keep
/// their existing paths.
/// Error kinds are pinned via their distinct help footers.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AbstractRootValuedNeighborTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private const string OutputVariable = "PARKER_VALUEDNEIGHBOR_OUTPUT";

    [Command("greet", "Greets.")]
    public sealed class ValuedNeighborGreetCommand : Command
    {
        [CommandOption("config", Description = "Config value.", Required = true)]
        public string? Config { get; set; }

        [CommandOption("output", Description = "Output value.", EnvironmentVariable = OutputVariable)]
        public string? Output { get; set; }

        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Config: {Config}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task RequiredValued_WithFlagNeighbor_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --config", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task TrailingBareRequired_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --config", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task EqualsForm_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing required option", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task HelpNeighbor_ShowsHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnknownNeighbor_ErrorsOnNeighbor()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config", "--bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --bogus", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Missing required option", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task NumericNeighbor_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config", "-5"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing required option", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalValued_WithFlagNeighbor_ReportsMissingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: --output", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task TrailingBareOptional_ReportsMissingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: --output", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task OptionalValued_WithConsumableNeighbor_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalValued_EqualsForm_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalValued_WithUnknownNeighbor_ErrorsOnNeighbor()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "--bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --bogus", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task OptionalValued_WithNumericNeighbor_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "-5"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalValued_WithHelpNeighbor_ShowsHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task OptionalValued_WithFlagNeighborAndEnvironmentSet_ReportsMissingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            CreateBuilder,
            ["--output", "--verbose"],
            new Dictionary<string, string?> { [OutputVariable] = "env-output.txt" });

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.DoesNotContain("env-output.txt", error);
        Assert.Contains("Missing value for option: --output", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'valued-neighbor-abstract-test --help' for more information on available commands and options.", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("valued-neighbor-abstract-test")
            .SetExecutableTitle("Valued Neighbor Abstract Test")
            .SetExecutableDescription("Valued neighbor abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ValuedNeighborGreetCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> create, string[] args, IReadOnlyDictionary<string, string?>? environment = null)
    {
        await ConsoleGate.WaitAsync();
        var priors = new Dictionary<string, string?>();
        try
        {
            if (environment is not null)
            {
                foreach (var (name, value) in environment)
                {
                    priors[name] = Environment.GetEnvironmentVariable(name);
                    Environment.SetEnvironmentVariable(name, value);
                }
            }

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
            foreach (var (name, prior) in priors)
            {
                Environment.SetEnvironmentVariable(name, prior);
            }

            ConsoleGate.Release();
        }
    }
}
