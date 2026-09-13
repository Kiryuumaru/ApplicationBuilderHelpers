using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process reflection-resilience tests for the CLI parser.
/// Exercises behavior the S1 hardening must preserve through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// base-class option inheritance, exact typed-array materialization for
/// <c>int[]</c>, enum parsing, and styled errors (never a raw fallback
/// exception) when a typed-array factory is unavailable.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ReflectionResilienceTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    public enum ProbeShade
    {
        Red,
        Green,
        Blue
    }

    public abstract class ShadedBaseCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.")]
        public string? Mode { get; set; }
    }

    [Command("typedleaf", "Probes inherited option binding.")]
    public sealed class InheritedOptionLeafCommand : ShadedBaseCommand
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Mode: {Mode ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("typedscores", "Probes exact typed-array binding.")]
    public sealed class TypedScoresCommand : Command
    {
        public static int[]? Captured;

        [CommandOption("scores", Description = "Scores.")]
        public int[]? Scores { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Captured = Scores;
            Console.WriteLine($"Scores: {(Scores is null ? "null" : string.Join(",", Scores))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("typedshade", "Probes enum option parsing.")]
    public sealed class TypedShadeCommand : Command
    {
        [CommandOption("shade", Description = "Shade choice.")]
        public ProbeShade Shade { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Shade: {Shade}");
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Custom <see cref="int"/> parser whose array factory throws, so the
    /// collection materialization path cannot silently substitute a wider
    /// array shape. Registered through the public <c>AddCommandTypeParser</c>
    /// entry point. Scalar parsing delegates to <see cref="int.TryParse"/>.
    /// </summary>
    public sealed class ThrowingIntArrayParser : ICommandTypeParser
    {
        public Type Type => typeof(int);

        public object? Parse(string? value, out string? validateError)
        {
            if (int.TryParse(value, out var result))
            {
                validateError = null;
                return result;
            }

            validateError = $"Invalid Int32 value: '{value}'. Expected a valid Int32.";
            return null;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => default(int);

        public Array CreateTypedArray(int length) => throw new InvalidOperationException("No typed array.");
    }

    [Fact]
    public async Task InheritedBaseOption_BindsOnLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedleaf", "--mode=json"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Mode: json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task IntegerArray_BindsExactTypedArray()
    {
        TypedScoresCommand.Captured = null;
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedscores", "--scores=1", "--scores=2", "--scores=3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Scores: 1,2,3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.NotNull(TypedScoresCommand.Captured);
        Assert.Equal(typeof(int[]), TypedScoresCommand.Captured.GetType());
        Assert.Equal([1, 2, 3], TypedScoresCommand.Captured);
    }

    [Fact]
    public async Task IntegerArray_InvalidElement_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedscores", "--scores=1", "--scores=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Int32 value: 'abc'", error);
    }

    [Fact]
    public async Task EnumOption_MatchingIgnoresCase()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedshade", "--shade=green"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Shade: Green", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task EnumOption_InvalidValue_ListsAllowedValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedshade", "--shade=Purple"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'Purple' is not valid for option '--shade'", error);
        Assert.Contains("Must be one of:", error);
        Assert.Contains("Red", error);
    }

    /// <summary>
    /// The S1 contract allows two outcomes when the element parser cannot
    /// supply a typed array: materialize the real element type or report a
    /// styled usage error. What must never happen is the current fault path:
    /// the <c>object[]</c> fallback is unassignable to <c>int[]</c>, so
    /// <c>Property.SetValue</c> throws a raw exception (exit 1). This test
    /// fails on that fault and passes on either contracted outcome.
    /// </summary>
    [Fact]
    public async Task IntegerArray_ArrayFactoryUnavailable_NeverFaults()
    {
        TypedScoresCommand.Captured = null;
        var builder = CreateBuilder().AddCommandTypeParser<ThrowingIntArrayParser>();
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["typedscores", "--scores=1", "--scores=2"]);

        Assert.True(exitCode == 0 || exitCode == 2, $"Expected success or styled usage error but got fault exit {exitCode}. Stdout: {output} Stderr: {error}");
        if (exitCode == 0)
        {
            Assert.NotNull(TypedScoresCommand.Captured);
            Assert.Equal(typeof(int[]), TypedScoresCommand.Captured.GetType());
            Assert.Equal([1, 2], TypedScoresCommand.Captured);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        else
        {
            Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
            Assert.Contains("--scores", error);
        }
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("reflect-test")
            .SetExecutableTitle("Reflect Test")
            .SetExecutableDescription("Reflection resilience verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<InheritedOptionLeafCommand>()
            .AddCommand<TypedScoresCommand>()
            .AddCommand<TypedShadeCommand>();
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
