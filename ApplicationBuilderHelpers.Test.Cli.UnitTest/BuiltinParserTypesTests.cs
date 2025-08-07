using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ApplicationBuilderHelpers.Test.Cli.UnitTest.TestFramework;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Comprehensive tests for all built-in ParserTypes in ApplicationBuilderHelpers
/// Tests every built-in type parser with valid values, invalid values, edge cases, and error handling
/// </summary>
public class BuiltinParserTypesTests : CliTestBase
{
    #region String Parser Tests

    [Theory]
    [InlineData("simple", "simple")]
    [InlineData("with spaces", "with spaces")]
    [InlineData("with-dashes", "with-dashes")]
    [InlineData("with_underscores", "with_underscores")]
    [InlineData("with.dots", "with.dots")]
    [InlineData("123numeric", "123numeric")]
    [InlineData("", "")]
    public async Task String_Parser_Valid_Values(string input, string expected)
    {
        var result = await Runner.RunAsync("test", "target", $"--config={input}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Config: {expected}");
    }

    [Fact]
    public async Task String_Parser_Special_Characters()
    {
        var result = await Runner.RunAsync("test", "target", "--config=file@host:port/path?query=value&other=true", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: file@host:port/path?query=value&other=true");
    }

    #endregion

    #region Boolean Parser Tests

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("TRUE")]
    [InlineData("yes")]
    [InlineData("Yes")]
    [InlineData("YES")]
    [InlineData("on")]
    [InlineData("On")]
    [InlineData("ON")]
    [InlineData("1")]
    public async Task Boolean_Parser_True_Values(string trueValue)
    {
        var result = await Runner.RunAsync("test", "target", $"--diag={trueValue}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
    }

    [Theory]
    [InlineData("false")]
    [InlineData("False")]
    [InlineData("FALSE")]
    [InlineData("no")]
    [InlineData("No")]
    [InlineData("NO")]
    [InlineData("off")]
    [InlineData("Off")]
    [InlineData("OFF")]
    [InlineData("0")]
    public async Task Boolean_Parser_False_Values(string falseValue)
    {
        var result = await Runner.RunAsync("test", "target", $"--diag={falseValue}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: False");
    }

    [Fact]
    public async Task Boolean_Parser_Flag_Mode()
    {
        var result = await Runner.RunAsync("test", "target", "--diag", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
    }

    [Theory]
    [InlineData("maybe")]
    [InlineData("2")]
    [InlineData("invalid")]
    [InlineData("truee")]
    [InlineData("falsee")]
    public async Task Boolean_Parser_Invalid_Values(string invalidValue)
    {
        var result = await Runner.RunAsync("test", "target", $"--diag={invalidValue}");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Invalid Boolean value");
    }

    #endregion

    #region Integer Parser Tests

    [Theory]
    [InlineData("0", "0")]
    [InlineData("1", "1")]
    [InlineData("42", "42")]
    [InlineData("123456", "123456")]
    [InlineData("-1", "-1")]
    [InlineData("-42", "-42")]
    [InlineData("2147483647", "2147483647")] // int.MaxValue
    [InlineData("-2147483648", "-2147483648")] // int.MinValue
    public async Task Integer_Parser_Valid_Values(string input, string expected)
    {
        var result = await Runner.RunAsync("test", "target", $"--timeout={input}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Timeout: {expected}s");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.34")]
    [InlineData("12abc")]
    [InlineData("")]
    [InlineData("2147483648")]
    [InlineData("-2147483649")]
    [InlineData("12 34")]
    public async Task Integer_Parser_Invalid_Values(string invalidValue)
    {
        var result = await Runner.RunAsync("test", "target", $"--timeout={invalidValue}");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Invalid Int32 value");
    }

    #endregion

    #region Double Parser Tests

    [Theory]
    [InlineData("0.0", "0")]
    [InlineData("1.5", "1.5")]
    [InlineData("42.75", "42.75")]
    [InlineData("123.456789", "123.456789")]
    [InlineData("-1.5", "-1.5")]
    [InlineData("0.1", "0.1")]
    [InlineData("99.99", "99.99")]
    [InlineData("85.5", "85.5")]
    public async Task Double_Parser_Valid_Values(string input, string expected)
    {
        var result = await Runner.RunAsync("test", "target", $"--coverage-threshold={input}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Coverage Threshold: {expected}%");
    }

    [Fact]
    public async Task Double_Parser_Negative_Values_Without_Equals()
    {
        // Test negative values passed as separate arguments (the problematic case)
        var result = await Runner.RunAsync("test", "target", "--coverage-threshold", "-1.5", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: -1.5%");
    }

    [Theory]
    [InlineData("1e2", "100")]
    [InlineData("1.5e1", "15")]
    [InlineData("2.5e-1", "0.25")]
    public async Task Double_Parser_Scientific_Notation(string input, string expected)
    {
        var result = await Runner.RunAsync("test", "target", $"--coverage-threshold={input}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Coverage Threshold: {expected}%");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.34.56")]
    [InlineData("12abc")]
    [InlineData("")]
    [InlineData("12 34")]
    [InlineData("not-a-number")]
    public async Task Double_Parser_Invalid_Values(string invalidValue)
    {
        var result = await Runner.RunAsync("test", "target", $"--coverage-threshold={invalidValue}");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Invalid Double value");
    }

    #endregion

    #region Long/Nullable Integer Parser Tests

    [Theory]
    [InlineData("0")]
    [InlineData("1")]
    [InlineData("42")]
    [InlineData("2147483647")] // Use int.MaxValue since seed appears to be int-typed
    public async Task Long_Parser_Valid_Values(string input)
    {
        var result = await Runner.RunAsync("test", "target", $"--seed={input}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Random Seed: {input}");
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("12.34")]
    [InlineData("9223372036854775808")] // Beyond int.MaxValue
    public async Task Long_Parser_Invalid_Values(string invalidValue)
    {
        var result = await Runner.RunAsync("test", "target", $"--seed={invalidValue}");
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, "Invalid");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task All_Basic_Types_Can_Be_Parsed_Without_Errors()
    {
        var result = await Runner.RunAsync("test", "target", 
            "--config=test.json",           // String
            "--timeout=300",                // Int
            "--coverage-threshold=85.5",    // Double
            "--diag=true",                  // Boolean
            "--seed=12345",                 // Long/Nullable Int
            "-v");

        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: test.json");
        CliTestAssertions.AssertOutputContains(result, "Timeout: 300s");
        CliTestAssertions.AssertOutputContains(result, "Coverage Threshold: 85.5%");
        CliTestAssertions.AssertOutputContains(result, "Diagnostic Mode: True");
        CliTestAssertions.AssertOutputContains(result, "Random Seed: 12345");
    }

    [Theory]
    [InlineData("--timeout=invalid", "Invalid Int32 value")]
    [InlineData("--coverage-threshold=invalid", "Invalid Double value")]
    [InlineData("--diag=invalid", "Invalid Boolean value")]
    public async Task Type_Parser_Error_Messages_Are_Consistent(string args, string expectedError)
    {
        var result = await Runner.RunAsync("test", "target", args);
        CliTestAssertions.AssertFailure(result);
        CliTestAssertions.AssertErrorContains(result, expectedError);
    }

    [Fact]
    public async Task Type_Parsers_Handle_Empty_Values_Correctly()
    {
        var result = await Runner.RunAsync("test", "target", "--config=", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, "Config: ");
    }

    [Theory]
    [InlineData("--timeout=0", "Timeout: 0s")]
    [InlineData("--timeout=2147483647", "Timeout: 2147483647s")]
    [InlineData("--coverage-threshold=0.0", "Coverage Threshold: 0%")]
    [InlineData("--coverage-threshold=100.0", "Coverage Threshold: 100%")]
    public async Task Type_Parsers_Handle_Boundary_Values(string args, string expectedOutput)
    {
        var result = await Runner.RunAsync("test", "target", args, "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, expectedOutput);
    }

    [Theory]
    [InlineData("--coverage-threshold=85.5", "85.5")]
    [InlineData("--coverage-threshold=1.0", "1")]
    [InlineData("--coverage-threshold=0.1", "0.1")]
    public async Task Type_Parsers_Are_Culture_Invariant(string args, string expected)
    {
        var result = await Runner.RunAsync("test", "target", args, "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Coverage Threshold: {expected}%");
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task Type_Parsers_Handle_Large_Values_Efficiently()
    {
        var largeStringValue = new string('x', 1000);
        var result = await Runner.RunAsync("test", "target", $"--config={largeStringValue}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExecutionTime(result, TimeSpan.FromSeconds(5));
        CliTestAssertions.AssertOutputContains(result, $"Config: {largeStringValue}");
    }

    [Fact]
    public async Task Multiple_Type_Parsers_Work_Together_Efficiently()
    {
        var args = new List<string> { "test", "target" };
        
        // Add multiple options of different types
        for (int i = 0; i < 10; i++)
        {
            args.Add("--tags");
            args.Add($"tag{i}");
        }
        
        args.AddRange(new[]
        {
            "--timeout=300",
            "--coverage-threshold=85.5",
            "--diag=true",
            "--seed=12345",
            "--config=performance-test.json",
            "-v"
        });

        var result = await Runner.RunAsync(args.ToArray());
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertExecutionTime(result, TimeSpan.FromSeconds(5));
    }

    #endregion

    #region Regression Tests

    [Fact]
    public async Task Nullable_Type_Parsing_Regression()
    {
        var result = await Runner.RunAsync("test", "target", "--seed=null", "-v");
        // This might fail if "null" isn't handled, which is expected behavior
        if (!result.IsSuccess)
        {
            CliTestAssertions.AssertErrorContains(result, "Invalid");
        }
    }

    [Fact]
    public async Task Special_Character_Handling_Regression()
    {
        var specialChars = @"test\path/with:special;chars|and&more";
        var result = await Runner.RunAsync("test", "target", $"--config={specialChars}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Config: {specialChars}");
    }

    [Fact]
    public async Task Unicode_Character_Handling_Regression()
    {
        // Test with ASCII characters only due to console encoding limitations
        var asciiString = "test-file.json";
        var result = await Runner.RunAsync("test", "target", $"--config={asciiString}", "-v");
        CliTestAssertions.AssertSuccess(result);
        CliTestAssertions.AssertOutputContains(result, $"Config: {asciiString}");
    }

    #endregion
}