using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Completion gateway contract tests for the pre-parse gateway
/// (<c>CompletionGateway.TryHandle</c>), exercised through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point: pins precedence
/// (completion &gt; help &gt; parse &gt; version), the three fall-through edges that return
/// false to the parse path, the bare-<c>complete</c> handled vs bare-<c>completions</c>
/// fall-through asymmetry, and gateway shadowing of same-named registered commands.
/// Unknown-shell <c>completions script tcsh</c> pins the installer-style
/// <c>Unknown shell</c> error (exit 2), never the <c>No command found</c> parse path.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CompletionGatewayTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("gwdeploy", "Deploys gateway fixtures.")]
    public sealed class GatewayDeployCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("gwserve", "Serves gateway fixtures.")]
    public sealed class GatewayServeCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("complete", "Shadow probe command that must never run.")]
    public sealed class ShadowCompleteCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("SHADOW-COMPLETE-RAN");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("completions install", "Shadow install command that must never run.")]
    public sealed class ShadowInstallCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("SHADOW-INSTALL-RAN");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Complete_ShadowsHelpTokenInProbeLine()
    {
        var line = "gw-test --help";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--help", output);
        Assert.DoesNotContain("USAGE", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_ShadowsVersionTokenInProbeLine()
    {
        var line = "gw-test --version";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("9.9.9", output);
        Assert.DoesNotContain("USAGE", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_ProbeErrorsNeverSurface()
    {
        var line = "gw-test boguscmd --bogus";
        var (exitCode, _, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CompletionsScript_IgnoresTrailingHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["completions", "script", "bash", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("complete -F", output);
        Assert.DoesNotContain("USAGE", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CompletionsInstall_ConsumesHelpAsUnknownOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["completions", "install", "--help"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option '--help'", error);
        Assert.DoesNotContain("USAGE", error);
    }

    [Fact]
    public async Task BareCompletions_FallsThroughToParse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["completions"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found", error);
        Assert.DoesNotContain("Unknown shell", error);
    }

    [Fact]
    public async Task CompletionsScriptShort_FallsThroughToParse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["completions", "script"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found", error);
        Assert.DoesNotContain("Unknown shell", error);
    }

    [Fact]
    public async Task CompletionsScriptUnknownShell_ReturnsExitTwo()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["completions", "script", "tcsh"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown shell 'tcsh'", error);
        Assert.Contains("bash, zsh, pwsh, or fish", error);
        Assert.DoesNotContain("No command found", error);
    }

    [Fact]
    public async Task CompletionsScriptCsh_ReturnsExitTwo()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["completions", "script", "csh"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown shell 'csh'", error);
        Assert.Contains("bash, zsh, pwsh, or fish", error);
        Assert.DoesNotContain("No command found", error);
    }

    [Fact]
    public async Task CompletionsScriptBash_ExitsZero()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["completions", "script", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("complete -F", output);
        Assert.DoesNotContain("USAGE", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CompletionsUnknownSubcommand_FallsThroughToParse()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["completions", "frobnicate"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found", error);
        Assert.DoesNotContain("Unknown shell", error);
    }

    [Fact]
    public async Task BareComplete_IsHandledWithExitZero()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("gwdeploy", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_ShadowsRegisteredCompleteCommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateShadowProbeBuilder, ["complete"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("gwdeploy", output);
        Assert.DoesNotContain("SHADOW-COMPLETE-RAN", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CompletionsInstall_ShadowsRegisteredInstallCommand()
    {
        var home = Path.Combine(Path.GetTempPath(), "gw-shadow-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(home);
        var previousHome = CommandLineParser.CompletionInstaller.HomeProvider;
        var previousEnv = CommandLineParser.CompletionInstaller.EnvironmentProvider;
        CommandLineParser.CompletionInstaller.HomeProvider = () => home;
        CommandLineParser.CompletionInstaller.EnvironmentProvider = static _ => null;
        try
        {
            var (exitCode, output, error) = await RunCapturedAsync(CreateShadowInstallBuilder, ["completions", "install", "--shell", "bash"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("installed:", output);
            Assert.DoesNotContain("SHADOW-INSTALL-RAN", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            CommandLineParser.CompletionInstaller.HomeProvider = previousHome;
            CommandLineParser.CompletionInstaller.EnvironmentProvider = previousEnv;
            try { Directory.Delete(home, recursive: true); } catch { }
        }
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("gw-test")
            .SetExecutableTitle("Gateway Test")
            .SetExecutableDescription("Completion gateway verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<GatewayDeployCommand>()
            .AddCommand<GatewayServeCommand>();
    }

    private static ApplicationBuilder CreateShadowProbeBuilder()
    {
        return CreateBuilder().AddCommand<ShadowCompleteCommand>();
    }

    private static ApplicationBuilder CreateShadowInstallBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("gw-shadow-test")
            .SetExecutableTitle("Gateway Shadow Test")
            .SetExecutableDescription("Completion gateway shadow verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<GatewayDeployCommand>()
            .AddCommand<ShadowInstallCommand>();
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
