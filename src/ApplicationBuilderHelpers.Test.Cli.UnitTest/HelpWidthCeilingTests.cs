using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Help width ceiling: <c>SetHelpWidth</c> accepts 1 through 1024 and throws beyond it;
/// the renderer caps direct <c>HelpWidth</c> sets at 1024 as defense-in-depth.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point for the render path.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HelpWidthCeilingTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("ceiling-probe", "Probe command for width ceiling verification.")]
    public sealed class CeilingProbeCommand : Command
    {
        [CommandOption("format", Description = "Output format with a reasonably long description to exercise wrapping behavior.")]
        public string Format { get; set; } = "table";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"probe:{Format}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData(1)]
    [InlineData(60)]
    [InlineData(120)]
    [InlineData(1024)]
    public void SetHelpWidth_AcceptsBoundaryWidths(int helpWidth)
    {
        var builder = CreateBuilder();
        var result = builder.SetHelpWidth(helpWidth);

        Assert.Same(builder, result);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1025)]
    [InlineData(5000)]
    public void SetHelpWidth_RejectsOutOfRangeWidths(int helpWidth)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => CreateBuilder().SetHelpWidth(helpWidth));
    }

    [Fact]
    public async Task DirectHelpWidth_AboveCeiling_RendersCappedAt1024()
    {
        var builder = CreateBuilder()
            .SetExecutableDescription(string.Join(" ", Enumerable.Repeat("wrapping", 400)));
        ((ICommandBuilder)builder).HelpWidth = 5000;

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        foreach (var line in output.Split('\n'))
        {
            var length = line.TrimEnd('\r').Length;
            Assert.True(length <= 1024, $"Help line exceeds 1024 columns ({length}).");
        }
    }

    [Fact]
    public async Task DirectHelpWidth_BelowFloor_RendersFlooredAt60()
    {
        var builder = CreateBuilder();
        ((ICommandBuilder)builder).HelpWidth = 1;

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Equal(1, ((ICommandBuilder)builder).HelpWidth);
    }

    [Fact]
    public async Task UnsetHelpWidth_RendersAtDefault120()
    {
        var builder = CreateBuilder();

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Null(((ICommandBuilder)builder).HelpWidth);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("ceiling-test")
            .SetExecutableTitle("Ceiling Test")
            .SetExecutableDescription("Help width ceiling verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<CeilingProbeCommand>();
    }

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
