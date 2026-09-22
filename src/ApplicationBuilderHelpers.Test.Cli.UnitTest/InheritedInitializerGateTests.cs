using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process guards for the global-option initializer gate over inherited options.
/// Options declared on a shared base report the base as their declaring type, so the
/// gate cannot resolve a single registration holder when two leaves derive from it
/// and keeps the option local. An option whose getter cannot be read likewise keeps
/// the option local instead of faulting.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class InheritedInitializerGateTests
{
    private static readonly SemaphoreSlim StateGate = new(1, 1);

    public abstract class SharedIdenticalBase : Command
    {
        [CommandOption("format", Description = "Output format.")]
        public string Format { get; set; } = "table";
    }

    [Command("identbase alpha", "First leaf inheriting the shared base option.")]
    public sealed class IdenticalBaseAlphaCommand : SharedIdenticalBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"identbase alpha:{Format}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("identbase beta", "Second leaf inheriting the shared base option.")]
    public sealed class IdenticalBaseBetaCommand : SharedIdenticalBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"identbase beta:{Format}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class SharedDivergentBase : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "base-default";
    }

    [Command("divshared alpha", "First leaf keeping the shared base default.")]
    public sealed class DivergentBaseAlphaCommand : SharedDivergentBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"divshared alpha:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("divshared beta", "Second leaf overriding the shared base default.")]
    public sealed class DivergentBaseBetaCommand : SharedDivergentBase
    {
        public DivergentBaseBetaCommand()
        {
            Shared = "divergent-default";
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"divshared beta:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("faulty alpha", "First leaf with an unreadable option.")]
    public sealed class UnreadableAlphaCommand : Command
    {
        [CommandOption("faulty", Description = "Unreadable value.")]
        public string? Faulty
        {
            get => throw new InvalidOperationException("Unreadable option.");
            set { }
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("faulty alpha ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("faulty beta", "Second leaf with an unreadable option.")]
    public sealed class UnreadableBetaCommand : Command
    {
        [CommandOption("faulty", Description = "Unreadable value.")]
        public string? Faulty
        {
            get => throw new InvalidOperationException("Unreadable option.");
            set { }
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("faulty beta ran");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task IdenticalBaseInitializer_StaysLocal()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<IdenticalBaseAlphaCommand>().AddCommand<IdenticalBaseBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--format", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var (alphaCode, alphaOutput, alphaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<IdenticalBaseAlphaCommand>().AddCommand<IdenticalBaseBetaCommand>(),
            ["identbase", "alpha", "--help"]);

        Assert.Equal(0, alphaCode);
        Assert.Contains("--format", alphaOutput);
        Assert.Contains("Default: table", alphaOutput);
        Assert.True(string.IsNullOrWhiteSpace(alphaError), $"Expected empty stderr but got: {alphaError}");

        var (betaCode, betaOutput, betaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<IdenticalBaseAlphaCommand>().AddCommand<IdenticalBaseBetaCommand>(),
            ["identbase", "beta", "--help"]);

        Assert.Equal(0, betaCode);
        Assert.Contains("--format", betaOutput);
        Assert.Contains("Default: table", betaOutput);
        Assert.True(string.IsNullOrWhiteSpace(betaError), $"Expected empty stderr but got: {betaError}");
    }

    [Fact]
    public async Task DivergentBaseInitializer_StaysLocal()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentBaseAlphaCommand>().AddCommand<DivergentBaseBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--shared", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var (alphaCode, alphaOutput, alphaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentBaseAlphaCommand>().AddCommand<DivergentBaseBetaCommand>(),
            ["divshared", "alpha", "--help"]);

        Assert.Equal(0, alphaCode);
        Assert.Contains("--shared", alphaOutput);
        Assert.Contains("Default: base-default", alphaOutput);
        Assert.DoesNotContain("divergent-default", alphaOutput);
        Assert.True(string.IsNullOrWhiteSpace(alphaError), $"Expected empty stderr but got: {alphaError}");

        var (betaCode, betaOutput, betaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentBaseAlphaCommand>().AddCommand<DivergentBaseBetaCommand>(),
            ["divshared", "beta", "--help"]);

        Assert.Equal(0, betaCode);
        Assert.Contains("--shared", betaOutput);
        Assert.Contains("Default: divergent-default", betaOutput);
        Assert.DoesNotContain("Default: base-default", betaOutput);
        Assert.True(string.IsNullOrWhiteSpace(betaError), $"Expected empty stderr but got: {betaError}");
    }

    [Fact]
    public async Task UnreadableInitializer_StaysLocalWithoutFault()
    {
        var (typeCode, typeOutput, typeError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<UnreadableAlphaCommand>().AddCommand<UnreadableBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, typeCode);
        Assert.Contains("GLOBAL OPTIONS:", typeOutput);
        Assert.DoesNotContain("--faulty", typeOutput);
        Assert.True(string.IsNullOrWhiteSpace(typeError), $"Expected empty stderr but got: {typeError}");

        var (typeRunCode, typeRunOutput, typeRunError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<UnreadableAlphaCommand>().AddCommand<UnreadableBetaCommand>(),
            ["faulty", "alpha"]);

        Assert.Equal(0, typeRunCode);
        Assert.Contains("faulty alpha ran", typeRunOutput);
        Assert.True(string.IsNullOrWhiteSpace(typeRunError), $"Expected empty stderr but got: {typeRunError}");

        var (instanceCode, instanceOutput, instanceError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand(new UnreadableAlphaCommand()).AddCommand(new UnreadableBetaCommand()),
            ["--help"]);

        Assert.Equal(0, instanceCode);
        Assert.Contains("GLOBAL OPTIONS:", instanceOutput);
        Assert.DoesNotContain("--faulty", instanceOutput);
        Assert.True(string.IsNullOrWhiteSpace(instanceError), $"Expected empty stderr but got: {instanceError}");

        var (instanceRunCode, instanceRunOutput, instanceRunError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand(new UnreadableAlphaCommand()).AddCommand(new UnreadableBetaCommand()),
            ["faulty", "beta"]);

        Assert.Equal(0, instanceRunCode);
        Assert.Contains("faulty beta ran", instanceRunOutput);
        Assert.True(string.IsNullOrWhiteSpace(instanceRunError), $"Expected empty stderr but got: {instanceRunError}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("inheritedgate-test")
            .SetExecutableTitle("InheritedGate Test")
            .SetExecutableDescription("Inherited initializer gate verification CLI.")
            .SetExecutableVersion("9.9.9");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args)
    {
        await StateGate.WaitAsync();
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
                var exitCode = await builderFactory().RunAsync(args);
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
            StateGate.Release();
        }
    }
}
