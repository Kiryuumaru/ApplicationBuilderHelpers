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
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
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

    [Command("typedlists", "Probes parser-owned typed-list binding.")]
    public sealed class TypedListsCommand : Command
    {
        public static List<int>? Captured;

        [CommandOption("scores", Description = "Scores.")]
        public List<int>? Scores { get; set; }

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

        public System.Collections.IList CreateTypedList(int capacity) => throw new InvalidOperationException("No typed list.");
    }

    /// <summary>
    /// Custom <see cref="object"/> parser whose factories throw, so the
    /// exactly-typed <c>List&lt;object?&gt;</c> fallback path is exercised.
    /// Registered through the public <c>AddCommandTypeParser</c> entry point.
    /// </summary>
    public sealed class ThrowingObjectListParser : ICommandTypeParser
    {
        public Type Type => typeof(object);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            return value;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => null;

        public Array CreateTypedArray(int length) => throw new InvalidOperationException("No typed array.");

        public System.Collections.IList CreateTypedList(int capacity) => throw new InvalidOperationException("No typed list.");
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
    /// <c>Property.SetValue</c> throws a raw exception (exit 1). Fails
    /// on that fault; passes on either contracted outcome.
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

    [Fact]
    public async Task IntegerList_BindsExactTypedList()
    {
        TypedListsCommand.Captured = null;
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedlists", "--scores=1", "--scores=2", "--scores=3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Scores: 1,2,3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.NotNull(TypedListsCommand.Captured);
        Assert.Equal(typeof(List<int>), TypedListsCommand.Captured.GetType());
        Assert.Equal([1, 2, 3], TypedListsCommand.Captured);
    }

    /// <summary>
    /// List-branch parity with the array S1 contract: when the element parser's
    /// list factory throws, binding reports a styled usage error (exit 2)
    /// instead of a raw fault (exit 1).
    /// </summary>
    [Fact]
    public async Task IntegerList_ListFactoryUnavailable_ReportsUsageError()
    {
        TypedListsCommand.Captured = null;
        var builder = CreateBuilder().AddCommandTypeParser<ThrowingIntArrayParser>();
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["typedlists", "--scores=1", "--scores=2"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("--scores", error);
        Assert.Contains("failed to create a typed list", error);
    }

    [Command("typedshapes", "Probes all list-compatible collection shapes.")]
    public sealed class TypedShapesCommand : Command
    {
        public static List<int>? CapturedList;
        public static IEnumerable<int>? CapturedEnumerable;
        public static ICollection<int>? CapturedCollection;
        public static IList<int>? CapturedListInterface;
        public static List<object>? CapturedObjects;

        [CommandOption("scores-list", Description = "Scores as list.")]
        public List<int>? ScoresList { get; set; }

        [CommandOption("scores-enumerable", Description = "Scores as enumerable.")]
        public IEnumerable<int>? ScoresEnumerable { get; set; }

        [CommandOption("scores-collection", Description = "Scores as collection.")]
        public ICollection<int>? ScoresCollection { get; set; }

        [CommandOption("scores-ilist", Description = "Scores as list interface.")]
        public IList<int>? ScoresIList { get; set; }

        [CommandOption("blobs", Description = "Blobs as object list.")]
        public List<object>? Blobs { get; set; }

        [CommandOption("shades", Description = "Shades as enum list (no parser registered).")]
        public List<ProbeShade>? Shades { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            CapturedList = ScoresList;
            CapturedEnumerable = ScoresEnumerable;
            CapturedCollection = ScoresCollection;
            CapturedListInterface = ScoresIList;
            CapturedObjects = Blobs;
            Console.WriteLine($"List: {(ScoresList is null ? "null" : string.Join(",", ScoresList))}");
            Console.WriteLine($"Enumerable: {(ScoresEnumerable is null ? "null" : string.Join(",", ScoresEnumerable))}");
            Console.WriteLine($"Collection: {(ScoresCollection is null ? "null" : string.Join(",", ScoresCollection))}");
            Console.WriteLine($"IList: {(ScoresIList is null ? "null" : string.Join(",", ScoresIList))}");
            Console.WriteLine($"Blobs: {(Blobs is null ? "null" : string.Join(",", Blobs))}");
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Custom <see cref="int"/> parser that records the capacity hint passed to
    /// <c>CreateTypedList</c>, proving the factory receives the element count
    /// (not zero or a constant). Scalar parsing delegates to
    /// <see cref="int.TryParse"/>.
    /// </summary>
    public sealed class CapacityRecordingIntParser : ICommandTypeParser
    {
        public static int LastCapacity = -1;

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

        public Array CreateTypedArray(int length) => new int[length];

        public System.Collections.IList CreateTypedList(int capacity)
        {
            LastCapacity = capacity;
            return new List<int>(capacity);
        }
    }

    [Fact]
    public async Task TypedListFactory_ReceivesElementCountAsCapacity()
    {
        CapacityRecordingIntParser.LastCapacity = -1;
        TypedListsCommand.Captured = null;
        var builder = CreateBuilder().AddCommandTypeParser<CapacityRecordingIntParser>();
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["typedlists", "--scores=1", "--scores=2", "--scores=3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Scores: 1,2,3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Equal(3, CapacityRecordingIntParser.LastCapacity);
        Assert.NotNull(TypedListsCommand.Captured);
        Assert.Equal(typeof(List<int>), TypedListsCommand.Captured.GetType());
    }

    [Fact]
    public async Task AllListShapes_BindExactTypedLists()
    {
        TypedShapesCommand.CapturedList = null;
        TypedShapesCommand.CapturedEnumerable = null;
        TypedShapesCommand.CapturedCollection = null;
        TypedShapesCommand.CapturedListInterface = null;
        TypedShapesCommand.CapturedObjects = null;
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedshapes", "--scores-list=1", "--scores-list=2", "--scores-enumerable=1", "--scores-enumerable=2", "--scores-collection=1", "--scores-collection=2", "--scores-ilist=1", "--scores-ilist=2"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("List: 1,2", output);
        Assert.Contains("Enumerable: 1,2", output);
        Assert.Contains("Collection: 1,2", output);
        Assert.Contains("IList: 1,2", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.NotNull(TypedShapesCommand.CapturedList);
        Assert.Equal(typeof(List<int>), TypedShapesCommand.CapturedList.GetType());
        Assert.Equal([1, 2], TypedShapesCommand.CapturedList);
        Assert.NotNull(TypedShapesCommand.CapturedEnumerable);
        Assert.Equal(typeof(List<int>), TypedShapesCommand.CapturedEnumerable.GetType());
        Assert.Equal([1, 2], TypedShapesCommand.CapturedEnumerable);
        Assert.NotNull(TypedShapesCommand.CapturedCollection);
        Assert.Equal(typeof(List<int>), TypedShapesCommand.CapturedCollection.GetType());
        Assert.Equal([1, 2], TypedShapesCommand.CapturedCollection);
        Assert.NotNull(TypedShapesCommand.CapturedListInterface);
        Assert.Equal(typeof(List<int>), TypedShapesCommand.CapturedListInterface.GetType());
        Assert.Equal([1, 2], TypedShapesCommand.CapturedListInterface);
    }

    /// <summary>
    /// Object-element parity with the array branch: <c>List&lt;object&gt;</c>
    /// materializes through the exactly-typed <c>List&lt;object?&gt;</c> fallback
    /// when no parser (or a throwing parser) is registered for
    /// <see cref="object"/>, never a styled usage error or raw fault.
    /// </summary>
    [Fact]
    public async Task ObjectList_WithoutParser_BindsAsObjects()
    {
        TypedShapesCommand.CapturedObjects = null;
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedshapes", "--blobs=a", "--blobs=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Blobs: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.NotNull(TypedShapesCommand.CapturedObjects);
        Assert.Equal(new List<object> { "a", "b" }, TypedShapesCommand.CapturedObjects);
    }

    [Fact]
    public async Task ObjectList_ParserListThrows_FallsBackToBoundValues()
    {
        TypedShapesCommand.CapturedObjects = null;
        var builder = CreateBuilder().AddCommandTypeParser<ThrowingObjectListParser>();
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["typedshapes", "--blobs=a", "--blobs=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Blobs: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.NotNull(TypedShapesCommand.CapturedObjects);
        Assert.Equal(new List<object> { "a", "b" }, TypedShapesCommand.CapturedObjects);
    }

    /// <summary>
    /// No-parser error contract for non-object element types: a list property
    /// whose element type has no registered parser reaches the collection
    /// materialization path (e.g. an enum list) and reports a styled usage
    /// error naming the registration path, never a raw fault.
    /// Enum lists exercise this contract because enums convert via the scalar
    /// pipeline (no parser entry), so a valid value survives conversion and
    /// the missing factory appears exactly at materialization.
    /// </summary>
    [Fact]
    public async Task ListShape_UnsupportedElementType_ReportsUsageError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["typedshapes", "--shades=Red", "--shades=Green"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("--shades", error);
        Assert.Contains("No type parser is registered", error);
        Assert.Contains("AddCommandTypeParser", error);
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
            .AddCommand<TypedListsCommand>()
            .AddCommand<TypedShapesCommand>()
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
