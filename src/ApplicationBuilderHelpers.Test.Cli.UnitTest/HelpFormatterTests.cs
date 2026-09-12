using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process help formatter tests for the CLI help system.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point: global help, leaf help, parent help listing children,
/// option default values, and unknown-command error footers.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HelpFormatterTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("helpleaf", "Shows leaf help output.")]
    public sealed class LeafHelpCommand : Command
    {
        [CommandOption("format", Description = "Output format.")]
        public string Format { get; set; } = "table";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"leaf:{Format}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("helpnest alpha", "Runs help nest alpha.")]
    public sealed class NestAlphaCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("nest alpha ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("helpnest beta", "Runs help nest beta.")]
    public sealed class NestBetaCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("nest beta ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task GlobalHelp_ExitsZeroAndContainsUsage()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LeafHelp_ExitsZeroAndContainsUsage()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["helpleaf", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ParentHelp_ListsNestedCommands()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["helpnest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE", output);
        Assert.Contains("Commands for helpnest", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var root = await RunCapturedAsync(CreateBuilder, ["--help"]);

        Assert.Equal(0, root.ExitCode);
        Assert.Contains("helpnest", root.Output);
        Assert.True(string.IsNullOrWhiteSpace(root.Error), $"Expected empty stderr but got: {root.Error}");
    }

    [Fact]
    public async Task LeafHelp_ShowsOptionDefaultValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["helpleaf", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--format", output);
        Assert.Contains("Default: table", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnknownCommand_ErrorFooterContainsHelpHint()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["boguscmd"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("--help", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("help-test")
            .SetExecutableTitle("Help Test")
            .SetExecutableDescription("Help formatter verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<LeafHelpCommand>()
            .AddCommand<NestAlphaCommand>()
            .AddCommand<NestBetaCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args, CancellationToken cancellationToken = default)
    {
        await ConsoleGate.WaitAsync();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await builderFactory().RunAsync(args, cancellationToken);
            outWriter.Flush();
            errorWriter.Flush();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleGate.Release();
        }
    }
}
