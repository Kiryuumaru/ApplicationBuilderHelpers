using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest.HumanFlow;

/// <summary>
/// Root-valued option scope: the concrete root (<c>MainCommand</c>) and
/// <c>test</c> both declare <c>--config</c>/<c>-c</c>, while
/// <c>enum-test</c> and <c>enum-limited</c> extend <c>Command</c> directly
/// and do not. A root-first valued option, flag, or short form ahead of a
/// non-owning leaf stays <c>Unknown option</c> (exit 2) with the leaf-scoped
/// dual footer and empty stdout; response-file expansion, leaf-first order,
/// and trailing help/version keep the same verdict. Owning leaves, the root
/// itself, and global options still bind (exit 0).
/// </summary>
public class RootValuedOptionLeafScopeTests : CliTestBase
{
    [Theory]
    [InlineData("--config=x.json", "enum-test")]
    [InlineData("--config", "enum-test")]
    [InlineData("--config=x.json", "enum-limited")]
    public async Task RootValuedOption_BeforeNonOwningLeaf_ReportsUnknownOption(string option, string command)
    {
        var args = option == "--config"
            ? new[] { "--config", "x.json", command }
            : new[] { option, command };
        var result = await Runner.RunAsync(args);

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --config");
        CliTestAssertions.AssertErrorContains(result, $"Run 'test {command} --help'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
    }

    [Fact]
    public async Task RootValuedOption_ResponseFileBeforeNonOwningLeaf_ReportsUnknownOption()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rsp-scope-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var file = Path.Combine(dir, "args.rsp");
            await File.WriteAllTextAsync(file, "--config=x.json enum-test");

            var result = await Runner.RunAsync("@" + file);

            CliTestAssertions.AssertFailure(result);
            CliTestAssertions.AssertExitCode(result, 2);
            CliTestAssertions.AssertErrorContains(result, "Unknown option: --config");
            CliTestAssertions.AssertErrorContains(result, "Run 'test enum-test --help'");
            CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
            Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public async Task NonOwningLeaf_AfterCommandWithValuedOption_ReportsUnknownOption()
    {
        var result = await Runner.RunAsync("enum-test", "--config=x.json");

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --config");
        CliTestAssertions.AssertErrorContains(result, "Run 'test enum-test --help'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
    }

    [Fact]
    public async Task ShortForm_BeforeNonOwningLeaf_ReportsUnknownWithoutValue()
    {
        var result = await Runner.RunAsync("-c", "x.json", "enum-test");

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: -c");
        CliTestAssertions.AssertErrorContains(result, "Run 'test enum-test --help'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
        Assert.DoesNotContain("x.json", result.StandardError, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
    }

    [Fact]
    public async Task AttachedShortForm_BeforeNonOwningLeaf_ReportsNameOnlyWithoutValue()
    {
        const string secret = "SuperSecretZ9";
        var result = await Runner.RunAsync($"-c{secret}", "enum-test");

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: -c");
        Assert.DoesNotContain(secret, result.StandardError, StringComparison.Ordinal);
        Assert.DoesNotContain($"-c{secret}", result.StandardError, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
    }

    [Fact]
    public async Task FlagOption_BeforeNonOwningLeaf_ReportsUnknownOption()
    {
        var result = await Runner.RunAsync("--quiet", "enum-test");

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --quiet");
        CliTestAssertions.AssertErrorContains(result, "Run 'test enum-test --help'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
    }

    [Fact]
    public async Task TrailingHelp_DoesNotForgiveUnknownOption()
    {
        var result = await Runner.RunAsync("--config=x.json", "enum-test", "--help");

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --config");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
        Assert.DoesNotContain("enum-test --help", result.StandardError, StringComparison.Ordinal);
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
    }

    [Fact]
    public async Task TrailingVersion_DoesNotForgiveUnknownOption()
    {
        var result = await Runner.RunAsync("--config=x.json", "enum-test", "--version");

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --config");
        CliTestAssertions.AssertErrorContains(result, "Run 'test enum-test --help'");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version'");
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
    }

    [Fact]
    public async Task GlobalOption_BindsOnNonOwningLeaf()
    {
        var result = await Runner.RunAsync("enum-test", "--help");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertNoError(result);
    }

    [Fact]
    public async Task RootValuedOption_BeforeOwningLeaf_Binds()
    {
        var result = await Runner.RunAsync("--config=x.json", "test", "mytarget");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: mytarget");
        CliTestAssertions.AssertOutputContains(result, "config=\"x.json\"");
        CliTestAssertions.AssertNoError(result);
    }

    [Fact]
    public async Task OwningLeaf_AcceptsValuedOptionAfterCommand()
    {
        var result = await Runner.RunAsync("test", "--config=x.json", "mytarget");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: mytarget");
        CliTestAssertions.AssertOutputContains(result, "config=\"x.json\"");
        CliTestAssertions.AssertNoError(result);
    }

    [Fact]
    public async Task RootValuedOption_AtRoot_Binds()
    {
        var result = await Runner.RunAsync("--config=x.json");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "Default Command");
        CliTestAssertions.AssertNoError(result);
    }

    [Fact]
    public async Task BareLeaf_RunsSuccessfully()
    {
        var result = await Runner.RunAsync("enum-test");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "EnumTest Command executed successfully!");
        CliTestAssertions.AssertNoError(result);
    }
}
