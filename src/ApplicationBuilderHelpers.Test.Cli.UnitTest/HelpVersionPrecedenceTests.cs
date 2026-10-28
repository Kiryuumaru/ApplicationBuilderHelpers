using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for help/version flag precedence: a bare optional valued option never
/// consumes a flag-looking neighbor, so the neighbor binds or
/// errors on its own merits while the valued option falls back to the
/// trailing-bare missing sentinel (env fallback or MissingRequired); a bare
/// required valued option consumes the neighbor as its value instead, help
/// never masks path errors, and a version flag beside help forgives the
/// dangling-value error so the command runs with the option unset.
/// A typo on the abstract <c>config</c> hub that precedes the help/version tail
/// stays a <c>RequiresSubcommand</c> usage error instead of taking the version
/// carve-out: both token orders, short and alias forms, help clusters, a single
/// help token before the typo, and valued-option variants all follow the same
/// guard. Version-only, help-only, post-separator, and leaf paths keep their
/// existing outcomes.
/// </summary>
public class HelpVersionPrecedenceTests : CliTestBase
{
    [Fact]
    public async Task Version_Flag_As_Option_Value_Is_Not_Consumed()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Version_Forgives_Missing_Value_Beside_Help()
    {
        var result = await Runner.RunAsync("test", "--config", "--help", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "Running test on target");
        CliTestAssertions.AssertOutputDoesNotContain(result, "config=");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertNoError(result);
    }

    [Fact]
    public async Task Version_Forgives_Missing_Value_Beside_Help_Alias()
    {
        var result = await Runner.RunAsync("test", "--config", "/?", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "Running test on target");
        CliTestAssertions.AssertOutputDoesNotContain(result, "config=");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertNoError(result);
    }

    [Fact]
    public async Task Known_Flag_Neighbor_Reports_Missing_Value()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--verbose");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing value for option: -c, --config");
        CliTestAssertions.AssertOutputDoesNotContain(result, "config=\"--verbose\"");
    }

    [Fact]
    public async Task Unknown_Flag_Neighbor_Errors_On_Merits()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--nope");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --nope");
    }

    [Fact]
    public async Task Required_Valued_Option_Consumes_Flag_Neighbor_As_Value()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--name", "--force", "--email", "e@x.com");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "Name: --force");
        CliTestAssertions.AssertOutputContains(result, "Force: False");
        CliTestAssertions.AssertNoError(result);
    }

    [Fact]
    public async Task Command_Help_Takes_Command_Path()
    {
        var result = await Runner.RunAsync("deploy", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Deploy applications to various environments");
    }

    [Fact]
    public async Task Help_Does_Not_Mask_Unknown_Option_Error()
    {
        var result = await Runner.RunAsync("build", "MyProject.csproj", "--unknown-option", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --unknown-option");
    }

    [Fact]
    public async Task Help_Does_Not_Mask_Unknown_Command_Error()
    {
        var result = await Runner.RunAsync("deply", "--help");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "No command found");
    }

    [Fact]
    public async Task Help_Skips_Required_Validation()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Help_Skips_Required_Argument_Validation()
    {
        var result = await Runner.RunAsync("required-test", "--name", "John", "--email", "john@example.com", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Help_Skips_All_Required_Validation()
    {
        var result = await Runner.RunAsync("required-test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Short_Help_Skips_Required_Validation()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "-h");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Version_Skips_Required_Validation()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
    }

    [Fact]
    public async Task Help_Takes_Precedence_Over_Version()
    {
        var result = await Runner.RunAsync("--version", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"^\d+\.\d+\.\d+", result.StandardOutput.TrimStart());
    }

    [Fact]
    public async Task Command_Help_Takes_Precedence_Over_Version()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Command_Help_Before_Version_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--help", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Command_Help_Version_Cluster_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "mytarget", "-hV");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Group_Unknown_Option_Before_Version_Reports_Unknown()
    {
        var result = await Runner.RunAsync("config", "--bogus", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Version_Before_Unknown_Option_Reports_Unknown()
    {
        var result = await Runner.RunAsync("config", "--version", "--bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Command_Unknown_Option_Before_Version_Reports_Unknown()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--bogus", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Command_Version_Before_Unknown_Option_Reports_Unknown()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version", "--bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Group_Separator_Before_Unknown_With_Version_Keeps_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config", "--", "--bogus", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Unknown_Before_Separator_With_Version_Reports_Unknown()
    {
        var result = await Runner.RunAsync("config", "--bogus", "--", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --bogus");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Version_Before_Separator_With_Unknown_Shows_Version()
    {
        var result = await Runner.RunAsync("config", "--version", "--", "--bogus");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task Group_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("config", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
    }

    [Fact]
    public async Task Command_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Group_Invalid_Flag_Literal_Before_Version_Reports_Invalid()
    {
        var result = await Runner.RunAsync("config", "--quiet=banana", "--version");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'banana' for option '--quiet'");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Version_Before_Invalid_Flag_Literal_Reports_Invalid()
    {
        var result = await Runner.RunAsync("config", "--version", "--quiet=banana");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value 'banana' for option '--quiet'");
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Theory]
    [InlineData("--help", "--version")]
    [InlineData("--version", "--help")]
    [InlineData("-h", "-V")]
    [InlineData("-V", "-h")]
    [InlineData("--help", "-V")]
    [InlineData("-V", "--help")]
    [InlineData("-h", "--version")]
    [InlineData("--version", "-h")]
    public async Task Group_Typo_With_Help_And_Version_Requires_Subcommand(string first, string second)
    {
        var expected = await Runner.RunAsync("config", "bogus", "--help");
        var result = await Runner.RunAsync("config", "bogus", first, second);

        CliTestAssertions.AssertExitCode(expected, 2);
        AssertSameResult(expected, result);
        AssertRequiresSubcommandError(result);
    }

    [Theory]
    [InlineData("-?")]
    [InlineData("/?")]
    [InlineData("-hV")]
    public async Task Group_Typo_With_Help_Alias_And_Version_Requires_Subcommand(string helpToken)
    {
        var expected = await Runner.RunAsync("config", "bogus", "--help");
        var result = await Runner.RunAsync("config", "bogus", helpToken, "--version");

        CliTestAssertions.AssertExitCode(expected, 2);
        AssertSameResult(expected, result);
        AssertRequiresSubcommandError(result);
    }

    [Theory]
    [InlineData("-hV")]
    [InlineData("-Vh")]
    public async Task Group_Help_Cluster_With_Version_Requires_Subcommand(string cluster)
    {
        var expected = await Runner.RunAsync("config", cluster);
        var result = await Runner.RunAsync("config", cluster, "--version");

        CliTestAssertions.AssertExitCode(expected, 2);
        AssertSameResult(expected, result);
        AssertRequiresSubcommandError(result);
    }

    [Fact]
    public async Task Group_Help_Before_Typo_With_Version_Requires_Subcommand()
    {
        var expected = await Runner.RunAsync("config", "--help", "bogus");
        var result = await Runner.RunAsync("config", "--help", "bogus", "--version");

        CliTestAssertions.AssertExitCode(expected, 2);
        AssertSameResult(expected, result);
        AssertRequiresSubcommandError(result);
    }

    [Fact]
    public async Task Group_Valued_Option_Then_Typo_With_Help_And_Version_Requires_Subcommand()
    {
        var expected = await Runner.RunAsync("config", "--format", "json", "bogus", "--help");
        var result = await Runner.RunAsync("config", "--format", "json", "bogus", "--help", "--version");

        CliTestAssertions.AssertExitCode(expected, 2);
        AssertSameResult(expected, result);
        AssertRequiresSubcommandError(result);
    }

    [Fact]
    public async Task Group_Valued_Option_Then_Help_And_Typo_With_Version_Requires_Subcommand()
    {
        var expected = await Runner.RunAsync("config", "--format", "json", "--help", "bogus");
        var result = await Runner.RunAsync("config", "--format", "json", "--help", "bogus", "--version");

        CliTestAssertions.AssertExitCode(expected, 2);
        AssertSameResult(expected, result);
        AssertRequiresSubcommandError(result);
    }

    [Fact]
    public async Task Group_Typo_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config", "bogus");

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --help' to see available subcommands and options.");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version' to show version information.");
        AssertNoRuntimeFault(result);
        AssertNoHelpOrVersionOutput(result);
    }

    [Fact]
    public async Task Group_Typo_With_Help_Requires_Subcommand()
    {
        var result = await Runner.RunAsync("config", "bogus", "--help");

        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        CliTestAssertions.AssertErrorContains(result, "Run 'test --version' to show version information.");
        Assert.DoesNotContain("Run 'test config --help'", result.StandardError);
        AssertNoRuntimeFault(result);
        AssertNoHelpOrVersionOutput(result);
    }

    [Fact]
    public async Task Group_Typo_With_Version_Shows_Version()
    {
        var result = await Runner.RunAsync("config", "bogus", "--version");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertOutputDoesNotContain(result, "requires a subcommand");
        CliTestAssertions.AssertNoError(result);
    }

    [Theory]
    [InlineData("--help", "--version")]
    [InlineData("--version", "--help")]
    public async Task Group_Help_And_Version_Shows_Help(string first, string second)
    {
        var expected = await Runner.RunAsync("config", "--help");
        var result = await Runner.RunAsync("config", first, second);

        CliTestAssertions.AssertSuccess(expected);
        AssertSameResult(expected, result);
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "test config");
        CliTestAssertions.AssertNoError(result);
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Leaf_Help_And_Version_Shows_Leaf_Help()
    {
        var expected = await Runner.RunAsync("config", "get", "--help");
        var result = await Runner.RunAsync("config", "get", "--help", "--version");

        CliTestAssertions.AssertSuccess(expected);
        AssertSameResult(expected, result);
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
        CliTestAssertions.AssertOutputContains(result, "test config get");
        CliTestAssertions.AssertNoError(result);
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }

    [Fact]
    public async Task Group_Typo_After_Separator_With_Help_And_Version_Requires_Subcommand()
    {
        var expected = await Runner.RunAsync("config", "bogus");
        var result = await Runner.RunAsync("config", "bogus", "--", "--help", "--version");

        CliTestAssertions.AssertExitCode(expected, 2);
        AssertSameResult(expected, result);
        AssertRequiresSubcommandError(result);
        CliTestAssertions.AssertErrorContains(result, "Run 'test config --help' to see available subcommands and options.");
    }

    [Fact]
    public async Task Group_Version_Before_Separator_Shows_Version()
    {
        var expected = await Runner.RunAsync("config", "bogus", "--version");
        var result = await Runner.RunAsync("config", "bogus", "--version", "--", "--help");

        CliTestAssertions.AssertSuccess(expected);
        AssertSameResult(expected, result);
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExitCode(result, 0);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "USAGE:");
        CliTestAssertions.AssertNoError(result);
    }

    private static void AssertSameResult(CliTestResult expected, CliTestResult result)
    {
        Assert.Equal(expected.ExitCode, result.ExitCode);
        Assert.Equal(expected.StandardOutput, result.StandardOutput);
        Assert.Equal(expected.StandardError, result.StandardError);
    }

    private static void AssertRequiresSubcommandError(CliTestResult result)
    {
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "'config' requires a subcommand");
        CliTestAssertions.AssertErrorContains(result, "Available subcommands: get, set");
        AssertNoRuntimeFault(result);
        AssertNoHelpOrVersionOutput(result);
    }

    private static void AssertNoRuntimeFault(CliTestResult result)
    {
        Assert.DoesNotContain("Object reference not set", result.CombinedOutput);
        Assert.DoesNotContain("Non-static method requires a target", result.CombinedOutput);
        Assert.DoesNotContain("TargetException", result.CombinedOutput);
        Assert.DoesNotContain("Fault", result.CombinedOutput);
        Assert.DoesNotContain("Run 'test --help' for more information on available commands and options.", result.CombinedOutput);
    }

    private static void AssertNoHelpOrVersionOutput(CliTestResult result)
    {
        Assert.True(string.IsNullOrWhiteSpace(result.StandardOutput), $"Expected empty stdout but got: {result.StandardOutput}");
        Assert.DoesNotContain("USAGE:", result.StandardOutput);
        Assert.DoesNotMatch(@"(?m)^\d+\.\d+\.\d+", result.StandardOutput);
    }
}
