using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process regression tests for second-run <c>--help</c> defaults with
/// caller-supplied instance registrations.
/// <c>AddCommand(ICommand)</c>-style registrations keep identity across runs, so
/// value binding mutates the shared instance. Help must report the
/// registration-time initializer default on every run, not the live
/// (possibly already-bound) property value. Type registrations resolve a fresh
/// instance per run and are unaffected. Joins the non-parallel
/// <c>ConsoleDecoupling</c> collection because the console streams are
/// process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class InstanceHelpDefaultTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("insthelp", "Probes help defaults with an instance registration.")]
    public sealed class InstanceHelpCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.")]
        public string Mode { get; set; } = "dev";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"insthelp:{Mode}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task InstanceRegistration_SecondRunHelpShowsInitializerDefault()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new InstanceHelpCommand());

        // Bind an explicit value through a real run: this mutates the shared
        // instance, so a live-value help read on the next run would report
        // the bound value as the default.
        var bound = await RunCapturedAsync(builder, ["insthelp", "--mode", "prod"]);

        Assert.Equal(0, bound.ExitCode);
        Assert.Contains("insthelp:prod", bound.Output);

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["insthelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Default: dev", output);
        Assert.DoesNotContain("Default: prod", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task TypeRegistration_SecondRunHelpShowsInitializerDefault()
    {
        var builder = CreateBuilder();
        builder.AddCommand<InstanceHelpCommand>();

        var bound = await RunCapturedAsync(builder, ["insthelp", "--mode", "prod"]);

        Assert.Equal(0, bound.ExitCode);
        Assert.Contains("insthelp:prod", bound.Output);

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["insthelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Default: dev", output);
        Assert.DoesNotContain("Default: prod", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("instancehelp-test")
            .SetExecutableTitle("InstanceHelp Test")
            .SetExecutableDescription("Instance help default verification CLI.")
            .SetExecutableVersion("9.9.9");
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
