# ApplicationBuilderHelpers Test Playground

A comprehensive testing framework for the ApplicationBuilderHelpers CLI using process-based testing.

## Overview

This test framework executes the compiled `test.exe` CLI application and validates its behavior by examining stdout, stderr, and exit codes. This approach provides true end-to-end testing of the CLI application.

## Features

- **Process-based testing**: Executes the actual CLI binary
- **Comprehensive assertions**: Validate exit codes, stdout, stderr, and execution time
- **Test organization**: Tests are organized into suites and groups
- **Flexible test execution**: Run all tests or specific suites
- **Detailed reporting**: Clear output showing passed/failed tests with timing
- **Edge case coverage**: Tests for various input scenarios and error conditions

## Test Suites

1. **Command Line Parsing Tests**
   - Basic command parsing
   - Option formats (long, short, compact, equals syntax)
   - Boolean option handling (flag mode and value mode)
   - Environment variable support
   - Array options

2. **Validation Tests**
   - FromAmong validation with case-insensitive matching
   - Type validation (integers, doubles, etc.)
   - Required options and arguments
   - Error message validation

3. **Complex Scenarios Tests**
   - Multiple options with mixed formats
   - All boolean value formats (`--diag`, `--diag=true`, `--diag false`, etc.)
   - Performance testing with many options
   - Subcommand help testing

4. **Edge Cases Tests**
   - Empty strings and special characters
   - Unicode support
   - Mixed option styles
   - Negative numbers and complex values

## Running Tests

### Prerequisites

1. Build the test CLI application:
```sh
dotnet build ../ApplicationBuilderHelpers.Test.Cli
```

2. Build the test playground:
```sh
dotnet build
```

### Run All Tests
```sh
dotnet run
```

### Run Specific Test Suite
```sh
dotnet run -- "Validation"
dotnet run -- "Command Line"
```

### Verbose Mode
```sh
dotnet run -- --verbose
```

## Test Framework Architecture

### Core Components

1. **CliTestRunner**: Executes the CLI and captures output with timeout handling
2. **CliTestAssertions**: Provides assertion methods for validating results
3. **TestSuiteBase**: Base class for organizing tests into suites and groups
4. **TestResult/TestSuiteResult**: Captures test execution results and timing

### Writing New Tests

Create a new test suite by extending `TestSuiteBase`:

```csharp
public class MyTests : TestSuiteBase
{
    public MyTests(CliTestRunner runner) : base(runner, "My Test Suite") { }

    protected override void DefineTests()
    {
        Test("Should do something", async () =>
        {
            var result = await Runner.RunAsync("command", "arg1", "--option=value");
            CliTestAssertions.AssertSuccess(result);
            CliTestAssertions.AssertOutputContains(result, "expected output");
        });

        TestGroup("Feature Group", () =>
        {
            Test("Feature test 1", async () => { ... });
            Test("Feature test 2", async () => { ... });
        });
    }
}
```

### Assertion Methods

- `AssertSuccess(result)` - Verify exit code is 0
- `AssertFailure(result)` - Verify exit code is non-zero
- `AssertExitCode(result, expected)` - Verify specific exit code
- `AssertOutputContains(result, text)` - Check stdout contains text
- `AssertOutputDoesNotContain(result, text)` - Check stdout doesn't contain text
- `AssertErrorContains(result, text)` - Check stderr contains text
- `AssertOutputMatches(result, pattern)` - Regex match on stdout
- `AssertNoError(result)` - Verify no stderr output
- `AssertExecutionTime(result, maxDuration)` - Performance assertion

## Test Coverage

The test suite covers all major features of the ApplicationBuilderHelpers CommandLineParser:

### ? Option Formats
- Long options: `--log-level=debug`, `--log-level debug`
- Short options: `-l=debug`, `-l debug`
- Compact format: `-ldebug`

### ? Boolean Options
- Flag mode: `--diag` sets `diag = true`
- Value mode: `--diag=false`, `--diag true`, `--diag=yes`, etc.
- All supported boolean values: `true/false`, `yes/no`, `1/0`

### ? Validation
- FromAmong validation with clear error messages
- Case-insensitive validation
- Type parsing and error handling
- Required options and arguments

### ? Advanced Features
- Environment variable support
- Array options with multiple values
- Subcommands and cascading help
- Global options across commands

### ? Edge Cases
- Unicode characters
- Special characters and spaces
- Empty values
- Performance with many options

## Example Test Output

```
ApplicationBuilderHelpers Test Playground
=========================================

?? Test executable: C:\...\test.exe

============================================================
Running Test Suite: Command Line Parsing
============================================================
? Should show help when no arguments provided (125ms)
? Should show version with --version flag (98ms)
? Should show version with -V flag (95ms)

  Option Formats:
  ? Long option with equals (--log-level=debug) (102ms)
  ? Long option with space (--log-level debug) (99ms)
  ? Short option with equals (-l=debug) (101ms)
  ? Compact short option (-ldebug) (105ms)

  Boolean Options:
  ? Boolean flag mode (--diag) (98ms)
  ? Boolean with true value (--diag true) (103ms)
  ? Boolean with equals false (--diag=false) (97ms)
  ...

????????????????????????????????????????????????????????????
Suite Summary: 15 passed, 0 failed, 15 total
Duration: 1.52s

================================================================================
OVERALL TEST SUMMARY
================================================================================

Test Suites: 4
Total Tests: 45
Passed: 45 ?
Failed: 0 ?
Duration: 5.23s

? ALL TESTS PASSED!
```

## Features Tested

### Command Line Parser Features
- ? Automatic help and version handling
- ? Command discovery and execution
- ? Global vs command-specific options
- ? Colored help output with themes
- ? Dynamic two-column layout

### Option Parsing
- ? Multiple option formats (long, short, compact, equals)
- ? Boolean options with flag and value modes
- ? Array options with multiple values
- ? Environment variable fallback
- ? FromAmong validation with case-insensitive matching

### Error Handling
- ? Clear error messages for invalid values
- ? Missing required options/arguments
- ? Type validation errors
- ? Unknown commands

This comprehensive testing framework ensures that the ApplicationBuilderHelpers library works correctly in real-world usage scenarios and provides confidence in its reliability and feature completeness.