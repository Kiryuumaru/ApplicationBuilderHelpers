using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for reserved version-word value forms: <c>--version=&lt;anything&gt;</c>,
/// <c>-V=&lt;anything&gt;</c>, bare <c>--no-version</c>, and
/// <c>--no-version=&lt;anything&gt;</c> are usage errors (exit 2), never version.
/// Bare <c>--version</c>/<c>-V</c> still show version, <c>=</c>-less clusters,
/// post-separator tokens, space-separated words, and lookalike names keep
/// their existing meaning. A bare <c>--version</c> alongside a misuse form
/// still shows version, while a bare <c>--help</c> alongside version misuse
/// still reports the misuse. When both misuse forms appear, the first names
/// the error at every depth (concrete and abstract agree).
/// </summary>
public class VersionReservedValueTests : CliTestBase
{
    [Fact]
    public async Task LongVersionEquals_FalseLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'false' for option '--version'");
    }

    [Fact]
    public async Task LongVersionEquals_TrueLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=true");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'true' for option '--version'");
    }

    [Fact]
    public async Task LongVersionEquals_ArbitraryValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
        CliTestAssertions.AssertErrorContains(result, "for more information");
        CliTestAssertions.AssertErrorContains(result, "to show version information");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task LongVersionEquals_EmptyValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value '' for option '--version'");
    }

    [Fact]
    public async Task LongVersionEquals_UppercaseFalse_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=FALSE");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'FALSE' for option '--version'");
    }

    [Fact]
    public async Task LongVersionEquals_UppercaseTrue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=TRUE");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'TRUE' for option '--version'");
    }

    [Fact]
    public async Task ShortVersionEquals_FalseLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-V=false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'false' for option '-V'");
    }

    [Fact]
    public async Task ShortVersionEquals_TrueLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-V=true");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'true' for option '-V'");
    }

    [Fact]
    public async Task ShortVersionEquals_ArbitraryValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-V=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '-V'");
        CliTestAssertions.AssertErrorContains(result, "for more information");
        CliTestAssertions.AssertErrorContains(result, "to show version information");
    }

    [Fact]
    public async Task ShortVersionEquals_EmptyValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-V=");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value '' for option '-V'");
    }

    [Fact]
    public async Task NegatedVersion_Bare_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--no-version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Option '--no-version' is not valid");
    }

    [Fact]
    public async Task NegatedVersion_WithValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--no-version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "does not accept a value");
    }

    [Fact]
    public async Task NegatedVersion_WithFalseLiteral_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--no-version=false");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "does not accept a value");
    }

    [Fact]
    public async Task NegatedVersion_WithEmptyValue_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--no-version=");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "does not accept a value");
    }

    [Fact]
    public async Task Misuse_AfterValidOption_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--verbose", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
    }

    [Fact]
    public async Task Misuse_BeforeValidOption_IsInvalidValue()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=x", "--verbose");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
    }

    [Fact]
    public async Task UnknownOption_BeforeMisuse_ErrorsOnUnknown()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--nope", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --nope");
    }

    [Fact]
    public async Task Misuse_BeforeUnknownOption_ErrorsOnMisuse()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=x", "--nope");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
    }

    [Fact]
    public async Task CombinedShortCluster_WithVersion_ShowsVersion()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-vV");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task ShortVersionWithoutEquals_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-Vfalse");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: -f");
    }

    [Fact]
    public async Task Separator_TurnsMisuseForm_IntoPositional()
    {
        var result = await Runner.RunAsync("test", "--", "--version=x");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: --version=x");
    }

    [Fact]
    public async Task SpaceSeparatedValue_AfterVersion_ShowsVersion()
    {
        var result = await Runner.RunAsync("test", "--version", "x");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task BareLongVersion_ShowsVersion()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task BareShortVersion_ShowsVersion()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-V");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task UppercaseVersionPrefix_WithValue_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--VERSION=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --VERSION");
    }

    [Fact]
    public async Task MixedCaseVersionPrefix_WithValue_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--Version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --Version");
    }

    [Fact]
    public async Task VersionPrefixWord_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--versioned");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --versioned");
    }

    [Fact]
    public async Task VersionedWord_WithValue_IsUnknownOption()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--versioned=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --versioned");
    }

    [Fact]
    public async Task RootVersionEquals_IsInvalidValue()
    {
        var result = await Runner.RunAsync("--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
        CliTestAssertions.AssertErrorContains(result, "for more information on available commands and options");
        CliTestAssertions.AssertErrorContains(result, "to show version information");
    }

    [Fact]
    public async Task AbstractCommand_VersionEquals_IsInvalidValue()
    {
        var result = await Runner.RunAsync("config", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
        CliTestAssertions.AssertErrorContains(result, "for more information on specific command options");
        CliTestAssertions.AssertErrorContains(result, "to show version information");
    }

    [Fact]
    public async Task AbstractCommand_UnknownBeforeMisuse_ErrorsOnMisuse()
    {
        var result = await Runner.RunAsync("config", "--bogus", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
    }

    [Fact]
    public async Task ShortVersionEquals_WithServe_IsInvalidValue()
    {
        var result = await Runner.RunAsync("serve", "-V=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '-V'");
    }

    [Fact]
    public async Task Misuse_DoesNotSkipRequiredValidation()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
    }

    [Fact]
    public async Task BareVersion_WithMisuse_ShowsVersion()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version", "--version=x");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Misuse_WithBareVersion_ShowsVersion()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=x", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task HelpMisuse_WithBareVersion_ShowsVersion()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=x", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task BareHelp_WithVersionMisuse_ErrorsOnMisuse()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
        CliTestAssertions.AssertErrorContains(result, "to show version information");
    }

    [Fact]
    public async Task HelpMisuse_BeforeVersionMisuse_NamesHelp()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help=x", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--help'");
    }

    [Fact]
    public async Task VersionMisuse_BeforeHelpMisuse_NamesVersion()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version=x", "--help=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
    }

    [Fact]
    public async Task AbstractCommand_HelpMisuse_BeforeVersionMisuse_NamesHelp()
    {
        var result = await Runner.RunAsync("config", "--help=x", "--version=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--help'");
    }

    [Fact]
    public async Task AbstractCommand_VersionMisuse_BeforeHelpMisuse_NamesVersion()
    {
        var result = await Runner.RunAsync("config", "--version=x", "--help=x");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'x' for option '--version'");
    }
}
