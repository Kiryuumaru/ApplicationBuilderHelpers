using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Pins issue-636 fault-4: a zero-subcommand app must not advertise a
/// <c>&lt;command&gt;</c> route to nowhere in its root <c>--help</c> footer
/// or its error footer. The usage line already omits <c>&lt;COMMAND&gt;</c>
/// when there are no children; the footer must match.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ZeroSubcommandFooterTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command(description: "Zero subcommand verification root.")]
    public sealed class ZeroRootCommand : Command
    {
        [CommandOption("count", Description = "Count value.")]
        public int Count { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("zero root ran");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ZeroSubcommand_GlobalHelp_Footer_HasNoCommandPlaceholder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.DoesNotContain("<command>", output);
        Assert.DoesNotContain("<COMMAND>", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ZeroSubcommand_UnknownOption_Error_HasNoCommandPlaceholder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["--unknown-option"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --unknown-option", error);
        Assert.DoesNotContain("<command>", error);
    }

    [Fact]
    public async Task ZeroSubcommand_InvalidValue_Error_HasNoCommandPlaceholder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["--count=notanumber"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Int32 value: 'notanumber'", error);
        Assert.DoesNotContain("<command>", error);
    }

    [Fact]
    public async Task ZeroSubcommand_BareInvoke_RunsRoot()
    {
        var (exitCode, output, error) = await RunCapturedAsync([]);

        Assert.Equal(0, exitCode);
        Assert.Contains("zero root ran", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateZeroBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("zero-subcommand-test")
            .SetExecutableTitle("Zero Subcommand Test")
            .SetExecutableDescription("Zero subcommand verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ZeroRootCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
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
            var exitCode = await CreateZeroBuilder().RunAsync(args);
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
