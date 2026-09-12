using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process validation-precedence tests for the CLI parser.
/// Required-option-via-env, optional-option-via-env, and explicit-CLI-beats-env
/// are already covered by <see cref="EnvironmentVariableFallbackTests"/>; this
/// class covers the remaining precedence slots through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// whitespace-only environment values count as missing, and environment-supplied
/// values stay subject to allowed-value and type-conversion validation.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because both the
/// console streams and the process environment are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ValidationPrecedenceTests
{
    private const string TokenVariable = "PARKER_PRECEDENCE_TOKEN";
    private const string ConfigVariable = "PARKER_PRECEDENCE_CONFIG";
    private const string ModeVariable = "PARKER_PRECEDENCE_MODE";
    private const string CountVariable = "PARKER_PRECEDENCE_COUNT";

    private static readonly SemaphoreSlim EnvGate = new(1, 1);

    [Command("precedencereq", "Probes required-option precedence.")]
    public sealed class PrecedenceRequiredCommand : Command
    {
        [CommandOption("token", Description = "Access token.", EnvironmentVariable = TokenVariable, Required = true)]
        public string? Token { get; set; }

        [CommandOption("name", Description = "User name.", Required = true)]
        public string? Name { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Token: {Token}");
            Console.WriteLine($"Name: {Name}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("precedenceopt", "Probes optional-option precedence.")]
    public sealed class PrecedenceOptionalCommand : Command
    {
        [CommandOption("config", Description = "Config file path.", EnvironmentVariable = ConfigVariable)]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Config: {Config ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("precedencecheck", "Probes constrained-option precedence.")]
    public sealed class PrecedenceConstrainedCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", EnvironmentVariable = ModeVariable, FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        [CommandOption("count", Description = "Retry count.", EnvironmentVariable = CountVariable)]
        public int Count { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Mode: {Mode ?? "null"}");
            Console.WriteLine($"Count: {Count}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task WhitespaceEnvironmentValue_DoesNotSatisfyRequiredOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["precedencereq", "--name", "Bob"],
            new Dictionary<string, string?> { [TokenVariable] = " " });

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required option: --token", error);
    }

    [Fact]
    public async Task WhitespaceEnvironmentValue_LeavesOptionalOptionEmpty()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["precedenceopt"],
            new Dictionary<string, string?> { [ConfigVariable] = " " });

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EnvironmentValue_RejectedWhenOutsideAllowedValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["precedencecheck"],
            new Dictionary<string, string?> { [ModeVariable] = "yaml", [CountVariable] = null });

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'yaml' is not valid for option '--mode'", error);
        Assert.Contains("Must be one of: json, xml", error);
    }

    [Fact]
    public async Task EnvironmentValue_RejectedWhenConversionFails()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["precedencecheck"],
            new Dictionary<string, string?> { [ModeVariable] = null, [CountVariable] = "abc" });

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Int32 value: 'abc'", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("precedence-test")
            .SetExecutableTitle("Precedence Test")
            .SetExecutableDescription("Validation precedence verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<PrecedenceRequiredCommand>()
            .AddCommand<PrecedenceOptionalCommand>()
            .AddCommand<PrecedenceConstrainedCommand>();
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
