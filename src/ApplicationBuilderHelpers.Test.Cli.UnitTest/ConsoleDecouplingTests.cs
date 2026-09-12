using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Themes;
using Microsoft.Extensions.Hosting;

using System.Text.RegularExpressions;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

[CollectionDefinition("ConsoleDecoupling", DisableParallelization = true)]
public sealed class ConsoleDecouplingCollection
{
}

/// <summary>
/// In-process console stream decoupling tests for the CLI parser.
/// Captures <see cref="Console.Out"/> / <see cref="Console.Error"/> via
/// <see cref="Console.SetOut(System.IO.TextWriter)"/> around the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ConsoleDecouplingTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets the specified name.")]
    public sealed class DecoupledGreetCommand : Command
    {
        [CommandArgument("name", Description = "Name to greet.", Position = 0)]
        public string? Name { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Hello {Name ?? "world"}!");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("publish", "Publishes the specified artifact.")]
    public sealed class DecoupledPublishCommand : Command
    {
        [CommandArgument("artifact", Description = "Artifact to publish.", Position = 0, Required = true)]
        public string? Artifact { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Publishing {Artifact}.");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task GlobalHelp_WritesToStandardOutputOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(() => CreateBuilder().RunAsync(["--help"]));

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", StripAnsi(output));
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CommandHelp_WritesToStandardOutputOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(() => CreateBuilder().RunAsync(["greet", "--help"]));

        Assert.Equal(0, exitCode);
        Assert.Contains("Greets the specified name.", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Version_WritesToStandardOutputOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(() => CreateBuilder().RunAsync(["--version"]));

        Assert.Equal(0, exitCode);
        Assert.Contains("9.9.9", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnknownOption_WritesToStandardErrorOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(() => CreateBuilder().RunAsync(["greet", "--unknown-option"]));

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: Unknown option: --unknown-option", error);
        Assert.Contains("Run 'decouple-test <command> --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task MissingRequiredArgument_WritesToStandardErrorOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(() => CreateBuilder().RunAsync(["publish"]));

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: Missing required argument", error);
        Assert.Contains("Run 'decouple-test <command> --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task RepeatedRuns_RemainStable()
    {
        var first = await RunCapturedAsync(() => CreateBuilder().RunAsync(["greet", "Alice"]));
        var second = await RunCapturedAsync(() => CreateBuilder().RunAsync(["greet", "Bob"]));
        var help = await RunCapturedAsync(() => CreateBuilder().RunAsync(["--help"]));

        Assert.Equal(0, first.ExitCode);
        Assert.Contains("Hello Alice!", first.Output);
        Assert.Equal(0, second.ExitCode);
        Assert.Contains("Hello Bob!", second.Output);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("USAGE:", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task HelpOutput_IsIdenticalAcrossThemes()
    {
        IConsoleTheme[] themes =
        [
            DefaultConsoleTheme.Instance,
            MonochromeConsoleTheme.Instance,
            HighContrastConsoleTheme.Instance,
            MinimalConsoleTheme.Instance,
        ];

        var rendered = new List<string>();
        foreach (var theme in themes)
        {
            var (exitCode, output, error) = await RunCapturedAsync(
                () => CreateBuilder(builder => ICommandBuilderExtensions.SetTheme(builder, theme)).RunAsync(["--help"]));

            Assert.Equal(0, exitCode);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
            rendered.Add(StripAnsi(output).Replace("\r\n", "\n", StringComparison.Ordinal).Trim());
        }

        foreach (var text in rendered.Skip(1))
        {
            Assert.Equal(rendered[0], text);
        }
    }

    private static ApplicationBuilder CreateBuilder(Action<ApplicationBuilder>? configure = null)
    {
        var builder = ApplicationBuilder.Create()
            .SetExecutableName("decouple-test")
            .SetExecutableTitle("Decouple Test")
            .SetExecutableDescription("Decoupling verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DecoupledGreetCommand>()
            .AddCommand<DecoupledPublishCommand>();
        configure?.Invoke(builder);
        return builder;
    }

    private static string StripAnsi(string text) =>
        Regex.Replace(text, "\x1B\\[[0-9;]*m", string.Empty);

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(Func<Task<int>> run)
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
                var exitCode = await run();
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
