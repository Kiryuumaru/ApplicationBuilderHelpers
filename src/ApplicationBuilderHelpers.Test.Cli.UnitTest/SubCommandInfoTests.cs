using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

[Collection("ConsoleDecoupling")]
public sealed class SubCommandInfoTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    public enum SubInfoKind { Fast, Slow }

    [Command("subinfo alpha", "Runs subinfo alpha.")]
    public sealed class SubInfoAlphaCommand : Command
    {
        [CommandOption('m', "mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string Mode { get; set; } = "json";

        [CommandOption('c', "count", Description = "Item count.")]
        public int Count { get; set; }

        [CommandOption("kind", Description = "Processing kind.")]
        public SubInfoKind Kind { get; set; } = SubInfoKind.Fast;

        [CommandArgument("target", Description = "Target item.", Position = 0)]
        public string? Target { get; set; }

        [CommandArgument("level", Description = "Level number.", Position = 1)]
        public int Level { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"alpha:{Mode}:{Count}:{Kind}:{Target ?? "null"}:{Level}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("subinfo beta", "Runs subinfo beta.")]
    public sealed class SubInfoBetaCommand : Command
    {
        [CommandOption('r', "reason", Description = "Reason value.", Required = true)]
        public string? Reason { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"beta:{Reason}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task InvalidOptionValue_ReportsAllowedValues()
    {
        var (exitCode, _, error) = await RunCapturedAsync(CreateBuilder, ["subinfo", "alpha", "--mode=bad"]);
        Assert.Equal(1, exitCode);
        Assert.Contains("--mode", error);
        Assert.Contains("Must be one of", error);
        Assert.Contains("json, xml", error);
    }

    [Fact]
    public async Task CommandHelp_ShowsOptionSignatures()
    {
        var (exitCode, output, _) = await RunCapturedAsync(CreateBuilder, ["subinfo", "alpha", "--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("-m, --mode", output);
        Assert.Contains("Output mode.", output);
        Assert.Contains("--count", output);
        Assert.Contains("Item count.", output);
    }

    [Fact]
    public async Task EnumOption_ListsValuesAndRejectsInvalid()
    {
        var (helpCode, helpOutput, _) = await RunCapturedAsync(CreateBuilder, ["subinfo", "alpha", "--help"]);
        Assert.Equal(0, helpCode);
        Assert.Contains("--kind", helpOutput);
        Assert.Contains("Fast", helpOutput);
        Assert.Contains("Slow", helpOutput);
        var (exitCode, _, error) = await RunCapturedAsync(CreateBuilder, ["subinfo", "alpha", "--kind=Bad"]);
        Assert.Equal(1, exitCode);
        Assert.Contains("--kind", error);
        Assert.Contains("Fast", error);
    }

    [Fact]
    public async Task PositionalArguments_ConvertAndExecute()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["subinfo", "alpha", "mytarget", "7", "-mxml", "--count", "5"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("alpha:xml:5:Fast:mytarget:7", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task InvalidArgumentValue_ReportsError()
    {
        var (exitCode, _, error) = await RunCapturedAsync(CreateBuilder, ["subinfo", "alpha", "mytarget", "notanum"]);
        Assert.Equal(1, exitCode);
        Assert.Contains("level", error);
        Assert.Contains("notanum", error);
    }

    [Fact]
    public async Task NestedSiblingCommands_BothExecute()
    {
        var alpha = await RunCapturedAsync(CreateBuilder, ["subinfo", "alpha"]);
        var beta = await RunCapturedAsync(CreateBuilder, ["subinfo", "beta", "--reason=hello"]);
        Assert.Equal(0, alpha.ExitCode);
        Assert.Contains("alpha:json:0:Fast:null:0", alpha.Output);
        Assert.Equal(0, beta.ExitCode);
        Assert.Contains("beta:hello", beta.Output);
    }

    [Fact]
    public async Task CommandHelp_ShowsTypeNames()
    {
        var (exitCode, output, _) = await RunCapturedAsync(CreateBuilder, ["subinfo", "alpha", "--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("<STRING>", output);
        Assert.Contains("<NUMBER>", output);
    }

    [Fact]
    public async Task MissingRequiredOption_ReportsDisplayName()
    {
        var (exitCode, _, error) = await RunCapturedAsync(CreateBuilder, ["subinfo", "beta"]);
        Assert.Equal(1, exitCode);
        Assert.Contains("--reason", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("subinfo-test")
            .SetExecutableTitle("Subinfo Test")
            .SetExecutableDescription("Subcommand info verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SubInfoAlphaCommand>()
            .AddCommand<SubInfoBetaCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(Func<ApplicationBuilder> builderFactory, string[] args, CancellationToken cancellationToken = default)
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
