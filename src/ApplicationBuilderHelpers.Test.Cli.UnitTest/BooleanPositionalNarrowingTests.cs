using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Positional boolean tests for the CLI parser.
/// Exercises boolean positional binding through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// true/yes/on/1 bind true and false/no/off/0 bind false (case-insensitive);
/// other literals report InvalidValue (exit 2) naming the expected spellings.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class BooleanPositionalNarrowingTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("boolnarrow", "Probes boolean positional narrowing.")]
    public sealed class BooleanNarrowingCommand : Command
    {
        [CommandArgument("flag", Description = "Flag.", Position = 0)]
        public bool Flag { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Flag: {Flag}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("Yes")]
    [InlineData("YES")]
    [InlineData("on")]
    [InlineData("On")]
    [InlineData("ON")]
    [InlineData("1")]
    public async Task Argument_Boolean_TrueValues_BindTrue(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["boolnarrow", input]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Flag: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("no")]
    [InlineData("No")]
    [InlineData("NO")]
    [InlineData("off")]
    [InlineData("Off")]
    [InlineData("OFF")]
    [InlineData("0")]
    public async Task Argument_Boolean_FalseValues_BindFalse(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["boolnarrow", input]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Flag: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("maybe")]
    [InlineData("2")]
    public async Task Argument_Boolean_Synonyms_Reject(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["boolnarrow", input]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains($"Invalid value '{input}' for argument 'flag'", error);
        Assert.Contains("Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("boolnarrow-test")
            .SetExecutableTitle("Bool Narrow Test")
            .SetExecutableDescription("Boolean narrowing verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<BooleanNarrowingCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
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
                var exitCode = await CreateBuilder().RunAsync(args);
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
