using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for auto-detection of required properties using the C# required keyword
/// </summary>
public class AutoRequiredDetectionTests : CliTestBase
{
    [Fact]
    public async Task Required_Keyword_Properties_Should_Be_Auto_Detected_As_Required_Options()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject", "--phone", "123-456-7890");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -n, --name");
    }

    [Fact]
    public async Task Required_Keyword_Arguments_Should_Be_Auto_Detected_As_Required()
    {
        var result = await Runner.RunAsync("auto-required-test", "--name", "John", "--email", "john@example.com", "--phone", "123-456-7890");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required argument: target");
    }

    [Fact]
    public async Task Explicit_Required_Attribute_Should_Still_Work()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject", "--name", "John", "--email", "john@example.com");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -p, --phone");
    }

    [Fact]
    public async Task All_Required_Parameters_Provided_Should_Succeed()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject", 
            "--name", "John", "--email", "john@example.com", "--phone", "123-456-7890");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Auto Required Test Command Executed");
        CliTestAssertions.AssertOutputContains(result, "Target: mytarget");
        CliTestAssertions.AssertOutputContains(result, "Project: myproject");
        CliTestAssertions.AssertOutputContains(result, "Name: John");
        CliTestAssertions.AssertOutputContains(result, "Email: john@example.com");
        CliTestAssertions.AssertOutputContains(result, "Phone: 123-456-7890");
    }

    [Fact]
    public async Task Required_And_Optional_Parameters_Should_Work_Together()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject", "mysource",
            "--name", "Jane", "--email", "jane@example.com", "--phone", "987-654-3210", 
            "--age", "30", "--force");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Target: mytarget");
        CliTestAssertions.AssertOutputContains(result, "Project: myproject");
        CliTestAssertions.AssertOutputContains(result, "Source: mysource");
        CliTestAssertions.AssertOutputContains(result, "Name: Jane");
        CliTestAssertions.AssertOutputContains(result, "Email: jane@example.com");
        CliTestAssertions.AssertOutputContains(result, "Phone: 987-654-3210");
        CliTestAssertions.AssertOutputContains(result, "Age: 30");
        CliTestAssertions.AssertOutputContains(result, "Force: True");
    }

    [Fact]
    public async Task Required_Email_Option_Missing_Should_Fail()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject", 
            "--name", "John", "--phone", "123-456-7890");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -e, --email");
    }

    [Fact]
    public async Task Required_Project_Argument_Missing_Should_Fail()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget",
            "--name", "John", "--email", "john@example.com", "--phone", "123-456-7890");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required argument: project");
    }

    [Fact]
    public async Task Optional_Parameters_Should_Not_Be_Required()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject",
            "--name", "John", "--email", "john@example.com", "--phone", "123-456-7890");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Source: not provided");
        CliTestAssertions.AssertOutputContains(result, "Age: 0");
        CliTestAssertions.AssertOutputContains(result, "Force: False");
    }

    [Fact]
    public async Task Help_Should_Show_All_Required_Options()
    {
        var result = await Runner.RunAsync("auto-required-test", "--help");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Test command demonstrating automatic required detection");
        CliTestAssertions.AssertOutputContains(result, "--name");
        CliTestAssertions.AssertOutputContains(result, "--email");
        CliTestAssertions.AssertOutputContains(result, "--phone");
        CliTestAssertions.AssertOutputContains(result, "Name parameter (auto-detected as required)");
        CliTestAssertions.AssertOutputContains(result, "Email parameter (auto-detected as required)");
        CliTestAssertions.AssertOutputContains(result, "Phone parameter (explicitly required)");
    }

    [Fact]
    public async Task Short_Options_Work_With_Auto_Required_Detection()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject",
            "-n", "Bob", "-e", "bob@example.com", "-p", "555-1234");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Name: Bob");
        CliTestAssertions.AssertOutputContains(result, "Email: bob@example.com");
        CliTestAssertions.AssertOutputContains(result, "Phone: 555-1234");
    }

    [Fact]
    public async Task Mixed_Required_Detection_Methods_Should_Work()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject",
            "--name=Alice", "--email=alice@example.com", "--phone=999-8888", "--age", "25");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "=== Required via C# keyword ===");
        CliTestAssertions.AssertOutputContains(result, "Name: Alice");
        CliTestAssertions.AssertOutputContains(result, "Email: alice@example.com");
        CliTestAssertions.AssertOutputContains(result, "=== Required via attribute ===");
        CliTestAssertions.AssertOutputContains(result, "Phone: 999-8888");
        CliTestAssertions.AssertOutputContains(result, "=== Optional parameters ===");
        CliTestAssertions.AssertOutputContains(result, "Age: 25");
    }

    [Fact]
    public async Task Error_Messages_Should_Be_Clear_For_Auto_Required()
    {
        var result = await Runner.RunAsync("auto-required-test", "mytarget", "myproject", "--phone", "123-456-7890");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Missing required option: -n, --name");
    }
}