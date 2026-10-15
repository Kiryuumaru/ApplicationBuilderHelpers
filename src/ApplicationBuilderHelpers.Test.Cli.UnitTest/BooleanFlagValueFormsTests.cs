using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Flag value-form truth-table tests for the CLI parser.
/// Pins the restored boolean-flag value contract through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// the 8 case-insensitive literals bind in <c>=</c>-form and space-form on
/// long and short spellings, non-literal space neighbors stay positional,
/// invalid <c>=</c>-literals (including empty) fail without becoming
/// positionals, and negation stays bare-only.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class BooleanFlagValueFormsTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("flagprobe", "Probes flag value-form binding.")]
    public sealed class FlagValueProbeCommand : Command
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

    [Command("hubprobe", "Abstract hub probe.")]
    public abstract class HubProbeBase : Command
    {
    }

    [Command("hubprobe get", "Gets a value.")]
    public sealed class HubProbeGetCommand : HubProbeBase
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hubprobe get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("hubprobe set", "Sets a value.")]
    public sealed class HubProbeSetCommand : HubProbeBase
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hubprobe set");
            return ValueTask.CompletedTask;
        }
    }

    public static TheoryData<string, bool> EqualsLiterals => new()
    {
        { "true", true },
        { "TRUE", true },
        { "TrUe", true },
        { "false", false },
        { "FALSE", false },
        { "FaLsE", false },
        { "yes", true },
        { "YES", true },
        { "no", false },
        { "NO", false },
        { "on", true },
        { "ON", true },
        { "off", false },
        { "OFF", false },
        { "1", true },
        { "0", false },
    };

    [Theory]
    [MemberData(nameof(EqualsLiterals))]
    public async Task LongEqualsForm_BindsLiteral(string literal, bool expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", $"--verbose={literal}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Verbose: {expected}", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [MemberData(nameof(EqualsLiterals))]
    public async Task ShortEqualsForm_BindsLiteral(string literal, bool expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", $"-v={literal}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Verbose: {expected}", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [MemberData(nameof(EqualsLiterals))]
    public async Task LongSpaceForm_BindsLiteral(string literal, bool expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "--verbose", literal]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Verbose: {expected}", output);
        Assert.Contains("Items: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [MemberData(nameof(EqualsLiterals))]
    public async Task ShortSpaceForm_BindsLiteral(string literal, bool expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "-v", literal]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Verbose: {expected}", output);
        Assert.Contains("Items: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SpaceNonLiteral_StaysPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "--verbose", "Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.Contains("Items: Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SpaceLiteral_PlusPositional_BindsBoth()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "--verbose", "true", "Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.Contains("Items: Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task InvalidEqualsLiteral_NeverBecomesPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "--verbose=xyz"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value 'xyz' for option '--verbose'. Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
        Assert.DoesNotContain("Items:", error);
    }

    [Fact]
    public async Task EmptyEqualsLiteral_ReportsInvalidBoolean()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "--verbose="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value '' for option '--verbose'. Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
    }

    [Fact]
    public async Task NegatedSpaceForm_StillRejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "--no-verbose", "true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'true'. Use bare '--no-verbose' to set the flag to 'false'.", error);
    }

    [Fact]
    public async Task NegatedEqualsForm_StillRejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "--no-verbose=false"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-verbose' does not accept a value 'false'. Use bare '--no-verbose' to set the flag to 'false'.", error);
    }

    [Fact]
    public async Task SecretInvalidLiteral_RedactsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "--secure=hunter2-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value provided for option '--secure'. Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
        Assert.DoesNotContain("hunter2-leak", error);
    }

    [Fact]
    public async Task AttachedShortForm_StaysClusterPath()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["flagprobe", "-vtrue"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: -t", error);
    }

    [Fact]
    public async Task AbstractHub_SpaceLiteral_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hubprobe", "--verbose", "true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'hubprobe' requires a subcommand", error);
        Assert.DoesNotContain("Invalid Boolean value", error);
        Assert.DoesNotContain("does not accept a value", error);
    }

    [Fact]
    public async Task AbstractHub_ValidEqualsLiteral_FallsThroughToRequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hubprobe", "--verbose=true"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'hubprobe' requires a subcommand", error);
        Assert.DoesNotContain("Invalid Boolean value", error);
    }

    [Fact]
    public async Task AbstractHub_InvalidEqualsLiteral_ReportsInvalidBoolean()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHubBuilder, ["hubprobe", "--verbose=banana"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value 'banana' for option '--verbose'. Expected 'true', 'false', 'yes', 'no', 'on', 'off', '1', or '0'", error);
        Assert.DoesNotContain("requires a subcommand", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("flag-value-forms-test")
            .SetExecutableTitle("Flag Value Forms Test")
            .SetExecutableDescription("Flag value-form verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<FlagValueProbeCommand>();
    }

    private static ApplicationBuilder CreateHubBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("flag-value-forms-test")
            .SetExecutableTitle("Flag Value Forms Test")
            .SetExecutableDescription("Flag value-form verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<HubProbeGetCommand>()
            .AddCommand<HubProbeSetCommand>();
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
