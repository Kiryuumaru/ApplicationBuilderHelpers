using System.Globalization;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Culture-invariant parsing tests: CLI tokens resolve identically regardless of
/// <see cref="CultureInfo.CurrentCulture"/>. Pins decimal-dot scalars, negative
/// numeric tokenizing, the <c>ChangeType</c> fallback, and comma-input behavior
/// under a comma-decimal locale (de-DE) through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point.
/// Probes format numbers with <see cref="CultureInfo.InvariantCulture"/> explicitly
/// because console display formatting is culture-sensitive by design (out of scope).
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CultureInvariantParsingTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);
    private static readonly CultureInfo CommaDecimalCulture = new("de-DE");

    [Command("cultureprobe", "Probes culture-invariant scalar parsing.")]
    public sealed class CultureProbeCommand : Command
    {
        [CommandOption("double-val", Description = "Double value.")]
        public double DoubleVal { get; set; }

        [CommandOption("float-val", Description = "Float value.")]
        public float FloatVal { get; set; }

        [CommandOption("decimal-val", Description = "Decimal value.")]
        public decimal DecimalVal { get; set; }

        [CommandArgument("offset", Description = "Offset value.", Position = 0)]
        public double Offset { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"DoubleVal: {DoubleVal.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"FloatVal: {FloatVal.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"DecimalVal: {DecimalVal.ToString(CultureInfo.InvariantCulture)}");
            Console.WriteLine($"Offset: {Offset.ToString(CultureInfo.InvariantCulture)}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("culturetok", "Probes culture-invariant numeric tokenizing.")]
    public sealed class CultureTokenizerCommand : Command
    {
        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Dedicated <c>ChangeType</c>-fallback probe: no parser is registered for
    /// <see cref="object"/> (see the registration list in the
    /// <c>ApplicationBuilder</c> constructor), and every
    /// <see cref="IConvertible"/>-from-string primitive (bool, numerics,
    /// DateTime, ...) owns a registry parser, so <c>object</c> is the only
    /// type that converts successfully through the <c>ConvertCore</c>-to-
    /// <c>ChangeType</c> fallback. A registry parser for <c>object</c> would
    /// silently reroute the probe, so the probe uses <c>object</c>
    /// rather than any registered scalar.
    /// </summary>
    [Command("culturefallback", "Probes the culture-invariant ChangeType fallback.")]
    public sealed class CultureFallbackCommand : Command
    {
        [CommandOption("code", Description = "Opaque code value.")]
        public object? Code { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Code: {Code?.ToString() ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Theory]
    [InlineData("--double-val=85.5", "DoubleVal: 85.5")]
    [InlineData("--double-val=-1.5", "DoubleVal: -1.5")]
    [InlineData("--float-val=1.5", "FloatVal: 1.5")]
    [InlineData("--float-val=-2.25", "FloatVal: -2.25")]
    [InlineData("--decimal-val=123.45", "DecimalVal: 123.45")]
    [InlineData("--decimal-val=-99.99", "DecimalVal: -99.99")]
    public async Task DecimalDotScalars_ParseIdenticallyUnderCommaDecimalCulture(string option, string expectedLine)
    {
        var (deExit, deOutput, deError) = await RunUnderCultureAsync(["cultureprobe", option], CommaDecimalCulture);
        var (ivExit, ivOutput, ivError) = await RunUnderCultureAsync(["cultureprobe", option], CultureInfo.InvariantCulture);

        Assert.Equal(0, deExit);
        Assert.Equal(0, ivExit);
        Assert.Contains(expectedLine, deOutput);
        Assert.Contains(expectedLine, ivOutput);
        Assert.Equal(ivOutput, deOutput);
        Assert.True(string.IsNullOrWhiteSpace(deError), $"Expected empty stderr but got: {deError}");
        Assert.True(string.IsNullOrWhiteSpace(ivError), $"Expected empty stderr but got: {ivError}");
    }

    [Fact]
    public async Task NegativePositional_BindsUnderCommaDecimalCulture()
    {
        var (exitCode, output, error) = await RunUnderCultureAsync(["cultureprobe", "-1.5"], CommaDecimalCulture);

        Assert.Equal(0, exitCode);
        Assert.Contains("Offset: -1.5", output);
        Assert.DoesNotContain("Unknown option", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("--double-val=-1.5", "DoubleVal: -1.5")]
    [InlineData("--decimal-val=-99.99", "DecimalVal: -99.99")]
    public async Task NegativeOptionValue_EqualsForm_BindsUnderCommaDecimalCulture(string option, string expectedLine)
    {
        var (exitCode, output, error) = await RunUnderCultureAsync(["cultureprobe", option], CommaDecimalCulture);

        Assert.Equal(0, exitCode);
        Assert.Contains(expectedLine, output);
        Assert.DoesNotContain("Unknown option", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NegativeOptionValue_SpaceForm_BindsUnderCommaDecimalCulture()
    {
        var (exitCode, output, error) = await RunUnderCultureAsync(["cultureprobe", "--double-val", "-1.5"], CommaDecimalCulture);

        Assert.Equal(0, exitCode);
        Assert.Contains("DoubleVal: -1.5", output);
        Assert.DoesNotContain("Unknown option", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("-5")]
    [InlineData("-1.5")]
    [InlineData("-0.25")]
    public async Task NegativeNumerics_TokenizeAsPositionalsUnderCommaDecimalCulture(string token)
    {
        var (exitCode, output, error) = await RunUnderCultureAsync(["culturetok", token], CommaDecimalCulture);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Items: {token}", output);
        Assert.DoesNotContain("Unknown option", error);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnregisteredFallback_ConvertsIdenticallyUnderCommaDecimalCulture()
    {
        var (deExit, deOutput, deError) = await RunUnderCultureAsync(["culturefallback", "--code=85.5"], CommaDecimalCulture);
        var (ivExit, ivOutput, ivError) = await RunUnderCultureAsync(["culturefallback", "--code=85.5"], CultureInfo.InvariantCulture);

        Assert.Equal(0, deExit);
        Assert.Equal(0, ivExit);
        Assert.Contains("Code: 85.5", deOutput);
        Assert.Equal(ivOutput, deOutput);
        Assert.True(string.IsNullOrWhiteSpace(deError), $"Expected empty stderr but got: {deError}");
        Assert.True(string.IsNullOrWhiteSpace(ivError), $"Expected empty stderr but got: {ivError}");
    }

    [Theory]
    [InlineData("--double-val=85,5", "DoubleVal: 855")]
    [InlineData("--decimal-val=85,5", "DecimalVal: 855")]
    public async Task CommaInput_ResolvesAsThousandsSeparatorDeterministically(string option, string expectedLine)
    {
        var (deExit, deOutput, deError) = await RunUnderCultureAsync(["cultureprobe", option], CommaDecimalCulture);
        var (ivExit, ivOutput, ivError) = await RunUnderCultureAsync(["cultureprobe", option], CultureInfo.InvariantCulture);

        Assert.Equal(0, deExit);
        Assert.Equal(0, ivExit);
        Assert.Contains(expectedLine, deOutput);
        Assert.DoesNotContain("85.5", deOutput);
        Assert.Equal(ivOutput, deOutput);
        Assert.True(string.IsNullOrWhiteSpace(deError), $"Expected empty stderr but got: {deError}");
        Assert.True(string.IsNullOrWhiteSpace(ivError), $"Expected empty stderr but got: {ivError}");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunUnderCultureAsync(string[] args, CultureInfo culture)
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = culture;
            CultureInfo.CurrentUICulture = culture;
            return await RunCapturedAsync(CreateBuilder(), args);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("culture-test")
            .SetExecutableTitle("Culture Test")
            .SetExecutableDescription("Culture-invariant verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<CultureProbeCommand>()
            .AddCommand<CultureTokenizerCommand>()
            .AddCommand<CultureFallbackCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(ApplicationBuilder builder, string[] args)
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
                var exitCode = await builder.RunAsync(args);
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
