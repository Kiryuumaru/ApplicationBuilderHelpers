using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process position tests for promoted global flags.
/// A flag declared identically on every leaf promotes to a shared global,
/// so root-placed, leaf-placed, interspersed, and valued occurrences bind
/// to the leaf implementation, and a shared secret global redacts invalid
/// values. Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class GlobalFlagPositionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets.")]
    public sealed class PositionGreetCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption("config", Description = "Config path.")]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Config: {Config ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("farewell", "Farewells.")]
    public sealed class PositionFarewellCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption("config", Description = "Config path.")]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Config: {Config ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("outer alpha", "Runs the outer alpha leaf.")]
    public sealed class OuterAlphaCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("outer beta", "Runs the outer beta leaf.")]
    public sealed class OuterBetaCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("vault alpha", "First leaf with shared secret option.")]
    public sealed class VaultAlphaCommand : Command
    {
        [CommandOption("vault-token", Description = "Vault token.", FromAmong = ["red", "blue"], Secret = true)]
        public string VaultToken { get; set; } = "red";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"vault alpha:{VaultToken}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("vault beta", "Second leaf with shared secret option.")]
    public sealed class VaultBetaCommand : Command
    {
        [CommandOption("vault-token", Description = "Vault token.", FromAmong = ["red", "blue"], Secret = true)]
        public string VaultToken { get; set; } = "red";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"vault beta:{VaultToken}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task RootPlacedFlag_ReachesLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--verbose", "greet"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LeafPlacedFlag_ReachesLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["greet", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task InterspersedFlag_ReachesNestedLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateNestedBuilder, ["outer", "--verbose", "alpha"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task RootPlacedValuedOption_ReachesLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--config", "appsettings.json", "greet"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Config: appsettings.json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SharedSecretOption_RootPlacedInvalidValue_OmitsValueKeepsValidList()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateVaultBuilder, ["--vault-token=bogus", "vault", "alpha"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value provided for option '--vault-token' is not valid.", error);
        Assert.Contains("Must be one of: red, blue", error);
        Assert.DoesNotContain("bogus", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("globalflag-test")
            .SetExecutableTitle("GlobalFlag Test")
            .SetExecutableDescription("Global flag position verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<PositionGreetCommand>()
            .AddCommand<PositionFarewellCommand>();
    }

    private static ApplicationBuilder CreateNestedBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("globalflag-test")
            .SetExecutableTitle("GlobalFlag Test")
            .SetExecutableDescription("Global flag position verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<OuterAlphaCommand>()
            .AddCommand<OuterBetaCommand>();
    }

    private static ApplicationBuilder CreateVaultBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("globalflag-test")
            .SetExecutableTitle("GlobalFlag Test")
            .SetExecutableDescription("Global flag position verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<VaultAlphaCommand>()
            .AddCommand<VaultBetaCommand>();
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
