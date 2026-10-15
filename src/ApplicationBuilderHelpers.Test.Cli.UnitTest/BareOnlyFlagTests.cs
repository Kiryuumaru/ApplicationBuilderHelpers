using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Flag value-form tests for the CLI parser.
/// Pins the restored contract through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// a flag binds every valid boolean literal in <c>=</c>-form and in
/// space-form, an invalid <c>=</c>-literal (including empty) fails (exit 2)
/// with the invalid-boolean text, a bare flag followed by a non-boolean word
/// keeps the word positional, negation and secret redaction keep their
/// branches, nullable flags agree, and post-separator tokens stay positional.
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
    public async Task EqualsTrueForm_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose=true"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EqualsFalseForm_BindsFalse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose=false"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EqualsOffForm_BindsFalse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose=off"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EqualsEmptyForm_ReportsInvalidBoolean()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value '' for option '--verbose'. Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task ShortEqualsForm_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "-v=true"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SpaceSeparatedFalseWord_BindsFalse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose", "false"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SpaceSeparatedOneWord_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--verbose", "1"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
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
    public async Task SecretEqualsForm_ReportsInvalidBooleanRedacted()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bareprobe", "--secure=s3cr3t-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value provided for option '--secure'. Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
        Assert.DoesNotContain("s3cr3t-leak", error);
        Assert.DoesNotContain("Unknown option", error);
    }

    [Fact]
    public async Task NullableEqualsForm_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["barenull", "--verbose=true"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
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
