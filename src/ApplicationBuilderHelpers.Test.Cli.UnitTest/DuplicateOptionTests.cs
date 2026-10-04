using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process duplicate-option tests for the CLI parser.
/// Default keeps last-wins for scalar repeats; strict mode via
/// <c>SetRejectDuplicateOptions(true)</c> rejects scalar repeats with
/// <c>Duplicate option: &lt;display-name&gt;</c> (exit 2) while arrays,
/// bare flags, and environment-supplied values stay exempt. Any
/// <c>=</c>-form on a flag is rejected by the bare-only gate before
/// duplicate handling runs.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class DuplicateOptionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("dupscalar", "Probes repeated scalar option handling.")]
    public sealed class DuplicateScalarCommand : Command
    {
        [CommandOption("text", Description = "Text value.")]
        public string? Text { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Text: {Text ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("duparray", "Probes repeated array option handling.")]
    public sealed class DuplicateArrayCommand : Command
    {
        [CommandOption("tags", Description = "Tags.")]
        public string[]? Tags { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Tags: {(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dupflag", "Probes repeated boolean flag handling.")]
    public sealed class DuplicateFlagCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dupalias", "Probes alias-form repeated scalar option handling.")]
    public sealed class DuplicateAliasedCommand : Command
    {
        [CommandOption('t', "text", Description = "Text value.")]
        public string? Text { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Text: {Text ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dupenv", "Probes environment-supplied value with explicit repeat.")]
    public sealed class DuplicateEnvCommand : Command
    {
        [CommandOption("text", Description = "Text value.", EnvironmentVariable = "PARKER_DUP_TEXT")]
        public string? Text { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Text: {Text ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ScalarOption_RepeatedValue_LastWins()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["dupscalar", "--text=a", "--text=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Text: b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ArrayOption_RepeatedValue_IsAllowed()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["duparray", "--tags=a", "--tags=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Tags: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FlagOption_BareRepeat_IsAllowed()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["dupflag", "--verbose", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FlagOption_RepeatedValue_ReportsBareOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["dupflag", "--verbose=true", "--verbose=false"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'true'. Use bare '--verbose'", error);
    }

    [Fact]
    public async Task AliasedOption_RepeatedAcrossAliasForms_LastWins()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["dupalias", "--text=a", "-t", "b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Text: b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Strict_ScalarOption_RepeatedValue_ReportsDuplicate()
    {
        var builder = CreateBuilder().SetRejectDuplicateOptions(true);
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["dupscalar", "--text=a", "--text=b"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate option: --text", error);
    }

    [Fact]
    public async Task Strict_AliasedOption_RepeatedAcrossAliasForms_ReportsDuplicate()
    {
        var builder = CreateBuilder().SetRejectDuplicateOptions(true);
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["dupalias", "--text=a", "-t", "b"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate option: -t, --text", error);
    }

    [Fact]
    public async Task Strict_ArrayOption_RepeatedValue_IsAllowed()
    {
        var builder = CreateBuilder().SetRejectDuplicateOptions(true);
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["duparray", "--tags=a", "--tags=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Tags: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Strict_FlagOption_BareRepeat_IsAllowed()
    {
        var builder = CreateBuilder().SetRejectDuplicateOptions(true);
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["dupflag", "--verbose", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Strict_FlagOption_RepeatedValue_ReportsBareOnly()
    {
        var builder = CreateBuilder().SetRejectDuplicateOptions(true);
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["dupflag", "--verbose=true", "--verbose=false"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'true'. Use bare '--verbose'", error);
    }

    [Fact]
    public async Task Strict_EnvironmentSuppliedValue_WithSingleExplicit_StaysAllowed()
    {
        var prior = Environment.GetEnvironmentVariable("PARKER_DUP_TEXT");
        try
        {
            Environment.SetEnvironmentVariable("PARKER_DUP_TEXT", "env-value");
            var builder = CreateBuilder().SetRejectDuplicateOptions(true);
            var (exitCode, output, error) = await RunCapturedAsync(builder, ["dupenv", "--text=cli"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Text: cli", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            Environment.SetEnvironmentVariable("PARKER_DUP_TEXT", prior);
        }
    }

    [Fact]
    public async Task SetRejectDuplicateOptions_ReturnsSameBuilder()
    {
        var builder = CreateBuilder();
        var result = builder.SetRejectDuplicateOptions(true);

        Assert.Same(builder, result);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("dupscalar-test")
            .SetExecutableTitle("DupScalar Test")
            .SetExecutableDescription("Duplicate scalar option verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DuplicateScalarCommand>()
            .AddCommand<DuplicateArrayCommand>()
            .AddCommand<DuplicateFlagCommand>()
            .AddCommand<DuplicateAliasedCommand>()
            .AddCommand<DuplicateEnvCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
        => await RunCapturedAsync(CreateBuilder(), args);

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(ApplicationBuilder builder, string[] args)
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
                var exitCode = await builder.RunAsync(args);
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
