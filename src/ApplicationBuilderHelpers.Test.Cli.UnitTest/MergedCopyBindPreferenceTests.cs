using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Issue #482 merged-copy bind-preference guards (green required, firing=stop).
/// D1: merged groups bind under the target command's own copy identity with
/// encounter-order values (secret values never echo under a non-secret copy).
/// D2: bare-ledger fidelity (optional bare fails, satisfied-then-bare optional
/// ignored, satisfied-then-bare required fails). D3: CLI wins over env.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams and the process environment are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class MergedCopyBindPreferenceTests
{
    private const string EnvConfigVariable = "PARKER_MERGEDCOPY_ENVCONFIG";

    private static readonly SemaphoreSlim StateGate = new(1, 1);

    [Command("secdiv alpha", "First leaf with secret token option.")]
    public sealed class SecDivAlphaCommand : Command
    {
        [CommandOption("token", Description = "Token value.", FromAmong = ["json", "xml"], Secret = true)]
        public string? Token { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"secdiv alpha:{Token ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("secdiv beta", "Second leaf with plain token option.")]
    public sealed class SecDivBetaCommand : Command
    {
        [CommandOption("token", Description = "Token value.", FromAmong = ["yaml", "toml"])]
        public string? Token { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"secdiv beta:{Token ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("sharedtags alpha", "First leaf with identical shared tags option.")]
    public sealed class SharedTagsAlphaCommand : Command
    {
        [CommandOption("tags", Description = "Tags.")]
        public string[]? Tags { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Tags: {(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("sharedtags beta", "Second leaf with identical shared tags option.")]
    public sealed class SharedTagsBetaCommand : Command
    {
        [CommandOption("tags", Description = "Tags.")]
        public string[]? Tags { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Tags: {(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("bareprobe", "Probes bare valued-option ledger fidelity.")]
    public sealed class BareProbeCommand : Command
    {
        [CommandOption('c', "config", Description = "Config file path.")]
        public string? Config { get; set; }

        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"config={Config ?? "null"} verbose={Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("barereq", "Probes bare required-option ledger fidelity.")]
    public sealed class BareReqCommand : Command
    {
        [CommandOption('n', "name", Description = "User name.", Required = true)]
        public string? Name { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"name={Name ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("envcliwins", "Probes CLI-wins-over-env precedence.")]
    public sealed class EnvCliWinsCommand : Command
    {
        [CommandOption("config", Description = "Config file path.", EnvironmentVariable = EnvConfigVariable)]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Config: {Config ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    // D1: secret value never echoes, even though the sibling copy is non-secret.
    [Fact]
    public async Task D1_SecretValue_NeverEchoesUnderNonSecretCopy()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SecDivAlphaCommand>().AddCommand<SecDivBetaCommand>(),
            ["secdiv", "alpha", "--token=hunter2secret"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Must be one of", error);
        Assert.DoesNotContain("hunter2secret", error);
        Assert.DoesNotContain("hunter2secret", output);
    }

    // D1: each target binds under its own copy's ValidValues list.
    [Fact]
    public async Task D1_TargetCopyValidValues_DecideBinding()
    {
        var (alphaCode, _, alphaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SecDivAlphaCommand>().AddCommand<SecDivBetaCommand>(),
            ["secdiv", "alpha", "--token=yaml"]);

        Assert.Equal(2, alphaCode);
        Assert.Contains("not valid", alphaError);

        var (betaCode, betaOutput, betaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SecDivAlphaCommand>().AddCommand<SecDivBetaCommand>(),
            ["secdiv", "beta", "--token=yaml"]);

        Assert.Equal(0, betaCode);
        Assert.Contains("secdiv beta:yaml", betaOutput);
        Assert.True(string.IsNullOrWhiteSpace(betaError), $"Expected empty stderr but got: {betaError}");
    }

    // D1: merged collection values preserve encounter order.
    [Fact]
    public async Task D1_MergedCollectionValues_PreserveEncounterOrder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SharedTagsAlphaCommand>().AddCommand<SharedTagsBetaCommand>(),
            ["sharedtags", "alpha", "--tags=a", "--tags=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Tags: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // D1: identity reader P1 prefers the target command's own copy.
    [Fact]
    public void D1_IdentityReader_PrefersTargetCommandCopy()
    {
        var prop = typeof(SecDivAlphaCommand).GetProperty(nameof(SecDivAlphaCommand.Token))!;
        var targetCopy = new SubCommandOptionInfo { Property = prop, PropertyType = typeof(string), LongName = "token", IsSecret = true };
        var storedCopy = new SubCommandOptionInfo { Property = prop, PropertyType = typeof(string), LongName = "token", IsSecret = false };
        var target = new SubCommandInfo { CommandParts = ["secdiv", "alpha"] };
        target.Options.Add(targetCopy);

        var result = new ParseResult { TargetCommand = target };
        result.OptionValues[storedCopy] = ["json"];

        Assert.True(result.TryGetCanonicalIdentityOption("token", out var resolved));
        Assert.Same(targetCopy, resolved);
    }

    // D1: identity reader P2 falls back to encounter order and never synthesizes.
    [Fact]
    public void D1_IdentityReader_FallsBackToEncounterOrder()
    {
        var prop = typeof(SharedTagsAlphaCommand).GetProperty(nameof(SharedTagsAlphaCommand.Tags))!;
        var first = new SubCommandOptionInfo { Property = prop, PropertyType = typeof(string[]), LongName = "tags" };
        var second = new SubCommandOptionInfo { Property = prop, PropertyType = typeof(string[]), LongName = "tags" };
        var target = new SubCommandInfo { CommandParts = ["sharedtags", "alpha"] };

        var result = new ParseResult { TargetCommand = target };
        result.OptionValues[first] = ["a"];
        result.OptionValues[second] = ["b"];

        Assert.True(result.TryGetCanonicalIdentityOption("tags", out var resolved));
        Assert.Same(first, resolved);

        Assert.True(result.TryGetMergedOptionValues(first, out var merged));
        Assert.Equal(["a", "b"], merged);

        Assert.False(result.TryGetCanonicalIdentityOption("missing", out _));
    }

    // D2: trailing bare optional valued option fails.
    [Fact]
    public async Task D2_TrailingBareOptional_ReportsMissing()
    {
        var (exitCode, _, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<BareProbeCommand>(),
            ["bareprobe", "--config"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Missing value for option: -c, --config", error);
    }

    // D2: satisfied-then-bare optional repeat keeps the first value.
    [Fact]
    public async Task D2_SatisfiedThenBareOptional_KeepsFirstValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<BareProbeCommand>(),
            ["bareprobe", "--config", "a.json", "--config"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("config=a.json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // D2: satisfied-then-bare required repeat fails.
    [Fact]
    public async Task D2_SatisfiedThenBareRequired_ReportsMissing()
    {
        var (exitCode, _, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<BareReqCommand>(),
            ["barereq", "--name", "John", "--name"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Missing required option: -n, --name", error);
    }

    // D2: bare valued option never consumes a flag-looking neighbor.
    [Fact]
    public async Task D2_BareValuedOption_WithFlagNeighbor_ReportsMissing()
    {
        var (exitCode, _, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<BareProbeCommand>(),
            ["bareprobe", "--config", "--verbose"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Missing value for option: -c, --config", error);
    }

    // D3: explicit CLI value wins over the environment fallback.
    [Fact]
    public async Task D3_ExplicitCliValue_WinsOverEnvironment()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<EnvCliWinsCommand>(),
            ["envcliwins", "--config=cli.json"],
            new Dictionary<string, string?> { [EnvConfigVariable] = "env.json" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: cli.json", output);
        Assert.DoesNotContain("env.json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // D3: omitted option still binds from the environment.
    [Fact]
    public async Task D3_OmittedOption_BindsFromEnvironment()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<EnvCliWinsCommand>(),
            ["envcliwins"],
            new Dictionary<string, string?> { [EnvConfigVariable] = "env.json" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: env.json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("mergedcopy-test")
            .SetExecutableTitle("MergedCopy Test")
            .SetExecutableDescription("Merged-copy bind-preference verification CLI.")
            .SetExecutableVersion("9.9.9");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args, IReadOnlyDictionary<string, string?>? environment = null)
    {
        await StateGate.WaitAsync();
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
                var exitCode = await builderFactory().RunAsync(args);
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

            StateGate.Release();
        }
    }
}
