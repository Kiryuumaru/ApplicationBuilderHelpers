using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests that missing-required and invalid-value failures aggregate instead of
/// the first failure masking the other.
/// </summary>
public class ValidationAggregationTests : CliTestBase
{
    [Fact]
    public async Task Missing_Argument_And_Invalid_Value_Report_Together()
    {
        var result = await Runner.RunAsync("build", "--verbosity", "maybe");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required argument: project");
        CliTestAssertions.AssertErrorContains(result, "Value 'maybe' is not valid for option '--verbosity'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of: quiet, minimal, normal, detailed, diagnostic");
    }

    [Fact]
    public async Task Missing_Argument_Orders_Before_Invalid_Value()
    {
        var result = await Runner.RunAsync("build", "--verbosity", "maybe");
        CliTestAssertions.AssertFailure(result);
        var missingAt = result.StandardError.IndexOf("Missing required argument: project", StringComparison.OrdinalIgnoreCase);
        var invalidAt = result.StandardError.IndexOf("not valid for option '--verbosity'", StringComparison.OrdinalIgnoreCase);
        Assert.True(missingAt >= 0, "Expected missing-argument error in stderr.");
        Assert.True(invalidAt >= 0, "Expected invalid-value error in stderr.");
        Assert.True(missingAt < invalidAt, "Expected missing error to order before invalid error.");
    }

    [Fact]
    public async Task Invalid_Verbosity_Alone_Reports_Single_Error()
    {
        var result = await Runner.RunAsync("build", "my.csproj", "--verbosity", "maybe");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Value 'maybe' is not valid for option '--verbosity'");
        CliTestAssertions.AssertErrorContains(result, "Must be one of: quiet, minimal, normal, detailed, diagnostic");
        Assert.DoesNotContain("Missing required argument", result.StandardError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Missing_Project_Alone_Reports_Single_Error()
    {
        var result = await Runner.RunAsync("build");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required argument: project");
        Assert.DoesNotContain("not valid for option", result.StandardError, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Multiple_Missing_Parameters_Report_Together()
    {
        var result = await Runner.RunAsync("required-test");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -n, --name");
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -e, --email");
        CliTestAssertions.AssertErrorContains(result, "Missing required argument: target");
    }

    [Fact]
    public async Task Multiple_Invalid_Values_Report_Together()
    {
        var result = await Runner.RunAsync("build", "my.csproj", "--verbosity", "maybe", "--target", "Bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Value 'maybe' is not valid for option '--verbosity'");
        CliTestAssertions.AssertErrorContains(result, "Value 'Bogus' is not valid for option '--target'");
        var targetAt = result.StandardError.IndexOf("not valid for option '--target'", StringComparison.OrdinalIgnoreCase);
        var verbosityAt = result.StandardError.IndexOf("not valid for option '--verbosity'", StringComparison.OrdinalIgnoreCase);
        Assert.True(targetAt >= 0, "Expected invalid-target error in stderr.");
        Assert.True(verbosityAt >= 0, "Expected invalid-verbosity error in stderr.");
        Assert.True(targetAt < verbosityAt, "Expected canonical-key order: target before verbosity.");
    }

    [Fact]
    public async Task Missing_Argument_And_Multiple_Invalid_Values_Report_Together()
    {
        var result = await Runner.RunAsync("build", "--verbosity", "maybe", "--target", "Bogus");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertExitCode(result, 2);
        CliTestAssertions.AssertErrorContains(result, "Missing required argument: project");
        CliTestAssertions.AssertErrorContains(result, "Value 'maybe' is not valid for option '--verbosity'");
        CliTestAssertions.AssertErrorContains(result, "Value 'Bogus' is not valid for option '--target'");
        var missingAt = result.StandardError.IndexOf("Missing required argument: project", StringComparison.OrdinalIgnoreCase);
        var firstInvalidAt = result.StandardError.IndexOf("not valid for option", StringComparison.OrdinalIgnoreCase);
        Assert.True(missingAt >= 0, "Expected missing-argument error in stderr.");
        Assert.True(firstInvalidAt >= 0, "Expected invalid-value error in stderr.");
        Assert.True(missingAt < firstInvalidAt, "Expected missing error to order before invalid errors.");
    }
}
