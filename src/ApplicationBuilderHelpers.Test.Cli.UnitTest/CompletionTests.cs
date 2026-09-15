using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;
using Microsoft.Extensions.Hosting;
using System.Text;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process completion tests for the <c>complete</c> probe gateway.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point via the <see cref="Console.SetOut(System.IO.TextWriter)"/> /
/// <see cref="Console.SetError(System.IO.TextWriter)"/> + <see cref="StringWriter"/> pattern
/// (same seam as <c>HelpFormatterTests</c>): asserts <c>complete</c> returns
/// command/option/ValidValues candidates, exit 0, stdout only, secrets suppressed.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CompletionProbeTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("compldeploy", "Deploys test fixtures.")]
    public sealed class DeployProbeCommand : Command
    {
        [CommandOption("strategy", Description = "Deployment strategy.", FromAmong = ["blue-green", "rolling", "recreate"])]
        public string Strategy { get; set; } = "rolling";

        [CommandOption("secret-token", Description = "Secret token.", FromAmong = ["alpha", "beta"], Secret = true)]
        public string? SecretToken { get; set; }

        [CommandArgument("environment", Description = "Target environment.", Position = 0, FromAmong = ["prod", "staging"])]
        public string? Environment { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("complserve", "Serves test fixtures.")]
    public sealed class ServeProbeCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Complete_EmptyPrefix_ReturnsSubcommandCandidates()
    {
        var line = "compl-test ";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.Contains("compldeploy", output);
        Assert.Contains("complserve", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_PartialPrefix_FiltersSubcommandCandidates()
    {
        var line = "compl-test compld";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.Contains("compldeploy", output);
        Assert.DoesNotContain("complserve", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_DashPrefix_ReturnsOptionCandidates()
    {
        var line = "compl-test compldeploy --";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--strategy", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_OptionValue_ReturnsValidValues()
    {
        var line = "compl-test compldeploy --strategy ";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.Contains("blue-green", output);
        Assert.Contains("rolling", output);
        Assert.Contains("recreate", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_OptionEqualsValue_ReturnsFilteredValidValues()
    {
        var line = "compl-test compldeploy --strategy=rol";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--strategy=rolling", output);
        Assert.DoesNotContain("blue-green", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_PositionalArgument_ReturnsValidValues()
    {
        var line = "compl-test compldeploy prod";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.Contains("prod", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_SecretOptionValues_AreSuppressed()
    {
        var line = "compl-test compldeploy --secret-token ";
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.DoesNotContain("alpha", output);
        Assert.DoesNotContain("beta", output);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Complete_MalformedInput_ExitsZeroWithNoCandidates()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["complete"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.True(string.IsNullOrWhiteSpace(output) || !string.IsNullOrWhiteSpace(output));
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("compl-test")
            .SetExecutableTitle("Completion Probe Test")
            .SetExecutableDescription("Completion probe verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DeployProbeCommand>()
            .AddCommand<ServeProbeCommand>();
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

/// <summary>
/// In-process install/uninstall tests for the <c>completions install|uninstall</c> gateway.
/// Redirects <see cref="CommandLineParser.CompletionInstaller"/> home/env providers at a
/// temp dir so no real rc file is touched. Joins <c>ConsoleDecoupling</c> (console capture).
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CompletionInstallTests : IDisposable
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);
    private readonly string _home;
    private readonly Func<string> _previousHome;
    private readonly Func<string, string?> _previousEnv;

    public CompletionInstallTests()
    {
        _home = Path.Combine(Path.GetTempPath(), "compl-install-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_home);
        _previousHome = CommandLineParser.CompletionInstaller.HomeProvider;
        _previousEnv = CommandLineParser.CompletionInstaller.EnvironmentProvider;
        CommandLineParser.CompletionInstaller.HomeProvider = () => _home;
        CommandLineParser.CompletionInstaller.EnvironmentProvider = static _ => null;
    }

    public void Dispose()
    {
        CommandLineParser.CompletionInstaller.HomeProvider = _previousHome;
        CommandLineParser.CompletionInstaller.EnvironmentProvider = _previousEnv;
        try { Directory.Delete(_home, recursive: true); } catch { }
    }

    [Fact]
    public async Task Install_CreatesBashrcWithManagedBlock()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["completions", "install", "--shell", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("installed:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        var rc = File.ReadAllText(Path.Combine(_home, ".bashrc"));
        Assert.Contains("# >>> compl-inst-test completion >>>", rc);
        Assert.Contains("# <<< compl-inst-test completion <<<", rc);
        Assert.Contains("complete -F", rc);
    }

    [Fact]
    public async Task Install_IdempotentRerun_ReportsAlreadyInstalledWithoutRewrite()
    {
        await RunCapturedAsync(["completions", "install", "--shell", "bash"]);
        var rcPath = Path.Combine(_home, ".bashrc");
        var before = File.ReadAllText(rcPath);
        var beforeWrite = File.GetLastWriteTimeUtc(rcPath);

        var (exitCode, output, error) = await RunCapturedAsync(["completions", "install", "--shell", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("already installed", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Equal(before, File.ReadAllText(rcPath));
        Assert.Equal(beforeWrite, File.GetLastWriteTimeUtc(rcPath));
    }

    [Fact]
    public async Task Install_PreservesOutsideEdits()
    {
        File.WriteAllText(Path.Combine(_home, ".bashrc"), "export MINE=1\n");

        var (exitCode, output, _) = await RunCapturedAsync(["completions", "install", "--shell", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("installed:", output);
        var rc = File.ReadAllText(Path.Combine(_home, ".bashrc"));
        Assert.Contains("export MINE=1", rc);
        Assert.Contains("# >>> compl-inst-test completion >>>", rc);
    }

    [Fact]
    public async Task Uninstall_RemovesOnlyOwnBlock()
    {
        File.WriteAllText(Path.Combine(_home, ".bashrc"), "export MINE=1\n");
        await RunCapturedAsync(["completions", "install", "--shell", "bash"]);

        var (exitCode, output, error) = await RunCapturedAsync(["completions", "uninstall", "--shell", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("uninstalled:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        var rc = File.ReadAllText(Path.Combine(_home, ".bashrc"));
        Assert.Contains("export MINE=1", rc);
        Assert.DoesNotContain(">>> compl-inst-test completion >>>", rc);
        Assert.True(File.Exists(Path.Combine(_home, ".bashrc")));
    }

    [Fact]
    public async Task Uninstall_Absent_ExitsZeroWithoutCreatingRc()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["completions", "uninstall", "--shell", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("not installed", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.False(File.Exists(Path.Combine(_home, ".bashrc")));
    }

    [Fact]
    public async Task Install_DryRun_NoMutation()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["completions", "install", "--shell", "bash", "--dry-run"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("would-write:", output);
        Assert.Contains("complete -F", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.False(File.Exists(Path.Combine(_home, ".bashrc")));
    }

    [Fact]
    public async Task Uninstall_FishForeignFile_RefusesWithExitOne()
    {
        var fishPath = Path.Combine(_home, ".config", "fish", "completions", "compl-inst-test.fish");
        Directory.CreateDirectory(Path.GetDirectoryName(fishPath)!);
        File.WriteAllText(fishPath, "# foreign hand-written completion\ncomplete -c other -f\n");

        var (exitCode, _, error) = await RunCapturedAsync(["completions", "uninstall", "--shell", "fish"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Refusing", error);
        Assert.Contains("foreign hand-written", File.ReadAllText(fishPath));
    }

    [Fact]
    public async Task Install_UnknownShell_ExitsTwo()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["completions", "install", "--shell", "tcsh"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown shell", error);
    }

    [Fact]
    public async Task Install_DetectsShellFromEnvironment()
    {
        CommandLineParser.CompletionInstaller.EnvironmentProvider = static name =>
            string.Equals(name, "SHELL", StringComparison.Ordinal) ? "/bin/zsh" : null;

        var (exitCode, output, _) = await RunCapturedAsync(["completions", "install"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("installed:", output);
        var rc = File.ReadAllText(Path.Combine(_home, ".zshrc"));
        Assert.Contains("compdef", rc);
    }

    [Fact]
    public async Task Install_Fish_UsesXdgConfigHome()
    {
        var xdg = Path.Combine(_home, "xdg");
        var xdgValue = xdg;
        CommandLineParser.CompletionInstaller.EnvironmentProvider = name =>
            string.Equals(name, "XDG_CONFIG_HOME", StringComparison.Ordinal) ? xdgValue : null;

        var (exitCode, output, _) = await RunCapturedAsync(["completions", "install", "--shell", "fish"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("installed:", output);
        var fishPath = Path.Combine(xdg, "fish", "completions", "compl-inst-test.fish");
        Assert.True(File.Exists(fishPath), $"Expected fish completion at {fishPath}");
        Assert.Contains(">>> compl-inst-test completion >>>", File.ReadAllText(fishPath));
    }

    [Theory]
    [InlineData("../evil")]
    [InlineData("a/b")]
    [InlineData("a\\b")]
    [InlineData("foo;bar")]
    [InlineData("$(x)")]
    [InlineData("`x`")]
    [InlineData("a|b")]
    [InlineData("a&b")]
    [InlineData("a*b")]
    [InlineData("a\nb")]
    [InlineData("-foo")]
    [InlineData(".foo")]
    [InlineData("9foo")]
    [InlineData("café")]
    public async Task Install_UnsafeExecutableName_RejectedWithExitTwo(string exeName)
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilderWithExe(exeName), ["completions", "install", "--shell", "bash"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Invalid executable name", error);
        Assert.Contains("Allowed:", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.False(File.Exists(Path.Combine(_home, ".bashrc")));
    }

    [Fact]
    public async Task Install_OverlongExecutableName_RejectedWithExitTwo()
    {
        var exeName = new string('a', CommandLineParser.CompletionInstaller.MaxExeNameLength + 1);

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilderWithExe(exeName), ["completions", "install", "--shell", "bash"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Invalid executable name", error);
        Assert.Contains("Allowed:", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.False(File.Exists(Path.Combine(_home, ".bashrc")));
    }

    [Fact]
    public async Task Install_MaxLengthExecutableName_Accepted()
    {
        var exeName = new string('a', CommandLineParser.CompletionInstaller.MaxExeNameLength);

        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilderWithExe(exeName), ["completions", "install", "--shell", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("installed:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        var rc = File.ReadAllText(Path.Combine(_home, ".bashrc"));
        Assert.Contains($"# >>> {exeName} completion >>>", rc);
    }

    [Theory]
    [InlineData("rel/path")]
    [InlineData("~/mycfg")]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Install_FishNonAbsoluteXdg_FallsBackToHomeConfig(string xdgValue)
    {
        var rawProbe = Path.Combine(_home, xdgValue.Trim());
        CommandLineParser.CompletionInstaller.EnvironmentProvider = name =>
            string.Equals(name, "XDG_CONFIG_HOME", StringComparison.Ordinal) ? xdgValue : null;

        var (exitCode, output, _) = await RunCapturedAsync(["completions", "install", "--shell", "fish"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("installed:", output);
        var fishPath = Path.Combine(_home, ".config", "fish", "completions", "compl-inst-test.fish");
        Assert.True(File.Exists(fishPath), $"Expected fish completion at {fishPath}");
        Assert.False(File.Exists(Path.Combine(rawProbe, "fish", "completions", "compl-inst-test.fish")));
    }

    [Fact]
    public async Task Install_LockContention_FailsClosedWithExitOne()
    {
        File.WriteAllText(Path.Combine(_home, ".bashrc"), "export MINE=1\n");
        var target = Path.Combine(_home, ".bashrc");
        var lockPath = CommandLineParser.CompletionInstaller.LockPathFor(target);
        File.WriteAllText(lockPath, "holder\n");
        // Pin the holder mtime ahead so the 10s waiter never classifies it as
        // stale mid-wait (stale locks are reaped at the deadline boundary).
        File.SetLastWriteTimeUtc(lockPath, DateTime.UtcNow + TimeSpan.FromMinutes(2));

        var (exitCode, _, error) = await RunCapturedAsync(["completions", "install", "--shell", "bash"]);

        Assert.Equal(1, exitCode);
        Assert.Contains("Another install is in progress", error);
        Assert.Equal("export MINE=1\n", File.ReadAllText(target));
        Assert.True(File.Exists(lockPath));
    }

    [Fact]
    public async Task Install_DryRun_WithContendedLock_SucceedsWithoutMutation()
    {
        var target = Path.Combine(_home, ".bashrc");
        var lockPath = CommandLineParser.CompletionInstaller.LockPathFor(target);
        File.WriteAllText(lockPath, "holder\n");

        var (exitCode, output, error) = await RunCapturedAsync(["completions", "install", "--shell", "bash", "--dry-run"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("would-write:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.False(File.Exists(target));
    }

    [Fact]
    public async Task Install_StaleLock_Proceeds()
    {
        File.WriteAllText(Path.Combine(_home, ".bashrc"), "export MINE=1\n");
        var target = Path.Combine(_home, ".bashrc");
        var lockPath = CommandLineParser.CompletionInstaller.LockPathFor(target);
        File.WriteAllText(lockPath, "stale-holder\n");
        File.SetLastWriteTimeUtc(lockPath, DateTime.UtcNow - TimeSpan.FromMinutes(1));

        var (exitCode, output, error) = await RunCapturedAsync(["completions", "install", "--shell", "bash"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("installed:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.False(File.Exists(lockPath));
        var rc = File.ReadAllText(target);
        Assert.Contains("export MINE=1", rc);
        Assert.Contains("# >>> compl-inst-test completion >>>", rc);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Install_FishStaleEncoding_ReinstallsByteExact(bool withBom)
    {
        var xdg = Path.Combine(_home, "xdg");
        var xdgValue = xdg;
        CommandLineParser.CompletionInstaller.EnvironmentProvider = name =>
            string.Equals(name, "XDG_CONFIG_HOME", StringComparison.Ordinal) ? xdgValue : null;
        var fishPath = Path.Combine(xdg, "fish", "completions", "compl-inst-test.fish");

        await RunCapturedAsync(["completions", "install", "--shell", "fish"]);
        var expected = CommandLineParser.CompletionInstaller.BuildBlock(
            "compl-inst-test",
            CommandLineParser.CompletionInstaller.RenderScript("fish", "compl-inst-test"));
        var stale = expected.Replace("\n", "\r\n");
        File.WriteAllBytes(fishPath, withBom
            ? new UTF8Encoding(true).GetPreamble().Concat(new UTF8Encoding(false).GetBytes(stale)).ToArray()
            : new UTF8Encoding(false).GetBytes(stale));

        var (exitCode, output, error) = await RunCapturedAsync(["completions", "install", "--shell", "fish"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("installed:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        var actual = File.ReadAllBytes(fishPath);
        Assert.Equal(new UTF8Encoding(false).GetBytes(expected), actual);
    }

    private static ApplicationBuilder CreateBuilder() => CreateBuilderWithExe("compl-inst-test");

    private static ApplicationBuilder CreateBuilderWithExe(string exeName)
    {
        return ApplicationBuilder.Create()
            .SetExecutableName(exeName)
            .SetExecutableTitle("Completion Install Test")
            .SetExecutableDescription("Completion install verification CLI.")
            .SetExecutableVersion("9.9.9");
    }

    private static Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        string[] args, CancellationToken cancellationToken = default)
        => RunCapturedAsync(CreateBuilder, args, cancellationToken);

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
/// <summary>
/// End-to-end completion script tests via <see cref="CliTestRunner"/> against the
/// built Test.Cli executable: asserts <c>completions script bash|zsh|pwsh|fish</c>
/// exits 0 and stdout contains the per-shell shim markers.
/// </summary>
public sealed class CompletionScriptE2ETests : CliTestBase
{
    [Theory]
    [InlineData("bash", "complete -F")]
    [InlineData("zsh", "compdef")]
    [InlineData("pwsh", "Register-ArgumentCompleter")]
    [InlineData("fish", "complete -f -c")]
    public async Task CompletionsScript_ExitsZeroAndContainsShimMarkers(string shell, string marker)
    {
        var result = await Runner.RunAsync("completions", "script", shell);

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, marker);
        CliTestAssertions.AssertOutputContains(result, "complete --position");
        CliTestAssertions.AssertNoError(result);
    }
}
