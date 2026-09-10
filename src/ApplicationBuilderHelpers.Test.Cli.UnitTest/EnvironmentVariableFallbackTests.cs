using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process environment-variable fallback tests for the CLI parser.
/// Exercises the environment fallback through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// explicit option values take precedence, unset or empty variables are treated
/// as missing, a bare option without a value still falls back, and required
/// options can be satisfied from the environment.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because both the
/// console streams and the process environment are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class EnvironmentVariableFallbackTests
{
    private const string ConfigVariable = "PARKER_ENVFALLBACK_CONFIG";
    private const string TokenVariable = "PARKER_ENVFALLBACK_TOKEN";

    private static readonly SemaphoreSlim EnvGate = new(1, 1);

    [Command("envprobe", "Probes optional environment variable fallback.")]
    public sealed class EnvProbeCommand : Command
    {
        [CommandOption("config", Description = "Config file path.", EnvironmentVariable = ConfigVariable)]
        public string? Config { get; set; }

        [CommandOption("plain", Description = "Option without environment fallback.")]
        public string? Plain { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Config: {Config ?? "null"}");
            Console.WriteLine($"Plain: {Plain ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("envrequired", "Probes required environment variable fallback.")]
    public sealed class EnvRequiredCommand : Command
    {
        [CommandOption("token", Description = "Access token.", EnvironmentVariable = TokenVariable, Required = true)]
        public string? Token { get; set; }

        [CommandOption("name", Description = "User name.", Required = true)]
        public string? Name { get; set; }

        [CommandOption("nickname", Description = "Nickname.", EnvironmentVariable = ConfigVariable)]
        public string? Nickname { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Token: {Token}");
            Console.WriteLine($"Name: {Name}");
            Console.WriteLine($"Nickname: {Nickname ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task OptionalOption_UsesEnvironmentValueWhenNotProvided()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe"],
            new Dictionary<string, string?> { [ConfigVariable] = "env-config.json" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: env-config.json", output);
        Assert.Contains("Plain: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ExplicitValue_TakesPrecedenceOverEnvironmentValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe", "--config", "cli-config.json"],
            new Dictionary<string, string?> { [ConfigVariable] = "env-config.json" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: cli-config.json", output);
        Assert.DoesNotContain("env-config.json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EqualsSyntaxValue_TakesPrecedenceOverEnvironmentValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe", "--config=equals-config.json"],
            new Dictionary<string, string?> { [ConfigVariable] = "env-config.json" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: equals-config.json", output);
        Assert.DoesNotContain("env-config.json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnsetEnvironmentVariable_LeavesOptionEmpty()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe"],
            new Dictionary<string, string?> { [ConfigVariable] = null });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EmptyEnvironmentVariable_TreatedAsUnset()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe"],
            new Dictionary<string, string?> { [ConfigVariable] = string.Empty });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task BareOptionWithoutValue_UsesEnvironmentValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe", "--config"],
            new Dictionary<string, string?> { [ConfigVariable] = "env-config.json" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: env-config.json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task BareOptionWithoutValue_AndWithoutEnvironmentValue_LeavesOptionEmpty()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe", "--config"],
            new Dictionary<string, string?> { [ConfigVariable] = null });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task OptionWithoutDeclaredFallback_IsUnaffectedByEnvironment()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe", "--plain", "hello"],
            new Dictionary<string, string?> { [ConfigVariable] = "env-config.json" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: env-config.json", output);
        Assert.Contains("Plain: hello", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EmptyExplicitValue_TakesPrecedenceOverEnvironmentValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envprobe", "--config", string.Empty],
            new Dictionary<string, string?> { [ConfigVariable] = "env-config.json" });

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("env-config.json", output);
        var configLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Config:", StringComparison.Ordinal));
        Assert.Equal("Config: ", configLine);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task RequiredOption_SatisfiedByEnvironmentVariable()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envrequired", "--name", "Bob"],
            new Dictionary<string, string?> { [TokenVariable] = "s3cret", [ConfigVariable] = "nick" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Token: s3cret", output);
        Assert.Contains("Name: Bob", output);
        Assert.Contains("Nickname: nick", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task RequiredValidation_OptionalFallbackDoesNotSatisfyRequiredName()
    {
        // The optional nickname declares an env fallback, but that does not
        // satisfy the missing required name: name is still reported missing.
        // Note: this does not exercise the internal Apply requiredOnly guard;
        // ParameterValidator only iterates required options, so that path is
        // unreachable through the public entry point.
        var (exitCode, output, error) = await RunCapturedAsync(["envrequired", "--token", "s3cret"],
            new Dictionary<string, string?> { [TokenVariable] = "from-env", [ConfigVariable] = "nick" });

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --name", error);
    }

    [Fact]
    public async Task RequiredOption_MissingWithoutEnvironmentVariable_Fails()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envrequired", "--name", "Bob"],
            new Dictionary<string, string?> { [TokenVariable] = null });

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --token", error);
    }

    [Fact]
    public async Task RequiredOptionWithoutDeclaredFallback_MissingFails()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envrequired"],
            new Dictionary<string, string?> { [TokenVariable] = "s3cret" });

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --name", error);
    }

    [Fact]
    public async Task RequiredOption_EmptyEnvironmentVariable_Fails()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envrequired", "--name", "Bob"],
            new Dictionary<string, string?> { [TokenVariable] = string.Empty });

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --token", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("envvar-test")
            .SetExecutableTitle("EnvVar Test")
            .SetExecutableDescription("Environment fallback verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<EnvProbeCommand>()
            .AddCommand<EnvRequiredCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        string[] args, IReadOnlyDictionary<string, string?>? environment = null)
    {
        await EnvGate.WaitAsync();
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
            foreach (var (name, prior) in priors)
            {
                Environment.SetEnvironmentVariable(name, prior);
            }

            EnvGate.Release();
        }
    }
}
