using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for reserved help-word value forms: <c>--help=&lt;anything&gt;</c>,
/// <c>-h=&lt;anything&gt;</c>, bare <c>--no-help</c>, and
/// <c>--no-help=&lt;anything&gt;</c> are usage errors (exit 2), never help.
/// Bare <c>--help</c>/<c>-h</c> still show help, <c>=</c>-less clusters,
/// post-separator tokens, space-separated words, and lookalike names keep
/// their existing meaning, and long-only options (e.g. <c>serve --host</c>)
/// never reclaim <c>-h=</c>-forms — those stay usage errors (exit 2) since
/// short <c>'h'</c> is reserved for help.
/// </summary>
public class HelpReservedValueTests : CliTestBase
{
    [Fact]
    public async Task LongHelpEquals_FalseLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'false' for option '--help'");
    }

    [Fact]
    public async Task LongHelpEquals_TrueLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=true");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'true' for option '--help'");
    }

    [Fact]
    public async Task LongHelpEquals_ArbitraryValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--help'");
    }

    [Fact]
    public async Task LongHelpEquals_EmptyValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value '' for option '--help'");
    }

    [Fact]
    public async Task LongHelpEquals_UppercaseFalse_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=False");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'False' for option '--help'");
    }

    [Fact]
    public async Task LongHelpEquals_UppercaseTrue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=TRUE");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'TRUE' for option '--help'");
    }

    [Fact]
    public async Task ShortHelpEquals_FalseLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-h=false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'false' for option '-h'");
    }

    [Fact]
    public async Task ShortHelpEquals_TrueLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-h=true");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'true' for option '-h'");
    }

    [Fact]
    public async Task ShortHelpEquals_ArbitraryValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-h=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '-h'");
    }

    [Fact]
    public async Task ShortHelpEquals_EmptyValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-h=");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value '' for option '-h'");
    }

    [Fact]
    public async Task NegatedHelp_Bare_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--no-help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Option '--no-help' is not valid");
    }

    [Fact]
    public async Task NegatedHelp_WithValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--no-help=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "does not accept a value");
    }

    [Fact]
    public async Task NegatedHelp_WithFalseLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--no-help=false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "does not accept a value");
    }

    [Fact]
    public async Task NegatedHelp_WithEmptyValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--no-help=");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "does not accept a value");
    }

    [Fact]
    public async Task Misuse_AfterValidOption_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--verbose", "--help=false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'false' for option '--help'");
    }

    [Fact]
    public async Task Misuse_BeforeValidOption_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=false", "--verbose");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'false' for option '--help'");
    }

    [Fact]
    public async Task UnknownOption_BeforeMisuse_ErrorsOnUnknown()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--nope", "--help=false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --nope");
    }

    [Fact]
    public async Task Misuse_BeforeUnknownOption_ErrorsOnMisuse()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=false", "--nope");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'false' for option '--help'");
    }

    [Fact]
    public async Task CombinedShortCluster_WithHelp_ShowsHelp()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-vh");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task ShortHelpWithoutEquals_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-hfalse");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: -hfalse");
    }

    [Fact]
    public async Task Separator_TurnsMisuseForm_IntoPositional()
    {
        var result = await Runner.RunAsync("test", "--", "--help=false");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: --help=false");
    }

    [Fact]
    public async Task SpaceSeparatedValue_AfterHelp_ShowsHelp()
    {
        var result = await Runner.RunAsync("test", "--help", "false");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task BareLongHelp_ShowsHelp()
    {
        var result = await Runner.RunAsync("test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
    }

    [Fact]
    public async Task BareShortHelp_ShowsHelp()
    {
        var result = await Runner.RunAsync("test", "-h");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "Run various test operations");
    }

    [Fact]
    public async Task UppercaseHelpPrefix_WithValue_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--HELP=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --HELP");
    }

    [Fact]
    public async Task HelpPrefixWord_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--helpful");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --helpful");
    }

    [Fact]
    public async Task HelperWord_WithValue_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--helper=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --helper");
    }

    [Fact]
    public async Task ShortHelpEquals_WithLongOnlyHost_IsInvalidValue()
    {
        var result = await Runner.RunAsync("serve", "-h=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '-h'");
    }

    [Fact]
    public async Task AbstractCommand_HelpEquals_IsInvalidValue()
    {
        var result = await Runner.RunAsync("config", "--help=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--help'");
    }
}
