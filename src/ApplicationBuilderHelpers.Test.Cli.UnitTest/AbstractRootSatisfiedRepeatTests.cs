using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for satisfied-then-bare repeats on the abstract-root path: a CLI
/// that registers only leaf subcommands has no root implementation, so
/// pre-separator tokens stay on the abstract branch of <c>ArgumentParser</c>.
/// An optional valued option that already consumed a value (separated form,
/// <c>=</c>-form, attached short remainder, or either alias form) keeps the
/// first value and succeeds, so a later bare occurrence of the same option
/// falls through to <c>RequiresSubcommand</c> (exit 2) instead of reporting
/// <c>Missing value for option</c> — matching the concrete-root validator,
/// which skips satisfied optional bare occurrences. A required valued option
/// repeated bare after a value still reports <c>MissingRequired</c>.
/// Single bare occurrences, unknown-first, help precedence, and the
/// environment-variable behavior keep their existing paths.
/// Error kinds are pinned via their distinct help footers.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AbstractRootSatisfiedRepeatTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private const string OutputVariable = "PARKER_SATISFIEDREPEAT_OUTPUT";

    [Command("greet", "Greets.")]
    public sealed class SatisfiedRepeatGreetCommand : Command
    {
        [CommandOption("config", Description = "Config value.", Required = true)]
        public string? Config { get; set; }

        [CommandOption("output", Description = "Output value.", EnvironmentVariable = OutputVariable)]
        public string? Output { get; set; }

        [CommandOption('o', "outalias", Description = "Aliased output value.")]
        public string? Outalias { get; set; }

        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Config: {Config}");
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Satisfied repeat root.")]
    public sealed class SatisfiedRepeatRootCommand : Command
    {
        [CommandOption("output", Description = "Output value.")]
        public string? Output { get; set; }

        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Output: {Output ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task OptionalSatisfiedThenBareWithFlagNeighbor_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "x", "--output", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalSatisfiedThenTrailingBare_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "x", "--output"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalEqualsSatisfiedThenBare_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output=x", "--output", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalAttachedSatisfiedThenBare_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["-ox", "--outalias", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalAliasSatisfiedThenBare_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--outalias", "x", "-o", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task OptionalSatisfiedThenBareWithEnvironmentSet_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            CreateBuilder,
            ["--output", "x", "--output", "--verbose"],
            new Dictionary<string, string?> { [OutputVariable] = "env-output.txt" });

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.DoesNotContain("env-output.txt", error);
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task RequiredSatisfiedThenBare_StillReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config", "x", "--config", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --config", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task RequiredEqualsSatisfiedThenBare_StillReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config=x", "--config", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --config", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task OptionalBareWithFlagNeighbor_StillReportsMissingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: --output", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task OptionalSatisfiedOnly_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task SatisfiedThenBareWithUnknownNeighbor_ReportsUnknown()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "x", "--output", "--bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --bogus", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Missing value for option", error);
        Assert.Contains("Run 'satisfied-repeat-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task SatisfiedThenBareWithHelpNeighbor_ShowsHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--output", "x", "--output", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ConcreteRoot_SatisfiedThenBare_KeepsFirstValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateConcreteBuilder, ["--output", "x", "--output", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Output: x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("satisfied-repeat-abstract-test")
            .SetExecutableTitle("Satisfied Repeat Abstract Test")
            .SetExecutableDescription("Satisfied repeat abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SatisfiedRepeatGreetCommand>();
    }

    private static ApplicationBuilder CreateConcreteBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("satisfied-repeat-concrete-test")
            .SetExecutableTitle("Satisfied Repeat Concrete Test")
            .SetExecutableDescription("Satisfied repeat concrete verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SatisfiedRepeatRootCommand>();
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
