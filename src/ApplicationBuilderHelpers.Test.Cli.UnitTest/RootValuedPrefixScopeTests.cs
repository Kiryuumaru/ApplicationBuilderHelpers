using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

[Collection("ConsoleDecoupling")]
public sealed class RootValuedPrefixScopeTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command(description: "Root owning a valued option no leaf binds.")]
    public sealed class PrefixRootCommand : Command
    {
        [CommandOption("config", Description = "Config file path.")]
        public string? Config { get; set; }

        [CommandOption('s', "secret", Description = "Secret value.", Secret = true)]
        public string? Secret { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"root:{Config ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("slow", "Slow leaf without the root valued option.")]
    public sealed class PrefixSlowCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("slow");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task PrefixEqualsForm_ReportsLeafScopedUnknownOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["--config=x", "slow"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --config", error);
        Assert.Contains("Run 'verify651 slow --help' for more information on specific command options.", error);
        Assert.Contains("Run 'verify651 --version' to show version information.", error);
    }

    [Fact]
    public async Task PrefixSpaceForm_ReportsLeafScopedUnknownOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["--config", "x", "slow"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --config", error);
        Assert.Contains("Run 'verify651 slow --help' for more information on specific command options.", error);
        Assert.Contains("Run 'verify651 --version' to show version information.", error);
    }

    [Fact]
    public async Task ResponseFileEqualsPrefix_ReportsLeafScopedUnknownOption()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "--config=x slow");
            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);
            Assert.Equal(2, exitCode);
            Assert.Contains("Unknown option: --config", error);
            Assert.Contains("Run 'verify651 slow --help' for more information on specific command options.", error);
            Assert.Contains("Run 'verify651 --version' to show version information.", error);
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task ResponseFileSpacePrefix_ReportsLeafScopedUnknownOption()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "--config x slow");
            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);
            Assert.Equal(2, exitCode);
            Assert.Contains("Unknown option: --config", error);
            Assert.Contains("Run 'verify651 slow --help' for more information on specific command options.", error);
            Assert.Contains("Run 'verify651 --version' to show version information.", error);
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task LeafFirstControl_ReportsIdenticalLeafScopedUnknownOption()
    {
        var (controlExit, controlOutput, controlError) = await RunCapturedAsync(["slow", "--config", "x"]);
        Assert.Equal(2, controlExit);
        Assert.Contains("Unknown option: --config", controlError);

        var (prefixExit, prefixOutput, prefixError) = await RunCapturedAsync(["--config=x", "slow"]);
        Assert.Equal(controlExit, prefixExit);
        Assert.Equal(controlError, prefixError);
    }

    [Fact]
    public async Task PrefixAttachedShortSecret_ReportsNameOnlyWithoutSecret()
    {
        const string secret = "Sup3rS3cret651Q";
        var (exitCode, output, error) = await RunCapturedAsync([$"-s{secret}", "slow"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: -s", error);
        Assert.DoesNotContain(secret, error);
        Assert.DoesNotContain($"-s{secret}", error);
        Assert.Contains("Run 'verify651 slow --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task LeafAttachedShortSecretEqualsForm_ReportsNameOnlyWithoutSecret()
    {
        const string secret = "Sup3rS3cret651M";
        var token = $"-s{secret}=extra";
        var (exitCode, output, error) = await RunCapturedAsync(["slow", token]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: -s", error);
        Assert.DoesNotContain(secret, error);
        Assert.DoesNotContain(token, error);
        Assert.Contains("Run 'verify651 slow --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task LeafAttachedShortSecret_ReportsNameOnlyWithoutSecret()
    {
        const string secret = "Sup3rS3cret651L";
        var (exitCode, output, error) = await RunCapturedAsync(["slow", $"-s{secret}"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: -s", error);
        Assert.DoesNotContain(secret, error);
        Assert.DoesNotContain($"-s{secret}", error);
        Assert.Contains("Run 'verify651 slow --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task LeafBareRun_StillSucceeds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["slow"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("slow", output);
    }

    [Fact]
    public async Task RootBareValuedRun_StillBindsRoot()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["--config", "root.json"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("root:root.json", output);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("verify651")
            .SetExecutableTitle("Verify 651")
            .SetExecutableDescription("Verify.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<PrefixRootCommand>()
            .AddCommand<PrefixSlowCommand>();
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rsp-651-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string WriteFile(string dir, string name, string content)
    {
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static void DeleteDir(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
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
