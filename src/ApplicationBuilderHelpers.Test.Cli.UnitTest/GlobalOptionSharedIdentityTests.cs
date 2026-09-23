using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process shared-identity contract for global options.
/// A single shared identity replaces per-command copies plus canonical-key
/// merging: case-distinct long names stay independent, divergent environment
/// or initializer metadata stays local, scalar repeats resolve last-wins,
/// collections accumulate, and initializers apply only when absent.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class GlobalOptionSharedIdentityTests
{
    private const string EnvAlphaVariable = "PARKER_SHAREDIDENTITY_ALPHA";
    private const string EnvBetaVariable = "PARKER_SHAREDIDENTITY_BETA";
    private const string DefaultPresenceVariable = "PARKER_SHAREDIDENTITY_DEFAULTPRESENCE";

    private static readonly SemaphoreSlim StateGate = new(1, 1);

    [Command("caseprobe", "Probes case-distinct option binding.")]
    public sealed class CaseCollisionProbeCommand : Command
    {
        [CommandOption("Verbose", Description = "Upper spelling.")]
        public string? VerboseUpper { get; set; }

        [CommandOption("verbose", Description = "Lower spelling.")]
        public string? VerboseLower { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Upper: {VerboseUpper ?? "null"}");
            Console.WriteLine($"Lower: {VerboseLower ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("envdiv alpha", "First leaf with environment-backed shared option.")]
    public sealed class EnvDivergentAlphaCommand : Command
    {
        [CommandOption("config", Description = "Config path.", EnvironmentVariable = EnvAlphaVariable)]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"envdiv alpha:{Config ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("envdiv beta", "Second leaf with divergent environment-backed shared option.")]
    public sealed class EnvDivergentBetaCommand : Command
    {
        [CommandOption("config", Description = "Config path.", EnvironmentVariable = EnvBetaVariable)]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"envdiv beta:{Config ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("initdiv alpha", "First leaf with initializer-backed shared option.")]
    public sealed class InitializerDivergentAlphaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "alpha-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"initdiv alpha:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("initdiv beta", "Second leaf with divergent initializer-backed shared option.")]
    public sealed class InitializerDivergentBetaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "beta-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"initdiv beta:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("identinit alpha", "First leaf with identical initializer-backed shared option.")]
    public sealed class IdenticalInitializerAlphaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "same-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"identinit alpha:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("identinit beta", "Second leaf with identical initializer-backed shared option.")]
    public sealed class IdenticalInitializerBetaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "same-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"identinit beta:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("scalewins", "Probes scalar repeat resolution.")]
    public sealed class ScalarLastWinsCommand : Command
    {
        [CommandOption("text", Description = "Text value.")]
        public string? Text { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Text: {Text ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("arrayacc", "Probes collection repeat accumulation.")]
    public sealed class ArrayAccumulateCommand : Command
    {
        [CommandOption("tags", Description = "Tags.")]
        public string[]? Tags { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Tags: {(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("valuedflag", "Probes valued flag repeat resolution.")]
    public sealed class ValuedFlagLastWinsCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("defaultpresence", "Probes initializer fallback ordering.")]
    public sealed class DefaultPresenceCommand : Command
    {
        [CommandOption("text", Description = "Text value.", EnvironmentVariable = DefaultPresenceVariable)]
        public string Text { get; set; } = "fallback";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Text: {Text}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task CaseDistinctLongNames_BindIndependently()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<CaseCollisionProbeCommand>(),
            ["caseprobe", "--Verbose=upper", "--verbose=lower"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Upper: upper", output);
        Assert.Contains("Lower: lower", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DivergentEnvironmentVariable_KeepsOptionLocal()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<EnvDivergentAlphaCommand>().AddCommand<EnvDivergentBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--config", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DivergentInitializer_KeepsOptionLocal()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<InitializerDivergentAlphaCommand>().AddCommand<InitializerDivergentBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--shared", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task IdenticalInitializer_PromotedGlobalHelpShowsDefinitionDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<IdenticalInitializerAlphaCommand>().AddCommand<IdenticalInitializerBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.Contains("--shared", output);
        Assert.Contains("Default: same-default", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DivergentInitializer_PerScopeLeafHelpShowsOwnDefault()
    {
        var (alphaCode, alphaOutput, alphaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<InitializerDivergentAlphaCommand>().AddCommand<InitializerDivergentBetaCommand>(),
            ["initdiv", "alpha", "--help"]);

        Assert.Equal(0, alphaCode);
        Assert.Contains("--shared", alphaOutput);
        Assert.Contains("Default: alpha-default", alphaOutput);
        Assert.DoesNotContain("beta-default", alphaOutput);
        Assert.True(string.IsNullOrWhiteSpace(alphaError), $"Expected empty stderr but got: {alphaError}");

        var (betaCode, betaOutput, betaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<InitializerDivergentAlphaCommand>().AddCommand<InitializerDivergentBetaCommand>(),
            ["initdiv", "beta", "--help"]);

        Assert.Equal(0, betaCode);
        Assert.Contains("--shared", betaOutput);
        Assert.Contains("Default: beta-default", betaOutput);
        Assert.DoesNotContain("alpha-default", betaOutput);
        Assert.True(string.IsNullOrWhiteSpace(betaError), $"Expected empty stderr but got: {betaError}");
    }

    [Fact]
    public async Task ScalarRepeat_LastValueWins()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ScalarLastWinsCommand>(),
            ["scalewins", "--text=a", "--text=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Text: b", output);
        Assert.DoesNotContain("Duplicate option", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CollectionRepeat_AccumulatesValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ArrayAccumulateCommand>(),
            ["arrayacc", "--tags=a", "--tags=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Tags: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ValuedFlagRepeat_LastValueWins()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ValuedFlagLastWinsCommand>(),
            ["valuedflag", "--verbose=true", "--verbose=false"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.DoesNotContain("Duplicate option", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task OmittedOption_PreservesInitializerWhenEnvironmentAbsent()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DefaultPresenceCommand>(),
            ["defaultpresence"],
            new Dictionary<string, string?> { [DefaultPresenceVariable] = null });

        Assert.Equal(0, exitCode);
        Assert.Contains("Text: fallback", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ExplicitValue_OverridesInitializerAndEnvironment()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DefaultPresenceCommand>(),
            ["defaultpresence", "--text=cli"],
            new Dictionary<string, string?> { [DefaultPresenceVariable] = "env-value" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Text: cli", output);
        Assert.DoesNotContain("env-value", output);
        Assert.DoesNotContain("fallback", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("sharedidentity-test")
            .SetExecutableTitle("SharedIdentity Test")
            .SetExecutableDescription("Shared identity verification CLI.")
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
