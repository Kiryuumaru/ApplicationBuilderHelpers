using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using System.Text.RegularExpressions;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process stdout-purity tests for hosting lifetime diagnostics.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point to prove machine-parsable stdout carries only command output while
/// hosting lifetime diagnostics never reach stdout and command diagnostics stay on stderr.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HostingLifetimeStreamTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private static readonly string[] LifetimeTokens =
    [
        "Application started",
        "Application stopping",
        "Microsoft.Hosting.Lifetime",
        "Hosting lifetime",
        "Now listening",
        "Content root",
        "Hosting environment",
    ];

    [Command("report", "Emits a single machine-parsable payload.")]
    public sealed class ReportCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("payload:42");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("streamfail", "Fails with a command error.")]
    public sealed class StreamFailCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            throw new CommandException("stream boom", 4);
        }
    }

    [Command("streamlifetime", "Registers lifetime callbacks before finishing.")]
    public sealed class StreamLifetimeCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            using var scope = applicationHost.Services.CreateScope();
            var lifetime = scope.ServiceProvider.GetRequiredService<LifetimeService>();
            lifetime.ApplicationExitingCallback(() => Console.WriteLine("stream exiting"));
            lifetime.ApplicationExitedCallback(() => Console.WriteLine("stream exited"));
            Console.WriteLine("payload:42");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task SuccessCommand_WritesOnlyPayloadToStdout()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["report"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("payload:42", StripAnsi(output).Trim());
        Assert.DoesNotContain("payload:42", error);
        AssertNoLifetimeTokens(output);
    }

    [Fact]
    public async Task GlobalHelp_WritesOnlyHelpToStdout()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", StripAnsi(output));
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        AssertNoLifetimeTokens(output);
    }

    [Fact]
    public async Task Version_WritesOnlyVersionToStdout()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["--version"]);

        Assert.Equal(0, exitCode);
        Assert.Equal("9.9.9", StripAnsi(output).Trim());
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        AssertNoLifetimeTokens(output);
    }

    [Fact]
    public async Task UnknownOption_WritesDiagnosticsToStderrOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["report", "--unknown-option"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: Unknown option: --unknown-option", error);
        AssertNoLifetimeTokens(output);
    }

    [Fact]
    public async Task CommandFailure_WritesDiagnosticsToStderrOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder(), ["streamfail"]);

        Assert.Equal(4, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error: stream boom", error);
        AssertNoLifetimeTokens(output);
    }

    [Fact]
    public async Task LifetimeCallbacks_StillFire()
    {
        var (exitCode, output, _) = await RunCapturedAsync(
            () => CreateBuilder(), ["streamlifetime"]);

        Assert.Equal(0, exitCode);
        var text = StripAnsi(output);
        Assert.Contains("payload:42", text);
        Assert.Contains("stream exiting", text);
        Assert.Contains("stream exited", text);
        Assert.True(
            text.IndexOf("payload:42", StringComparison.Ordinal) < text.IndexOf("stream exiting", StringComparison.Ordinal),
            $"Expected payload before exiting callbacks but got: {text}");
        Assert.True(
            text.IndexOf("stream exiting", StringComparison.Ordinal) < text.IndexOf("stream exited", StringComparison.Ordinal),
            $"Expected exiting callbacks before exited callbacks but got: {text}");
        AssertNoLifetimeTokens(output);
    }

    private static void AssertNoLifetimeTokens(string output)
    {
        var text = StripAnsi(output);
        foreach (var token in LifetimeTokens)
        {
            Assert.DoesNotContain(token, text, StringComparison.OrdinalIgnoreCase);
        }
    }

    private static string StripAnsi(string text) =>
        Regex.Replace(text, "\x1B\\[[0-9;]*m", string.Empty);

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("stream-test")
            .SetExecutableTitle("Stream Test")
            .SetExecutableDescription("Stdout purity verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ReportCommand>()
            .AddCommand<StreamFailCommand>()
            .AddCommand<StreamLifetimeCommand>();
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
