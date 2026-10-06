using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process parser-type tests for boolean and small numeric/date parsers.
/// Exercises the built-in parsers through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// valid values bind, overflow/empty/invalid values report stderr errors.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ParserTypeTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("numprobe", "Probes boolean and numeric parsing.")]
    public sealed class NumericProbeCommand : Command
    {
        [CommandOption("flag", Description = "Boolean flag.")]
        public bool Flag { get; set; }

        [CommandOption("byte-val", Description = "Byte value.")]
        public byte ByteVal { get; set; }

        [CommandOption("sbyte-val", Description = "SByte value.")]
        public sbyte SByteVal { get; set; }

        [CommandOption("short-val", Description = "Short value.")]
        public short ShortVal { get; set; }

        [CommandOption("ushort-val", Description = "UShort value.")]
        public ushort UShortVal { get; set; }

        [CommandOption("uint-val", Description = "UInt value.")]
        public uint UIntVal { get; set; }

        [CommandOption("ulong-val", Description = "ULong value.")]
        public ulong ULongVal { get; set; }

        [CommandOption("long-val", Description = "Long value.")]
        public long LongVal { get; set; }

        [CommandOption("float-val", Description = "Float value.")]
        public float FloatVal { get; set; }

        [CommandOption("double-val", Description = "Double value.")]
        public double DoubleVal { get; set; }

        [CommandOption("decimal-val", Description = "Decimal value.")]
        public decimal DecimalVal { get; set; }

        [CommandOption("char-val", Description = "Char value.")]
        public char CharVal { get; set; }

        [CommandOption("date-val", Description = "Date value.")]
        public DateTime DateVal { get; set; }

        [CommandOption("offset-val", Description = "Offset value.")]
        public DateTimeOffset OffsetVal { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Flag: {Flag}");
            Console.WriteLine($"ByteVal: {ByteVal}");
            Console.WriteLine($"SByteVal: {SByteVal}");
            Console.WriteLine($"ShortVal: {ShortVal}");
            Console.WriteLine($"UShortVal: {UShortVal}");
            Console.WriteLine($"UIntVal: {UIntVal}");
            Console.WriteLine($"ULongVal: {ULongVal}");
            Console.WriteLine($"LongVal: {LongVal}");
            Console.WriteLine($"FloatVal: {FloatVal}");
            Console.WriteLine($"DoubleVal: {DoubleVal}");
            Console.WriteLine($"DecimalVal: {DecimalVal}");
            Console.WriteLine($"CharVal: {CharVal}");
            Console.WriteLine($"DateVal: {DateVal:yyyy-MM-dd}");
            Console.WriteLine($"OffsetVal: {OffsetVal:yyyy-MM-dd}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData("true")]
    [InlineData("TRUE")]
    [InlineData("Yes")]
    [InlineData("on")]
    [InlineData("1")]
    public async Task Boolean_TrueSynonyms_RejectBareOnly(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", $"--flag={input}"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--flag' does not accept a value", error);
        Assert.Contains($"'{input}'", error);
    }

    [Theory]
    [InlineData("false")]
    [InlineData("FALSE")]
    [InlineData("No")]
    [InlineData("off")]
    [InlineData("0")]
    public async Task Boolean_FalseSynonyms_RejectBareOnly(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", $"--flag={input}"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--flag' does not accept a value", error);
        Assert.Contains($"'{input}'", error);
    }

    [Fact]
    public async Task Boolean_BareFlag_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", "--flag"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Flag: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("maybe")]
    [InlineData("2")]
    [InlineData("truee")]
    public async Task Boolean_InvalidValue_ReportsError(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", $"--flag={input}"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("does not accept a value", error);
    }

    [Fact]
    public async Task IntegerTypes_ValidBoundaries_BindValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync([
            "numprobe",
            "--byte-val=255",
            "--sbyte-val=-128",
            "--short-val=-32768",
            "--ushort-val=65535",
            "--uint-val=4294967295",
            "--ulong-val=18446744073709551615",
            "--long-val=-9223372036854775808",
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains("ByteVal: 255", output);
        Assert.Contains("SByteVal: -128", output);
        Assert.Contains("ShortVal: -32768", output);
        Assert.Contains("UShortVal: 65535", output);
        Assert.Contains("UIntVal: 4294967295", output);
        Assert.Contains("ULongVal: 18446744073709551615", output);
        Assert.Contains("LongVal: -9223372036854775808", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("--byte-val=256", "Value '256' is out of range for Byte. Expected a whole number between 0 and 255.")]
    [InlineData("--byte-val=-1", "Value '-1' is out of range for Byte. Expected a whole number between 0 and 255.")]
    [InlineData("--byte-val=", "Invalid value '' for option '--byte-val': Expected a whole number between 0 and 255.")]
    [InlineData("--sbyte-val=128", "Value '128' is out of range for SByte. Expected a whole number between -128 and 127.")]
    [InlineData("--short-val=32768", "Value '32768' is out of range for Int16. Expected a whole number between -32768 and 32767.")]
    [InlineData("--ushort-val=65536", "Value '65536' is out of range for UInt16. Expected a whole number between 0 and 65535.")]
    [InlineData("--uint-val=-1", "Value '-1' is out of range for UInt32. Expected a whole number between 0 and 4294967295.")]
    [InlineData("--ulong-val=-1", "Value '-1' is out of range for UInt64. Expected a whole number between 0 and 18446744073709551615.")]
    [InlineData("--long-val=9223372036854775808", "Value '9223372036854775808' is out of range for Int64. Expected a whole number between -9223372036854775808 and 9223372036854775807.")]
    public async Task IntegerTypes_OverflowOrEmpty_ReportsError(string option, string expectedError)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains(expectedError, error);
    }

    [Theory]
    [InlineData("x")]
    [InlineData("Z")]
    [InlineData("7")]
    public async Task Char_ValidValue_BindsValue(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", $"--char-val={input}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"CharVal: {input}", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("--char-val=", "Invalid value '' for option '--char-val': Expected a single character.")]
    [InlineData("--char-val=ab", "Invalid Char value: 'ab'. Expected a single character.")]
    [InlineData("--char-val=12", "Invalid Char value: '12'. Expected a single character.")]
    public async Task Char_InvalidValue_ReportsError(string option, string expectedError)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains(expectedError, error);
    }

    [Fact]
    public async Task FloatingTypes_ValidValues_BindValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync([
            "numprobe",
            "--float-val=1.5",
            "--double-val=2.5",
            "--decimal-val=123.45",
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains("FloatVal: 1.5", output);
        Assert.Contains("DoubleVal: 2.5", output);
        Assert.Contains("DecimalVal: 123.45", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("--float-val=abc", "Invalid Single value: 'abc'. Expected a number (for example '1.5').")]
    [InlineData("--float-val=", "Invalid value '' for option '--float-val': Expected a number (for example '1.5').")]
    [InlineData("--double-val=not-a-number", "Invalid Double value: 'not-a-number'. Expected a number (for example '1.5').")]
    [InlineData("--decimal-val=12.34.56", "Invalid Decimal value: '12.34.56'. Expected a decimal number (for example '123.45').")]
    public async Task FloatingTypes_InvalidValue_ReportsError(string option, string expectedError)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains(expectedError, error);
    }

    [Fact]
    public async Task DateTypes_ValidValues_BindValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync([
            "numprobe",
            "--date-val=2024-01-15",
            "--offset-val=2024-01-15T12:30:00+02:00",
        ]);

        Assert.Equal(0, exitCode);
        Assert.Contains("DateVal: 2024-01-15", output);
        Assert.Contains("OffsetVal: 2024-01-15", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("--date-val=not-a-date", "Invalid DateTime value: 'not-a-date'. Expected a date and time (for example '2024-01-15' or '2024-01-15 13:30:00').")]
    [InlineData("--offset-val=not-an-offset", "Invalid DateTimeOffset value: 'not-an-offset'. Expected a date and time with an offset (for example '2024-01-15 13:30:00 +02:00').")]
    public async Task DateTypes_InvalidValue_ReportsError(string option, string expectedError)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["numprobe", option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains(expectedError, error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("parser-test")
            .SetExecutableTitle("Parser Test")
            .SetExecutableDescription("Parser verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<NumericProbeCommand>();
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
