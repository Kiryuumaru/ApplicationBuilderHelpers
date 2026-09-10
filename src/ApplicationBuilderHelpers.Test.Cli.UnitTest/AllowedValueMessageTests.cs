using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process allowed-value message tests for the CLI parser.
/// Options and positional arguments share one message template so callers can
/// rely on a single shape regardless of which parameter rejected the value.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
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

    [Fact]
    public async Task ArgumentAllowedValueMessage_MatchesOptionMessageShape()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapemsgarg", "yaml"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'yaml' is not valid for", error);
        Assert.Contains("Must be one of: json, xml", error);
    }

    [Fact]
    public async Task OptionAllowedValueMessage_MatchesArgumentMessageShape()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["shapemsg", "--mode", "yaml"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'yaml' is not valid for option '--mode'", error);
        Assert.Contains("Must be one of: json, xml", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("shapemsg-test")
            .SetExecutableTitle("ShapeMsg Test")
            .SetExecutableDescription("Allowed-value message verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ShapeOptionCommand>()
            .AddCommand<ShapeArgumentCommand>();
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
