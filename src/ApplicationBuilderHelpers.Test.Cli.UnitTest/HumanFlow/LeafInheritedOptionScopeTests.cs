using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest.HumanFlow;

/// <summary>
/// Leaf option scope: <c>enum-test</c> and <c>enum-limited</c> extend
/// <c>Command</c> directly, so base-defined options (<c>--log-level</c>,
/// <c>--quiet</c>) are not bindable there and report <c>Unknown option</c>
/// (exit 2), while <c>BaseCommand</c> leaves (<c>test</c>) still accept them.
/// Parse, leaf help, and <c>complete</c> candidates stay consistent.
/// </summary>
public class LeafInheritedOptionScopeTests : CliTestBase
{
    [Theory]
    [InlineData("enum-test", "--log-level=debug", "--log-level")]
    [InlineData("enum-test", "--quiet", "--quiet")]
    [InlineData("enum-limited", "--log-level=debug", "--log-level")]
    [InlineData("enum-limited", "--quiet", "--quiet")]
    public async Task EnumLeaf_RejectsNonBindableBaseOption(string command, string option, string optionName)
    {
        var result = await Runner.RunAsync(command, option);

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, $"Unknown option: {optionName}");
    }

    [Fact]
    public async Task BaseLeaf_AcceptsBaseOptions()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--log-level", "debug", "--quiet");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: mytarget");
        CliTestAssertions.AssertNoError(result);
    }

    [Theory]
    [InlineData("enum-test", "--enum-level")]
    [InlineData("enum-limited", "--limited-level")]
    public async Task EnumLeaf_HelpHidesNonBindableOptions(string command, string ownOption)
    {
        var result = await Runner.RunAsync(command, "--help");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, ownOption);
        CliTestAssertions.AssertOutputContains(result, "GLOBAL OPTIONS:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "--log-level");
        CliTestAssertions.AssertOutputDoesNotContain(result, "--quiet");
    }

    [Fact]
    public async Task BaseLeaf_HelpShowsBaseOptions()
    {
        var result = await Runner.RunAsync("test", "--help");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "--log-level");
        CliTestAssertions.AssertOutputContains(result, "--quiet");
    }

    [Theory]
    [InlineData("enum-test", "--enum-level")]
    [InlineData("enum-limited", "--limited-level")]
    public async Task Completion_HidesNonBindableOptionsOnEnumLeaf(string command, string ownOption)
    {
        var line = $"test {command} --";
        var result = await Runner.RunAsync("complete", "--position", line.Length.ToString(), line);

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, ownOption);
        CliTestAssertions.AssertOutputContains(result, "--verbose");
        CliTestAssertions.AssertOutputDoesNotContain(result, "--log-level");
        CliTestAssertions.AssertOutputDoesNotContain(result, "--quiet");
        CliTestAssertions.AssertNoError(result);
    }

    [Fact]
    public async Task Completion_ShowsBaseOptionsOnBaseLeaf()
    {
        var line = "test test --";
        var result = await Runner.RunAsync("complete", "--position", line.Length.ToString(), line);

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "--log-level");
        CliTestAssertions.AssertOutputContains(result, "--quiet");
        CliTestAssertions.AssertNoError(result);
    }

    [Theory]
    [InlineData("enum-test", "EnumLevel: Information")]
    [InlineData("enum-limited", "LimitedLevel: Information")]
    public async Task EnumLeaf_BareRunSucceeds(string command, string defaultMarker)
    {
        var result = await Runner.RunAsync(command);

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "Target: default");
        CliTestAssertions.AssertOutputContains(result, defaultMarker);
        CliTestAssertions.AssertNoError(result);
    }
}
