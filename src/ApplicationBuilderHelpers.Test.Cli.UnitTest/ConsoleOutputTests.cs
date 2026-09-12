using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

using System.Text.RegularExpressions;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process console output stream tests.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point to cover the <c>ConsoleOutput</c> routing paths: standard output for
/// help/version, standard error for failures with help footers, command failure
/// messages, and plain (non-colored) rendering while redirected.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ConsoleOutputTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("coutgreet", "Greets the specified name.")]
    public sealed class GreetCommand : Command
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

    [Command("coutpublish", "Publishes the specified artifact.")]
    public sealed class PublishCommand : Command
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

    [Command("coutfail", "Fails with a command error.")]
    public sealed class FailCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            throw new CommandException("cout boom", 4);
        }
    }

    [Fact]
    public async Task GlobalHelp_WritesUsageToStdoutOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Version_WritesVersionToStdoutOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["--version"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("9.9.9", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnknownOption_WritesErrorFooterToStderrOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["coutgreet", "--unknown-option"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: Unknown option: --unknown-option", error);
        Assert.Contains("Run 'cout-test <command> --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task MissingRequiredArgument_WritesErrorFooterToStderrOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["coutpublish"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: Missing required argument", error);
        Assert.Contains("Run 'cout-test <command> --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task CommandFailure_WritesMessageToStderrWithGenericFooter()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["coutfail"]);

        Assert.Equal(4, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: cout boom", error);
        Assert.Contains("Run 'cout-test --help' for more information on available commands and options.", error);
    }

    [Fact]
    public async Task RedirectedStreams_PlainContentSurvivesColorEncoding()
    {
        var help = await RunCapturedAsync(() => CreateBuilder(), ["--help"]);
        var failure = await RunCapturedAsync(() => CreateBuilder(), ["coutfail"]);

        Assert.Equal(0, help.ExitCode);
        Assert.Contains("USAGE:", StripAnsi(help.Output));
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
        Assert.Equal(4, failure.ExitCode);
        Assert.Contains("Error: cout boom", StripAnsi(failure.Error));
        Assert.True(string.IsNullOrWhiteSpace(failure.Output), $"Expected empty stdout but got: {failure.Output}");
    }

    private static string StripAnsi(string text) =>
        Regex.Replace(text, "\x1B\\[[0-9;]*m", string.Empty);

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("cout-test")
            .SetExecutableTitle("Cout Test")
            .SetExecutableDescription("Console output verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<GreetCommand>()
            .AddCommand<PublishCommand>()
            .AddCommand<FailCommand>();
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
