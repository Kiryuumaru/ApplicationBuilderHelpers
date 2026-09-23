using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process allowed-value message tests for the CLI parser.
/// Options and positional arguments share one message template so callers can
/// rely on a single shape regardless of which parameter rejected the value.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AllowedValueMessageTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("shapemsg", "Probes the option allowed-value message shape.")]
    public sealed class ShapeOptionCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Mode: {Mode ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("shapemsgarg", "Probes the argument allowed-value message shape.")]
    public sealed class ShapeArgumentCommand : Command
    {
        [CommandArgument("mode", Description = "Output mode.", Position = 0, FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Mode: {Mode ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    public enum ShapeShade
    {
        Red,
        Green,
        Blue
    }

    [Command("shapenumarg", "Probes plain-enum argument validation, help and completion.")]
    public sealed class ShapeEnumArgumentCommand : Command
    {
        [CommandArgument("shade", Description = "Shade value.", Position = 0)]
        public ShapeShade Shade { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Shade: {Shade}");
            return ValueTask.CompletedTask;
        }
    }

    public enum ShapeCaseShade
    {
        Low,
        High
    }

    [Command("shapenumcasearg", "Probes case-sensitive plain-enum argument validation.")]
    public sealed class ShapeCaseSensitiveEnumArgumentCommand : Command
    {
        [CommandArgument("level", Description = "Level value.", Position = 0, CaseSensitive = true)]
        public ShapeCaseShade Level { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ArgumentAllowedValueMessage_MatchesOptionMessageShape()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapemsgarg", "yaml"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'yaml' is not valid for", error);
        Assert.Contains("Must be one of: json, xml", error);
    }

    [Fact]
    public async Task OptionAllowedValueMessage_MatchesArgumentMessageShape()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapemsg", "--mode", "yaml"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'yaml' is not valid for option '--mode'", error);
        Assert.Contains("Must be one of: json, xml", error);
    }

    [Fact]
    public async Task PlainEnumArgument_RejectsInvalidValue_WithAutoPopulatedList()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapenumarg", "Purple"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'Purple' is not valid for argument 'shade'", error);
        Assert.Contains("Must be one of: Red, Green, Blue", error);
    }

    [Fact]
    public async Task PlainEnumArgument_AcceptsValidValue_CaseInsensitive()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapenumarg", "green"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Shade: Green", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task PlainEnumArgument_Help_ListsAutoPopulatedValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapenumarg", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Possible values: Red, Green, Blue", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task PlainEnumArgument_Completion_ReturnsAutoPopulatedValues()
    {
        var line = "shapemsg-test shapenumarg ";
        var (exitCode, output, error) = await RunCapturedAsync(["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Red", output);
        Assert.Contains("Green", output);
        Assert.Contains("Blue", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CaseSensitivePlainEnumArgument_AcceptsExactCase()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapenumcasearg", "Low"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Level: Low", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CaseSensitivePlainEnumArgument_RejectsWrongCase()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapenumcasearg", "low"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'low' is not valid for argument 'level'", error);
        Assert.Contains("Must be one of: Low, High", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("shapemsg-test")
            .SetExecutableTitle("ShapeMsg Test")
            .SetExecutableDescription("Allowed-value message verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ShapeOptionCommand>()
            .AddCommand<ShapeArgumentCommand>()
            .AddCommand<ShapeEnumArgumentCommand>()
            .AddCommand<ShapeCaseSensitiveEnumArgumentCommand>();
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
