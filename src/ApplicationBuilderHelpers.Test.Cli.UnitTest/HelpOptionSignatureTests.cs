using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Pins the indented option signatures rendered in the help options table.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point so the help-table path (indent + name + placeholder) stays in sync
/// with the bare option signature.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HelpOptionSignatureTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("sigprobe", "Probes option signature rendering.")]
    public sealed class SignatureProbeCommand : Command
    {
        [CommandOption('f', "format", Description = "Output format.")]
        public string Format { get; set; } = "table";

        [CommandOption('v', "verbose", Description = "Enable verbose output.")]
        public bool Verbose { get; set; }

        [CommandOption("mode", Description = "Operation mode.")]
        public string Mode { get; set; } = string.Empty;

        [CommandOption("names", Description = "Names to include.")]
        public List<string> Names { get; set; } = [];

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"probe:{Format}:{Mode}:{Names.Count}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ValuedOptionWithShortName_RendersIndentedSignatureWithPlaceholder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["sigprobe", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Contains("    -f, --format <STRING>", Normalize(output));
    }

    [Fact]
    public async Task FlagOptionWithShortName_RendersIndentedSignatureWithoutPlaceholder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["sigprobe", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        var normalized = Normalize(output);
        Assert.Contains("    -v, --verbose", normalized);
        Assert.DoesNotContain("--verbose <", normalized);
    }

    [Fact]
    public async Task ValuedOptionWithLongNameOnly_RendersIndentedSignatureWithPlaceholder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["sigprobe", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Contains("    --mode <STRING>", Normalize(output));
    }

    [Fact]
    public async Task CollectionOption_RendersIndentedSignatureWithEllipsisPlaceholder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["sigprobe", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Contains("    --names <STRING...>", Normalize(output));
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("sig-test")
            .SetExecutableTitle("Signature Test")
            .SetExecutableDescription("Option signature verification CLI.")
            .SetExecutableVersion("9.9.9")
            .SetHelpWidth(120)
            .AddCommand<SignatureProbeCommand>();
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);

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
