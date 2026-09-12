using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process duplicate-option tests for the CLI parser.
/// A scalar option accepts a single value: repeating it on the command line is
/// rejected with a clear error instead of silently keeping one occurrence.
/// Array options stay repeatable and bare boolean flags stay idempotent.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
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

    [Fact]
    public async Task ScalarOption_RepeatedValue_IsRejected()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["dupscalar", "--text=a", "--text=b"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate option", error);
        Assert.Contains("--text", error);
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
    public async Task FlagOption_RepeatedValue_IsRejected()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["dupflag", "--verbose=true", "--verbose=true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate option", error);
        Assert.Contains("--verbose", error);
    }

    [Fact]
    public async Task AliasedOption_RepeatedAcrossAliasForms_IsRejected()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["dupalias", "--text=a", "-t", "b"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Duplicate option", error);
        Assert.Contains("--text", error);
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
            .AddCommand<DuplicateAliasedCommand>();
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
