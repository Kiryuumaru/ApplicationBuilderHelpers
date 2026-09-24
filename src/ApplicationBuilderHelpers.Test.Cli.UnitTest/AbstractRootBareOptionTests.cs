using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for a typed-bare valued option on the abstract-root path (#549):
/// a CLI that registers only leaf subcommands has no root implementation, so
/// pre-separator tokens stay on the abstract branch of <c>ArgumentParser</c>.
/// A root-visible (globally promoted) valued option typed bare
/// (<c>--data</c>, <c>-d</c>, <c>-vd</c> cluster tail, env-set
/// <c>--cfg</c>) reports <c>MissingRequired</c> (exit 2), never
/// <c>RequiresSubcommand</c>; valid valued forms, surplus positionals,
/// post-separator tokens, and help/version neighbors keep their paths.
/// Error kinds are pinned via their distinct help footers.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AbstractRootBareOptionTests
{
    private const string CfgVariable = "TYRELL_BAREOPTION_CFG";

    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets.")]
    public sealed class BareOptionGreetCommand : Command
    {
        [CommandOption('d', "data", Description = "Data value.")]
        public string? Data { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Data={Data} Verbose={Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("farewell", "Farewells.")]
    public sealed class BareOptionFarewellCommand : Command
    {
        [CommandOption('d', "data", Description = "Data value.")]
        public string? Data { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("bye");
            return ValueTask.CompletedTask;
        }
    }

    [Command("greet", "Greets.")]
    public sealed class BareOptionEnvGreetCommand : Command
    {
        [CommandOption("cfg", Description = "Cfg value.", EnvironmentVariable = CfgVariable)]
        public string? Cfg { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Cfg={Cfg}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("farewell", "Farewells.")]
    public sealed class BareOptionEnvFarewellCommand : Command
    {
        [CommandOption("cfg", Description = "Cfg value.", EnvironmentVariable = CfgVariable)]
        public string? Cfg { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("bye");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub", "Abstract hub.")]
    public abstract class BareOptionHubBase : Command
    {
    }

    [Command("hub get", "Gets a value.")]
    public sealed class BareOptionHubGetCommand : BareOptionHubBase
    {
        [CommandOption('d', "data", Description = "Data value.")]
        public string? Data { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub get");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task BareLong_ReportsMissingRequired()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--data"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -d, --data", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task BareLong_WithFlag_ReportsMissingRequired()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--data", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -d, --data", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task BareShort_ReportsMissingRequired()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["-d"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -d, --data", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task BareShort_WithFlag_ReportsMissingRequired()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["-d", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -d, --data", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task ClusterValuedTail_ReportsMissingRequired()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["-vd"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -d, --data", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task EnvBare_ReportsMissingRequiredDespiteEnv()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateEnvBuilder, ["--cfg"],
            new Dictionary<string, string?> { [CfgVariable] = "env.json" });

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: --cfg", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task EnvBare_WithFlag_ReportsMissingRequiredDespiteEnv()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateEnvBuilder, ["--cfg", "--verbose"],
            new Dictionary<string, string?> { [CfgVariable] = "env.json" });

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: --cfg", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task EnvOmitted_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateEnvBuilder, ["--verbose"],
            new Dictionary<string, string?> { [CfgVariable] = "env.json" });

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task ValidValued_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--data=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task SpaceValued_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--data", "x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task AttachedShort_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["-dvalue"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task ClusterValued_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["-vdval"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task SurplusPositional_KeepsRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--verbose", "foo"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.DoesNotContain("Unexpected argument", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task SatisfiedValued_WithSurplus_KeepsRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--data", "x", "foo"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.DoesNotContain("Unexpected argument", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task PostSeparator_StaysSilent()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--", "--data"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' to see available commands and options.", error);
    }

    [Fact]
    public async Task UnknownNeighbor_StillUnknownOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--data", "--bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --bogus", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task InvalidLiteral_StillInvalidValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--verbose=banana", "--data"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value 'banana' for option '--verbose'.", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task BareWithHelp_ShowsHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hub", "--data", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Abstract hub.", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task BareWithVersion_ShowsVersion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--data", "--version"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("9.9.9", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AbstractPrefix_Bare_NamesHubCommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hub", "--data"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: -d, --data", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.Contains("Run 'bare-option-abstract-test hub --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task AbstractPrefix_ValidValued_KeepsRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hub", "--data=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'hub' requires a subcommand", error);
        Assert.DoesNotContain("Missing value", error);
        Assert.Contains("Run 'bare-option-abstract-test hub --help' to see available subcommands and options.", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("bare-option-abstract-test")
            .SetExecutableTitle("Bare Option Abstract Test")
            .SetExecutableDescription("Bare option abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<BareOptionGreetCommand>()
            .AddCommand<BareOptionFarewellCommand>();
    }

    private static ApplicationBuilder CreateEnvBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("bare-option-abstract-test")
            .SetExecutableTitle("Bare Option Abstract Test")
            .SetExecutableDescription("Bare option abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<BareOptionEnvGreetCommand>()
            .AddCommand<BareOptionEnvFarewellCommand>();
    }

    private static ApplicationBuilder CreateHubBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("bare-option-abstract-test")
            .SetExecutableTitle("Bare Option Abstract Test")
            .SetExecutableDescription("Bare option abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<BareOptionHubGetCommand>();
    }

    private static Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(Func<ApplicationBuilder> create, string[] args) =>
        RunCapturedAsync(create, args, environment: null);

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> create, string[] args, IReadOnlyDictionary<string, string?>? environment)
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
