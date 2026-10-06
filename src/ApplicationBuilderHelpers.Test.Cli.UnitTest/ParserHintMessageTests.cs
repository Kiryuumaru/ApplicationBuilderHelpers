using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser.TypeConversion;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process format-hint tests for scalar parse failures.
/// Every built-in parser names the expected shape on failure, whole-number
/// parsers split out-of-range input from malformed input, and both enum
/// failure sites name the allowed values. All failures stay
/// <see cref="CommandErrorKind.InvalidValue"/> (exit 2).
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ParserHintMessageTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    public enum HintShade
    {
        Red,
        Green,
        Blue
    }

    [Command("hintprobe", "Probes scalar format hints.")]
    public sealed class HintProbeCommand : Command
    {
        [CommandOption("int-val", Description = "Int value.")]
        public int IntVal { get; set; }

        [CommandOption("long-val", Description = "Long value.")]
        public long LongVal { get; set; }

        [CommandOption("uint-val", Description = "UInt value.")]
        public uint UIntVal { get; set; }

        [CommandOption("double-val", Description = "Double value.")]
        public double DoubleVal { get; set; }

        [CommandOption("decimal-val", Description = "Decimal value.")]
        public decimal DecimalVal { get; set; }

        [CommandOption("char-val", Description = "Char value.")]
        public char CharVal { get; set; }

        [CommandOption("guid-val", Description = "Guid value.")]
        public Guid GuidVal { get; set; }

        [CommandOption("date-val", Description = "Date value.")]
        public DateTime DateVal { get; set; }

        [CommandOption("offset-val", Description = "Offset value.")]
        public DateTimeOffset OffsetVal { get; set; }

        [CommandOption("day-val", Description = "Day value.")]
        public DateOnly DayVal { get; set; }

        [CommandOption("tod-val", Description = "Time value.")]
        public TimeOnly TodVal { get; set; }

        [CommandOption("span-val", Description = "Span value.")]
        public TimeSpan SpanVal { get; set; }

        [CommandOption("uri-val", Description = "Uri value.")]
        public Uri? UriVal { get; set; }

        [CommandOption("version-val", Description = "Version value.")]
        public Version? VersionVal { get; set; }

        [CommandOption("shade", Description = "Shade value.")]
        public HintShade Shade { get; set; } = HintShade.Red;

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hintprobe:ok");
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData("--int-val=abc", "Invalid Int32 value: 'abc'. Expected a whole number between -2147483648 and 2147483647.")]
    [InlineData("--long-val=abc", "Invalid Int64 value: 'abc'. Expected a whole number between -9223372036854775808 and 9223372036854775807.")]
    [InlineData("--uint-val=abc", "Invalid UInt32 value: 'abc'. Expected a whole number between 0 and 4294967295.")]
    [InlineData("--double-val=abc", "Invalid Double value: 'abc'. Expected a number (for example '1.5').")]
    [InlineData("--decimal-val=abc", "Invalid Decimal value: 'abc'. Expected a decimal number (for example '123.45').")]
    [InlineData("--char-val=ab", "Invalid Char value: 'ab'. Expected a single character.")]
    [InlineData("--guid-val=abc", "Invalid Guid value: 'abc'. Expected a GUID (for example '3f2504e0-4f89-11d3-9a0c-0305e82c3301').")]
    [InlineData("--date-val=abc", "Invalid DateTime value: 'abc'. Expected a date and time (for example '2024-01-15' or '2024-01-15 13:30:00').")]
    [InlineData("--offset-val=abc", "Invalid DateTimeOffset value: 'abc'. Expected a date and time with an offset (for example '2024-01-15 13:30:00 +02:00').")]
    [InlineData("--day-val=abc", "Invalid DateOnly value: 'abc'. Expected a date (for example '2024-01-15').")]
    [InlineData("--tod-val=abc", "Invalid TimeOnly value: 'abc'. Expected a time (for example '13:30').")]
    [InlineData("--span-val=abc", "Invalid TimeSpan value: 'abc'. Expected a time span (for example '01:30:00').")]
    [InlineData("--uri-val=:::not a uri", "Invalid Uri value: ':::not a uri'. Expected a URI (for example 'https://example.com').")]
    [InlineData("--version-val=abc", "Invalid Version value: 'abc'. Expected a version (for example '1.2.3').")]
    public async Task Scalar_Shape_Failure_Names_Format_Hint(string option, string expectedHint)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["hintprobe", option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains(expectedHint, error);
    }

    [Theory]
    [InlineData("--int-val=2147483648", "Value '2147483648' is out of range for Int32.")]
    [InlineData("--long-val=9223372036854775808", "Value '9223372036854775808' is out of range for Int64.")]
    [InlineData("--uint-val=-1", "Value '-1' is out of range for UInt32.")]
    public async Task Scalar_Range_Failure_Names_Range_Split(string option, string expectedRange)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["hintprobe", option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains(expectedRange, error);
        Assert.Contains("Expected a whole number between", error);
    }

    [Theory]
    [InlineData("--decimal-val=1e5", "Invalid Decimal value: '1e5'. Expected a decimal number without an exponent (for example '123.45').")]
    [InlineData("--decimal-val=79228162514264337593543950336", "Value '79228162514264337593543950336' is out of range for Decimal. Expected a decimal number (for example '123.45').")]
    public async Task Decimal_Shape_And_Overflow_Branch_Pins(string option, string expectedHint)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["hintprobe", option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains(expectedHint, error);
    }

    [Fact]
    public async Task Enum_EmptyEquals_Names_Allowed_Values()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["hintprobe", "--shade="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value '' for option '--shade'", error);
        Assert.Contains("Expected one of: Red, Green, Blue.", error);
    }

    [Fact]
    public async Task Enum_InvalidValue_Names_Allowed_Values()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["hintprobe", "--shade=Purple"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'Purple' is not valid for option '--shade'", error);
        Assert.Contains("Must be one of: Red, Green, Blue", error);
    }

    [Theory]
    [InlineData("--decimal-val=NaN")]
    [InlineData("--decimal-val=Infinity")]
    public async Task Decimal_NonFinite_Shape_Failure_Names_Format_Hint(string option)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["hintprobe", option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains($"Invalid Decimal value: '{option["--decimal-val=".Length..]}'. Expected a decimal number (for example '123.45').", error);
    }

    [Fact]
    public void Enum_Conversion_Failure_Names_Allowed_Values()
    {
        var collection = ApplicationBuilder.Create();
        var failure = Assert.Throws<CommandException>(() =>
            TypeConversion.Convert("Purple", typeof(HintShade), false, null, "option '--shade'", collection, false, false));

        Assert.Equal(2, failure.ExitCode);
        Assert.Equal(CommandErrorKind.InvalidValue, failure.Kind);
        Assert.Contains("Expected one of: Red, Green, Blue.", failure.Message);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("hint-test")
            .SetExecutableTitle("Hint Test")
            .SetExecutableDescription("Parser hint verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<HintProbeCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
    {
        await ConsoleGate.WaitAsync();
        try
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            using var outWriter = new StringWriter();
            using var errorWriter = new StringWriter();
            Console.SetOut(outWriter);
            Console.SetError(errorWriter);
            try
            {
                var exitCode = await CreateBuilder().RunAsync(args);
                outWriter.Flush();
                errorWriter.Flush();
                return (exitCode, outWriter.ToString(), errorWriter.ToString());
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
        finally
        {
            ConsoleGate.Release();
        }
    }
}
