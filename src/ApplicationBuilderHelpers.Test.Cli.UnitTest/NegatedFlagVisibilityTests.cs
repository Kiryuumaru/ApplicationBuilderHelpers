using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Visibility tests for hidden <c>--no-*</c> bool negation on a leaf command:
/// a bool flag binds bare <c>--no-verbose</c> to false, help lists the negated
/// flag form only, valued-option negations reject as <c>InvalidValue</c> (exit 2)
/// with bool-only guidance, and bare valued negations defer to
/// <c>--version</c> when version wins.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class NegatedFlagVisibilityTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets.")]
    public sealed class VisibilityGreetCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption("shout", Description = "Shout value.")]
        public string? Shout { get; set; }

        [CommandArgument("person", Description = "Person.", Position = 0)]
        public string? Person { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Shout: {Shout ?? "null"}");
            Console.WriteLine($"Person: {Person ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task NegatedFlag_SetsVerboseFalse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["greet", "Alice", "--no-verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.Contains("Person: Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Help_ListsNegationForFlagOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["greet", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Contains("--no-verbose", output);
        Assert.DoesNotContain("--no-shout", output);
    }

    [Fact]
    public async Task BareValuedNegation_RejectsWithoutAcceptingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["greet", "Alice", "--no-shout"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-shout' does not accept a value. Negation applies to boolean flags only; omit '--no-shout' or use '--shout=<value>'.", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task ValuedNegation_RejectsWithoutAcceptingValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["greet", "Alice", "--no-shout=x"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-shout' does not accept a value 'x'. Negation applies to boolean flags only; omit '--no-shout' or use '--shout=<value>'.", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task BareValuedNegation_WithTrailingVersion_ShowsVersion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["greet", "Alice", "--no-shout", "--version"]);

        Assert.Equal(0, exitCode);
        Assert.Matches(@"(?m)^\d+\.\d+\.\d+", output);
        Assert.DoesNotContain("does not accept a value", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task BareValuedNegation_WithLeadingVersion_ShowsVersion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["greet", "Alice", "--version", "--no-shout"]);

        Assert.Equal(0, exitCode);
        Assert.Matches(@"(?m)^\d+\.\d+\.\d+", output);
        Assert.DoesNotContain("does not accept a value", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("negated-visibility-test")
            .SetExecutableTitle("Negated Visibility Test")
            .SetExecutableDescription("Negated visibility verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<VisibilityGreetCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(Func<ApplicationBuilder> create, string[] args)
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
                var exitCode = await create().RunAsync(args);
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
