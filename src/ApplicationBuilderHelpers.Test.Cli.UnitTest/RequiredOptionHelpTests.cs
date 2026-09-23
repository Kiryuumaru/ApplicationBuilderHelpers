using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process help description tests for required options.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point: a required option renders the <c>(required)</c> marker on the line
/// immediately after its description and omits the <c>Default:</c> line (even for
/// value types whose CLR default would otherwise read as a default),
/// while an optional option with an explicit initializer keeps its
/// <c>Default:</c> line.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class RequiredOptionHelpTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    private const string TokenVariable = "PARKER_REQHELP_TOKEN";

    [Command("reqhelp", "Probes required option help descriptions.")]
    public sealed class RequiredHelpCommand : Command
    {
        [CommandOption("count", Description = "Item count.", Required = true)]
        public int Count { get; set; }

        [CommandOption("label", Description = "Label value.", Required = true)]
        public string? Label { get; set; }

        [CommandOption("retries", Description = "Retry attempts.")]
        public int Retries { get; set; } = 3;

        [CommandOption("token", Description = "Access token.", FromAmong = ["alpha", "beta"], EnvironmentVariable = TokenVariable, Required = true)]
        public string? Token { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"reqhelp:{Count}:{Label}:{Retries}:{Token}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task RequiredInteger_OmitsPhantomZeroDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["reqhelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var block = EntryBlock(output, "--count");
        Assert.Contains("Item count.", block[0], StringComparison.Ordinal);
        Assert.Equal("(required)", block[1].Trim());
        Assert.DoesNotContain("Default:", string.Join("\n", block), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequiredString_ShowsMarkerWithoutDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["reqhelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var block = EntryBlock(output, "--label");
        Assert.Contains("Label value.", block[0], StringComparison.Ordinal);
        Assert.Equal("(required)", block[1].Trim());
        Assert.DoesNotContain("Default:", string.Join("\n", block), StringComparison.Ordinal);
    }

    [Fact]
    public async Task OptionalIntegerWithInitializer_ShowsDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["reqhelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var block = EntryBlock(output, "--retries");
        Assert.Contains("Retry attempts.", block[0], StringComparison.Ordinal);
        Assert.DoesNotContain("(required)", string.Join("\n", block), StringComparison.Ordinal);
        Assert.Contains("Default: 3", string.Join("\n", block), StringComparison.Ordinal);
    }

    [Fact]
    public async Task RequiredWithEnvironmentFallback_ShowsMarkerAndEnvironmentLine()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["reqhelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var block = EntryBlock(output, "--token");
        Assert.Contains("Access token.", block[0], StringComparison.Ordinal);
        Assert.Equal("(required)", block[1].Trim());
        Assert.Contains("Possible values:", block[2], StringComparison.Ordinal);
        Assert.Contains($"Environment variable: {TokenVariable}", block[3], StringComparison.Ordinal);
        Assert.DoesNotContain("Default:", string.Join("\n", block), StringComparison.Ordinal);
    }

    private static List<string> EntryBlock(string output, string marker)
    {
        var lines = Normalize(output).Split('\n');
        var start = Array.FindIndex(lines, l => l.Contains(marker, StringComparison.Ordinal));
        Assert.True(start >= 0, $"Expected help to contain {marker} but got:\n{output}");

        var block = new List<string> { lines[start] };
        for (var i = start + 1; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.Length == 0)
            {
                break;
            }

            if (trimmed.StartsWith('-'))
            {
                break;
            }

            if (trimmed.EndsWith(':'))
            {
                break;
            }

            block.Add(lines[i]);
        }

        return block;
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("reqhelp-test")
            .SetExecutableTitle("ReqHelp Test")
            .SetExecutableDescription("Required help verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<RequiredHelpCommand>();
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);

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
