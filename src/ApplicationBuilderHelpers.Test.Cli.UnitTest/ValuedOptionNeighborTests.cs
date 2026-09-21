using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for bare valued-option neighbor handling (reject-by-default): a bare
/// valued option never consumes a flag-looking neighbor. The neighbor binds or
/// errors on its own merits while the valued option falls back to the
/// trailing-bare missing sentinel (environment fallback or MissingRequired).
/// In-token forms (=-form, compact) and numeric neighbors still bind as values.
/// </summary>
public class ValuedOptionNeighborTests : CliTestBase
{
    [Fact]
    public async Task Valued_Option_Does_Not_Consume_Known_Flag_Neighbor()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: mytarget");
        CliTestAssertions.AssertOutputContains(result, "verbose=true");
        CliTestAssertions.AssertOutputDoesNotContain(result, "config=\"--verbose\"");
    }

    [Fact]
    public async Task Valued_Option_With_Unknown_Neighbor_Errors_On_Neighbor()
    {
        var result = await Runner.RunAsync("test", "target", "--filter", "--force");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option");
        CliTestAssertions.AssertOutputDoesNotContain(result, "filter=\"--force\"");
    }

    [Fact]
    public async Task Required_Valued_Option_With_Flag_Neighbor_Reports_Missing()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--email", "j@x.com", "--name", "--force");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -n, --name");
    }

    [Fact]
    public async Task Valued_Option_With_Help_Neighbor_Shows_Help()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "USAGE:");
    }

    [Fact]
    public async Task Valued_Option_With_Version_Neighbor_Shows_Version()
    {
        var result = await Runner.RunAsync("test", "mytarget", "--config", "--version");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputMatches(result, @"\d+\.\d+\.\d+");
        CliTestAssertions.AssertOutputDoesNotContain(result, "Running test on target");
    }

    [Fact]
    public async Task Valued_Option_With_Flag_Neighbor_Still_Uses_Environment_Fallback()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "env-config.json"
        };
        var result = await Runner.RunAsync(envVars, "test", "target", "--config", "--verbose");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: env-config.json");
    }

    [Fact]
    public async Task Valued_Option_With_Unknown_Neighbor_Still_Uses_Environment_Fallback_For_Error_Path()
    {
        var envVars = new Dictionary<string, string>
        {
            ["TEST_CONFIG"] = "env-config.json"
        };
        var result = await Runner.RunAsync(envVars, "test", "target", "--config", "--nope");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unknown option: --nope");
    }

    [Fact]
    public async Task Valued_Option_With_Numeric_Neighbor_Binds_Value()
    {
        var result = await Runner.RunAsync("test", "target", "--seed", "-5", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Random Seed: -5");
    }

    [Fact]
    public async Task Equals_Form_Value_May_Be_Flag_Looking()
    {
        var result = await Runner.RunAsync("test", "target", "--config=--verbose", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: --verbose");
    }

    [Fact]
    public async Task Compact_Form_Value_May_Be_Flag_Looking()
    {
        var result = await Runner.RunAsync("test", "target", "-c--verbose", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: --verbose");
    }

    [Fact]
    public async Task Separator_Blocks_Neighbor_Consumption()
    {
        var result = await Runner.RunAsync("test", "target", "--config", "--", "--verbose");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Unexpected argument '--verbose'");
    }

    [Fact]
    public async Task Collection_Option_With_Flag_Between_Occurrences_Binds_Both()
    {
        var result = await Runner.RunAsync("test", "target",
            "--tags", "unit",
            "--verbose",
            "--tags", "integration",
            "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Tags: unit, integration");
    }

    [Fact]
    public async Task Trailing_Bare_Required_Option_Reports_Missing()
    {
        var result = await Runner.RunAsync("required-test", "mytarget", "--name", "John", "--email");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -e, --email");
    }

    [Fact]
    public async Task Trailing_Bare_Optional_Option_Stays_Omitted()
    {
        var result = await Runner.RunAsync("test", "target", "--config");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Running test on target: target");
    }

    [Fact]
    public async Task Satisfied_Required_Option_With_Trailing_Bare_Repeat_Reports_Missing()
    {
        // #470: a satisfied required valued option repeated bare at
        // end-of-line errors exactly like the unsatisfied-then-bare ordering.
        var result = await Runner.RunAsync("required-test", "mytarget", "--name", "John", "--email", "j@x.com", "--name");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -n, --name");
    }

    [Fact]
    public async Task Satisfied_Optional_Option_With_Trailing_Bare_Repeat_Stays_Omitted()
    {
        // #470 scope guard: required-only. An optional satisfied-then-bare
        // repeat keeps the first value and succeeds.
        var result = await Runner.RunAsync("test", "target", "--config", "a.json", "--config");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "config=\"a.json\"");
    }
}
