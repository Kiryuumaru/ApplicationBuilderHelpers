using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Bare-only flag tests for the CLI parser.
/// Pins the frozen contract through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// every <c>=</c>-form on a flag fails (exit 2) with the bare-only text,
/// a bare flag followed by a boolean-looking word fails at the option,
/// a non-boolean neighbor stays positional, negation and secret redaction
/// keep their branches, nullable flags agree, and post-separator tokens
/// stay positional.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class BareOnlyFlagTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("bareprobe", "Probes bare-only flag handling.")]
    public sealed class BareProbeCommand : Command
    {
        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption("secure", Description = "Secure flag.", Secret = true)]
        public bool Secure { get; set; }

        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Secure: {Secure}");
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("barenull", "Probes nullable bare-only flag handling.")]
    public sealed class BareNullableProbeCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool? Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task EqualsTrueForm_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose=true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'true'. Use bare '--verbose'", error);
        Assert.Contains("--no-verbose", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task EqualsFalseForm_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose=false"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'false'. Use bare '--verbose'", error);
        Assert.Contains("--no-verbose", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task EqualsOffForm_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose=off"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'off'. Use bare '--verbose'", error);
        Assert.Contains("--no-verbose", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task EqualsEmptyForm_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value. Use bare '--verbose'", error);
        Assert.Contains("--no-verbose", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task ShortEqualsForm_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "-v=true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'true'. Use bare '--verbose'", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task SpaceSeparatedFalseWord_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose", "false"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'false'. Use bare '--verbose'", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task SpaceSeparatedOne_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose", "1"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value '1'. Use bare '--verbose'", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task SpaceSeparatedName_Binds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose", "Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.Contains("Items: Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegatedEqualsForm_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--no-verbose=false"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'false'. Use bare '--no-verbose' to set the flag to 'false'.", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task SecretEqualsForm_Redacted()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--secure=s3cr3t-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--secure' does not accept a value. Use bare '--secure'", error);
        Assert.DoesNotContain("s3cr3t-leak", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task NullableEqualsForm_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["barenull", "--verbose=true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--verbose' does not accept a value 'true'. Use bare '--verbose'", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task SeparatorEqualsForm_StaysPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--", "--verbose=true"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.Contains("Items: --verbose=true", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("bare-only-test")
            .SetExecutableTitle("Bare Only Test")
            .SetExecutableDescription("Bare-only flag verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<BareProbeCommand>()
            .AddCommand<BareNullableProbeCommand>();
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
