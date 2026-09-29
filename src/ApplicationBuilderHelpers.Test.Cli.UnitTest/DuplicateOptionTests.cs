using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>Duplicate-option tests, default last-wins and opt-in strict rejection (#593).</summary>
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

    [Command("dupenv", "Probes strict env+CLI non-duplication.")]
    public sealed class DuplicateEnvCommand : Command
    {
        [CommandOption("text", Description = "Text value.", EnvironmentVariable = "DUPSTRICT_TEXT")]
        public string? Text { get; set; }

        [CommandOption("count", Description = "Count value.")]
        public int Count { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Text: {Text ?? "null"} Count: {Count}");
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
    public async Task FlagOption_RepeatedValue_LastWins()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["dupflag", "--verbose=true", "--verbose=false"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
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
    public async Task Strict_ScalarRepeat_ReportsDuplicate()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupscalar", "--text=a", "--text=b"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate option: --text", error);
    }

    [Fact]
    public async Task Strict_AliasedRepeatAcrossForms_ReportsDuplicate()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupalias", "--text=a", "-t", "b"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate option: -t, --text", error);
    }

    [Fact]
    public async Task Strict_SingleOccurrence_Succeeds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupscalar", "--text=a"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Text: a", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Strict_ArrayRepeat_StillAccumulates()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["duparray", "--tags=a", "--tags=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Tags: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Strict_FlagRepeat_StillIdempotent()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupflag", "--verbose", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Strict_ValuedFlagRepeat_StillLastWins()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupflag", "--verbose=true", "--verbose=false"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.DoesNotContain("Duplicate option", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Strict_BareRepeat_ReportsMissingNeverDuplicate()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupscalar", "--text=a", "--text"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: --text", error);
        Assert.DoesNotContain("Duplicate option", error);
    }

    [Fact]
    public async Task Strict_EnvPlusCli_IsNotDuplicate()
    {
        var prior = Environment.GetEnvironmentVariable("DUPSTRICT_TEXT");
        Environment.SetEnvironmentVariable("DUPSTRICT_TEXT", "env-value");
        try
        {
            var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupenv", "--text=cli-value"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Text: cli-value", output);
            Assert.DoesNotContain("env-value", output);
            Assert.DoesNotContain("Duplicate option", error);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            Environment.SetEnvironmentVariable("DUPSTRICT_TEXT", prior);
        }
    }

    [Fact]
    public async Task Strict_DuplicatePlusInvalid_InvalidBeatsDuplicate()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupenv", "--text=a", "--text=b", "--count=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate option: --text", error);
        Assert.Contains("Invalid value", error);
    }

    [Fact]
    public async Task Strict_DuplicatePlusUnknown_UnknownWinsAlone()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateStrictBuilder(), ["dupscalar", "--text=a", "--text=b", "--bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --bogus", error);
        Assert.DoesNotContain("Duplicate option", error);
    }

    private static ApplicationBuilder CreateStrictBuilder() =>
        CreateBuilder().SetRejectDuplicateOptions();

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
