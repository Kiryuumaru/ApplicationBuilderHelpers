using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for mixed-type FromAmong false-reject.
/// Non-string <c>FromAmong</c> entries are normalized into the effective target
/// type before comparison, so an enum option with int entries accepts the
/// defined numeric spellings (<c>0</c>, <c>1</c>) and rejects the undefined
/// ones (<c>2</c>, <c>3</c>) with <c>NotAmong</c>.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class MixedTypeFromAmongTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    public enum MixedLevel
    {
        Low = 0,
        High = 1
    }

    [Command("mixedprobe", "Probes mixed-type FromAmong entries on options.")]
    public sealed class MixedOptionCommand : Command
    {
        [CommandOption("level", Description = "Level choice.", FromAmong = [0, 1, 2, 3])]
        public MixedLevel Level { get; set; }

        [CommandOption("total", Description = "Total choice.", FromAmong = [1, 2])]
        public long Total { get; set; }

        [CommandOption("count", Description = "Count choice.", FromAmong = [2.5])]
        public int Count { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"Total: {Total}");
            Console.WriteLine($"Count: {Count}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("mixedargs", "Probes mixed-type FromAmong entries on arguments.")]
    public sealed class MixedArgumentCommand : Command
    {
        [CommandArgument("level", Description = "Level choice.", Position = 0, FromAmong = [0, 1, 2, 3])]
        public MixedLevel Level { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData("0", "Low")]
    [InlineData("1", "High")]
    public async Task MixedEnumOption_DefinedNumericSpelling_Accepts(string input, string expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["mixedprobe", $"--level={input}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Level: {expected}", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("2")]
    [InlineData("3")]
    public async Task MixedEnumOption_UndefinedNumericSpelling_Rejects(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["mixedprobe", $"--level={input}"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains($"Value '{input}' is not valid for option '--level'", error);
        Assert.Contains("Must be one of: 0, 1, 2, 3", error);
    }

    [Theory]
    [InlineData("0", "Low")]
    [InlineData("1", "High")]
    public async Task MixedEnumArgument_DefinedNumericSpelling_Accepts(string input, string expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["mixedargs", input]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Level: {expected}", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("2")]
    [InlineData("3")]
    public async Task MixedEnumArgument_UndefinedNumericSpelling_Rejects(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["mixedargs", input]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains($"Value '{input}' is not valid for argument 'level'", error);
        Assert.Contains("Must be one of: 0, 1, 2, 3", error);
    }

    [Fact]
    public async Task MixedNumericOption_CrossTypeEntry_Accepts()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["mixedprobe", "--total=1"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Total: 1", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task MixedNumericOption_InexactFractionalEntry_NeverMatches()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["mixedprobe", "--count=2"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value '2' is not valid for option '--count'", error);
        Assert.Contains("Must be one of:", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("mixedtype-test")
            .SetExecutableTitle("MixedType Test")
            .SetExecutableDescription("Mixed-type FromAmong verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<MixedOptionCommand>()
            .AddCommand<MixedArgumentCommand>();
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
