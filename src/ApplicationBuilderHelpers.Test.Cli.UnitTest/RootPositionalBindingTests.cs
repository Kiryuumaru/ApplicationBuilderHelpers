using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process root-positional binding tests.
/// Pins the concrete-root positional contract through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// a bare value binds a childless concrete root's positional without requiring
/// the <c>--</c> separator, the separator form keeps binding, an exact leaf
/// name still routes to the leaf, a near-miss leaf name still errors with a
/// suggestion instead of binding, a distant value binds the mixed root
/// positional, root positionals render in global help but never in leaf help,
/// help/version and dash tokens keep precedence, and surplus still exits 2.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class RootPositionalBindingTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command(description: "Solo verification root.")]
    public sealed class SoloRootCommand : Command
    {
        [CommandArgument("name", Description = "Root name value.", Position = 0)]
        public string? Name { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"solo root:{Name ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Mixed verification root.")]
    public sealed class MixedRootCommand : Command
    {
        [CommandArgument("name", Description = "Root name value.", Position = 0)]
        public string? Name { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"mixed root:{Name ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("mixedleaf", "Runs the mixed leaf.")]
    public sealed class MixedLeafCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("mixed leaf ran");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task SoloRootPositional_BindsWithoutSeparator()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSoloBuilder, ["Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("solo root:Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SoloRootPositional_SentinelForm_Binds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSoloBuilder, ["--", "Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("solo root:Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task MixedRoot_ExactLeafName_RoutesToLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateMixedBuilder, ["mixedleaf"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("mixed leaf ran", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task MixedRoot_NearMissLeafName_RejectsWithSuggestion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateMixedBuilder, ["mixedleef"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("mixedleef", error);
        Assert.Contains("Did you mean", error);
    }

    [Fact]
    public async Task MixedRoot_DistantValue_BindsRootPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateMixedBuilder, ["Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("mixed root:Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task MixedRoot_SentinelForm_BindsLeafNameLiterally()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateMixedBuilder, ["--", "mixedleaf"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("mixed root:mixedleaf", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SoloRoot_GlobalHelp_ListsPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSoloBuilder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("ARGUMENTS:", output);
        Assert.Contains("Root name value.", output);
        Assert.Contains("[name]", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task MixedRoot_LeafHelp_OmitsRootPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateMixedBuilder, ["mixedleaf", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Runs the mixed leaf.", output);
        Assert.DoesNotContain("NAME", output);
        Assert.DoesNotContain("<name>", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SoloRoot_HelpFlag_ShowsHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSoloBuilder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SoloRoot_UnknownOption_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSoloBuilder, ["--unknown-option"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --unknown-option", error);
    }

    [Fact]
    public async Task SoloRoot_SurplusPositional_Rejects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSoloBuilder, ["Alice", "Bob"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unexpected argument 'Bob'", error);
    }

    private static ApplicationBuilder CreateSoloBuilder()
    {
        return CreateBaseBuilder("solo-root-test", "Solo Root Test", "Solo root verification CLI.")
            .AddCommand<SoloRootCommand>();
    }

    private static ApplicationBuilder CreateMixedBuilder()
    {
        return CreateBaseBuilder("mixed-root-test", "Mixed Root Test", "Mixed root verification CLI.")
            .AddCommand<MixedRootCommand>()
            .AddCommand<MixedLeafCommand>();
    }

    private static ApplicationBuilder CreateBaseBuilder(string name, string title, string description)
    {
        return ApplicationBuilder.Create()
            .SetExecutableName(name)
            .SetExecutableTitle(title)
            .SetExecutableDescription(description)
            .SetExecutableVersion("9.9.9");
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
