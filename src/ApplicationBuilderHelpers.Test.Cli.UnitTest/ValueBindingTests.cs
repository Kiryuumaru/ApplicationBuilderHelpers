using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process value-binding tests for the CLI parser.
/// Exercises binding through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// scalar type conversions, allowed-value validation, array binding,
/// and positional argument binding.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ValueBindingTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    public enum ProbeColor
    {
        Red,
        Green,
        Blue
    }

    [Command("bindprobe", "Probes scalar value binding.")]
    public sealed class ScalarBindCommand : Command
    {
        [CommandOption("text", Description = "Text value.")]
        public string? Text { get; set; }

        [CommandOption("count", Description = "Integer value.")]
        public int Count { get; set; }

        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption("correlation", Description = "Correlation id.")]
        public Guid CorrelationId { get; set; }

        [CommandOption("not-before", Description = "Start date.")]
        public DateTime NotBefore { get; set; }

        [CommandOption("maybe-count", Description = "Optional integer.")]
        public int? MaybeCount { get; set; }

        [CommandOption("color", Description = "Favorite color.")]
        public ProbeColor Color { get; set; }

        [CommandOption("payload", Description = "Opaque payload.")]
        public object? Payload { get; set; }

        [CommandOption("duration", Description = "Duration value.")]
        public TimeSpan Duration { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Text: {Text ?? "null"}");
            Console.WriteLine($"Count: {Count}");
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Correlation: {CorrelationId}");
            Console.WriteLine($"NotBefore: {NotBefore:yyyy-MM-dd}");
            Console.WriteLine($"MaybeCount: {MaybeCount?.ToString() ?? "null"}");
            Console.WriteLine($"Color: {Color}");
            Console.WriteLine($"Payload: {Payload?.ToString() ?? "null"}");
            Console.WriteLine($"Duration: {Duration}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("bindchoice", "Probes constrained and array value binding.")]
    public sealed class ConstrainedBindCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        [CommandOption("level", Description = "Level.", FromAmong = ["Low", "High"], CaseSensitive = true)]
        public string? Level { get; set; }

        [CommandOption("color", Description = "Color choice.", FromAmong = ["Red", "Green"])]
        public ProbeColor Color { get; set; }

        [CommandOption("tags", Description = "Tags.")]
        public string[]? Tags { get; set; }

        [CommandOption("scores", Description = "Scores.")]
        public int[]? Scores { get; set; }

        [CommandOption("formats", Description = "Formats.", FromAmong = ["json", "xml"])]
        public string[]? Formats { get; set; }

        [CommandOption("blobs", Description = "Blobs.")]
        public object[]? Blobs { get; set; }

        [CommandOption("durations", Description = "Durations.")]
        public TimeSpan[]? Durations { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Mode: {Mode ?? "null"}");
            Console.WriteLine($"Level: {Level ?? "null"}");
            Console.WriteLine($"Color: {Color}");
            Console.WriteLine($"Tags: {(Tags is null ? "null" : string.Join(",", Tags))}");
            Console.WriteLine($"Scores: {(Scores is null ? "null" : string.Join(",", Scores))}");
            Console.WriteLine($"Formats: {(Formats is null ? "null" : string.Join(",", Formats))}");
            Console.WriteLine($"Blobs: {(Blobs is null ? "null" : string.Join(",", Blobs))}");
            Console.WriteLine($"Durations: {(Durations is null ? "null" : string.Join(",", Durations))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("bindargs", "Probes positional argument binding.")]
    public sealed class ArgumentBindCommand : Command
    {
        [CommandArgument("name", Description = "Name.", Position = 0)]
        public string? Name { get; set; }

        [CommandArgument("count", Description = "Count.", Position = 1)]
        public int Count { get; set; }

        [CommandArgument("level", Description = "Level.", Position = 2, FromAmong = ["low", "high"])]
        public string? Level { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Name: {Name ?? "null"}");
            Console.WriteLine($"Count: {Count}");
            Console.WriteLine($"Level: {Level ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("bindmany", "Probes array argument binding.")]
    public sealed class ArrayArgumentBindCommand : Command
    {
        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Custom <see cref="object"/> parser whose array factory throws so the
    /// binder's typed-array fallback path is exercised. Registered through the
    /// public <c>AddCommandTypeParser</c> entry point (see
    /// <c>docs/custom-type-parsers.md</c>). The <c>object</c> element type is
    /// used because the fallback produces an <c>object[]</c>, which is only
    /// assignable back to an <c>object[]</c> property.
    /// </summary>
    public sealed class ThrowingArrayObjectParser : ICommandTypeParser
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

    [Command("bindlists", "Probes object list binding.")]
    public sealed class ObjectListBindCommand : Command
    {
        [CommandOption("blobs", Description = "Blobs.")]
        public List<object>? Blobs { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Blobs: {(Blobs is null ? "null" : string.Join(",", Blobs))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("bindnumbers", "Probes integer array argument binding.")]
    public sealed class IntArrayArgumentBindCommand : Command
    {
        [CommandArgument("numbers", Description = "Numbers.", Position = 0)]
        public int[]? Numbers { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Numbers: {(Numbers is null ? "null" : string.Join(",", Numbers))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("bindboolarg", "Probes boolean positional argument binding.")]
    public sealed class BooleanArgumentBindCommand : Command
    {
        [CommandArgument("flag", Description = "Flag.", Position = 0)]
        public bool Flag { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Flag: {Flag}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("bindrequired", "Probes required positional argument binding.")]
    public sealed class RequiredTestCommand : Command
    {
        [CommandArgument("target", Description = "Target.", Position = 0, Required = true)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Target: {Target ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("bindreqopt", "Probes required option binding.")]
    public sealed class RequiredOptionBindCommand : Command
    {
        [CommandOption("label", Description = "Label value.", Required = true)]
        public string? Label { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Label: {Label ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("bindenv", "Probes environment fallback precedence binding.")]
    public sealed class EnvEmptyPrecedenceCommand : Command
    {
        [CommandOption("config", Description = "Config file path.", EnvironmentVariable = BindEnvConfigVariable)]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Config: {Config ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    private const string BindEnvConfigVariable = "PARKER_BINDENV_CONFIG";

    [Fact]
    public async Task Scalar_String_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--text=hello"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Text: hello", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_String_EmptyValue_BindsEmpty()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--text="]);

        Assert.Equal(0, exitCode);
        var textLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Text:", StringComparison.Ordinal));
        Assert.Equal("Text: ", textLine);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_BareOptionWithoutValue_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--text"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: --text", error);
    }

    [Theory]
    [InlineData("42", "42")]
    [InlineData("0", "0")]
    [InlineData("-5", "-5")]
    public async Task Scalar_Integer_BindsValue(string input, string expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", $"--count={input}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Count: {expected}", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Integer_Invalid_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--count=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Int32 value: 'abc'", error);
    }

    [Theory]
    [InlineData("true")]
    [InlineData("True")]
    [InlineData("yes")]
    [InlineData("on")]
    [InlineData("1")]
    public async Task Scalar_Boolean_TrueSynonyms_BindTrue(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", $"--verbose={input}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("false")]
    [InlineData("No")]
    [InlineData("off")]
    [InlineData("0")]
    public async Task Scalar_Boolean_FalseSynonyms_BindFalse(string input)
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", $"--verbose={input}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Boolean_BareFlag_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Boolean_Invalid_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--verbose=maybe"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value", error);
    }

    [Fact]
    public async Task Scalar_Guid_BindsValue()
    {
        const string input = "3f2504e0-4f89-11d3-9a0c-0305e82c3301";
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", $"--correlation={input}"]);

        Assert.Equal(0, exitCode);
        Assert.Contains($"Correlation: {input}", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Guid_Invalid_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--correlation=not-a-guid"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Guid value", error);
    }

    [Fact]
    public async Task Scalar_DateTime_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--not-before=2024-01-15"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("NotBefore: 2024-01-15", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_DateTime_Invalid_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--not-before=not-a-date"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid DateTime value", error);
    }

    [Fact]
    public async Task Scalar_Nullable_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--maybe-count=7"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("MaybeCount: 7", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Nullable_Omitted_StaysNull()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("MaybeCount: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Nullable_Invalid_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--maybe-count=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Int32 value: 'abc'", error);
    }

    [Fact]
    public async Task Scalar_Enum_MatchingIgnoresCase()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--color=green"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Color: Green", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Enum_Invalid_ReportsAllowedValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--color=Purple"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'Purple' is not valid for option '--color'", error);
        Assert.Contains("Must be one of:", error);
        Assert.Contains("Red", error);
    }

    [Fact]
    public async Task Scalar_Object_BindsViaChangeType()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--payload=hello"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Payload: hello", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_UnsupportedType_ReportsInvalidFormat()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--duration=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value 'abc' for option '--duration'", error);
        Assert.Contains("Invalid TimeSpan value: 'abc'", error);
    }

    [Fact]
    public async Task Constrained_Mode_AcceptsListedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--mode=json"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Mode: json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Constrained_Mode_MatchingIgnoresCase()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--mode=JSON"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Mode: JSON", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Constrained_Mode_RejectsUnlistedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--mode=yaml"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'yaml' is not valid for option '--mode'", error);
        Assert.Contains("Must be one of: json, xml", error);
    }

    [Fact]
    public async Task Constrained_CaseSensitive_AcceptsExactCase()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--level=Low"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Level: Low", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Constrained_CaseSensitive_RejectsWrongCase()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--level=low"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'low' is not valid for option '--level'", error);
        Assert.Contains("Must be one of: Low, High", error);
    }

    [Fact]
    public async Task Constrained_EnumWithAllowedValues_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--color=Green"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Color: Green", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Constrained_EnumWithAllowedValues_RejectsUnlistedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--color=Blue"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'Blue' is not valid for option '--color'", error);
        Assert.Contains("Must be one of: Red, Green", error);
    }

    [Fact]
    public async Task Array_Strings_BindAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--tags=a", "--tags=b", "--tags=c"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Tags: a,b,c", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Array_Integers_BindAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--scores=1", "--scores=2", "--scores=3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Scores: 1,2,3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Array_Integer_InvalidElement_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--scores=1", "--scores=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Int32 value: 'abc'", error);
    }

    [Fact]
    public async Task Array_AllowedValues_ValidateEachElement()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--formats=json", "--formats=yaml"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'yaml' is not valid for option '--formats'", error);
        Assert.Contains("Must be one of: json, xml", error);
    }

    [Fact]
    public async Task Array_AllowedValues_AcceptsAllListedElements()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--formats=json", "--formats=xml"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Formats: json,xml", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Array_WithoutParser_BindsAsObjects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--blobs=a", "--blobs=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Blobs: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Array_UnsupportedElementType_InvalidElement_ReportsInvalidFormat()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--durations=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value 'abc' for option '--durations'", error);
        Assert.Contains("Invalid TimeSpan value: 'abc'", error);
    }

    [Fact]
    public async Task Array_ParserArrayThrows_FallsBackToBoundValues()
    {
        var builder = CreateBuilder().AddCommandTypeParser<ThrowingArrayObjectParser>();
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["bindchoice", "--blobs=a", "--blobs=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Blobs: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ObjectList_WithoutParser_BindsAsObjects()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder().AddCommand<ObjectListBindCommand>(), ["bindlists", "--blobs=a", "--blobs=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Blobs: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ObjectList_ParserListThrows_FallsBackToBoundValues()
    {
        var builder = CreateBuilder().AddCommand<ObjectListBindCommand>().AddCommandTypeParser<ThrowingArrayObjectParser>();
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["bindlists", "--blobs=a", "--blobs=b"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Blobs: a,b", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Array_BareOptionWithoutValue_ReportsMissing()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindchoice", "--tags"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing value for option: --tags", error);
    }

    [Fact]
    public async Task Argument_StringAndInteger_BindByPosition()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs", "Alice", "3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Name: Alice", output);
        Assert.Contains("Count: 3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_Integer_Invalid_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs", "Alice", "abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value 'abc' for argument 'count'", error);
    }

    [Fact]
    public async Task Argument_EmptyString_PreservesEmpty()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs", string.Empty]);

        Assert.Equal(0, exitCode);
        var nameLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Name:", StringComparison.Ordinal));
        Assert.Equal("Name: ", nameLine);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_Omitted_StaysNull()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Name: null", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_RequiredEmptyString_BindsEmpty()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindrequired", string.Empty]);

        Assert.Equal(0, exitCode);
        var targetLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Target:", StringComparison.Ordinal));
        Assert.Equal("Target: ", targetLine);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_RequiredOmitted_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindrequired"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Missing required argument: target", error);
    }

    [Fact]
    public async Task EmptyString_OptionAndArgument_HaveParity()
    {
        var (optionExit, optionOutput, optionError) = await RunCapturedAsync(["bindprobe", "--text="]);
        var (argumentExit, argumentOutput, argumentError) = await RunCapturedAsync(["bindargs", string.Empty]);

        Assert.Equal(0, optionExit);
        Assert.Equal(0, argumentExit);
        var textLine = optionOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Text:", StringComparison.Ordinal));
        var nameLine = argumentOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Name:", StringComparison.Ordinal));
        Assert.Equal("Text: ", textLine);
        Assert.Equal("Name: ", nameLine);
        Assert.True(string.IsNullOrWhiteSpace(optionError), $"Expected empty stderr but got: {optionError}");
        Assert.True(string.IsNullOrWhiteSpace(argumentError), $"Expected empty stderr but got: {argumentError}");
    }

    [Fact]
    public async Task Argument_AllowedValues_RejectsEmptyString()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs", "Alice", "3", string.Empty]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value '' is not valid for argument 'level'", error);
        Assert.Contains("Must be one of: low, high", error);
    }

    [Fact]
    public async Task Argument_WhitespaceString_BindsVerbatim()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs", " "]);

        Assert.Equal(0, exitCode);
        var nameLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Name:", StringComparison.Ordinal));
        Assert.Equal("Name:  ", nameLine);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_Array_PreservesEmptyElement()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindmany", string.Empty]);

        Assert.Equal(0, exitCode);
        var itemsLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Items:", StringComparison.Ordinal));
        Assert.Equal("Items: ", itemsLine);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_Integer_Empty_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs", "Alice", string.Empty]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value '' for argument 'count'", error);
    }

    [Fact]
    public async Task Argument_Boolean_EmptyValue_BindsTrue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindboolarg", string.Empty]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Flag: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Boolean_EmptyValue_Rejected()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--verbose="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value '' for option '--verbose'", error);
    }

    [Fact]
    public async Task Scalar_Guid_Empty_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--correlation="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Guid value: ''", error);
    }

    [Fact]
    public async Task Scalar_DateTime_Empty_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindprobe", "--not-before="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid DateTime value: ''", error);
    }

    [Fact]
    public async Task RequiredOption_EmptyValue_SatisfiesRequired()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindreqopt", "--label="]);

        Assert.Equal(0, exitCode);
        var labelLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Label:", StringComparison.Ordinal));
        Assert.Equal("Label: ", labelLine);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task RequiredOption_EmptyValueSpaceForm_SatisfiesRequired()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindreqopt", "--label", string.Empty]);

        Assert.Equal(0, exitCode);
        var labelLine = output
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(l => l.StartsWith("Label:", StringComparison.Ordinal));
        Assert.Equal("Label: ", labelLine);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_IntegerArray_EmptyElement_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindnumbers", "1", string.Empty]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value '' for argument 'numbers'", error);
    }

    [Fact]
    public async Task ExplicitEmptyValue_TakesPrecedenceOverEnvironmentValue()
    {
        var prior = Environment.GetEnvironmentVariable(BindEnvConfigVariable);
        Environment.SetEnvironmentVariable(BindEnvConfigVariable, "env-config.json");
        try
        {
            var (exitCode, output, error) = await RunCapturedAsync(["bindenv", "--config", string.Empty]);

            Assert.Equal(0, exitCode);
            Assert.DoesNotContain("env-config.json", output);
            var configLine = output
                .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .FirstOrDefault(l => l.StartsWith("Config:", StringComparison.Ordinal));
            Assert.Equal("Config: ", configLine);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            Environment.SetEnvironmentVariable(BindEnvConfigVariable, prior);
        }
    }

    [Fact]
    public async Task Argument_AllowedValues_AcceptsListedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs", "Alice", "3", "HIGH"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Level: HIGH", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_AllowedValues_RejectsUnlistedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindargs", "Alice", "3", "medium"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'medium' is not valid for argument 'level'", error);
        Assert.Contains("Must be one of: low, high", error);
    }

    [Fact]
    public async Task Argument_Array_BindsAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindmany", "a", "b", "c"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Items: a,b,c", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_IntegerArray_BindsAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindnumbers", "1", "2", "3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Numbers: 1,2,3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_IntegerArray_InvalidElement_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["bindnumbers", "1", "xyz"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value 'xyz' for argument 'numbers'", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("bind-test")
            .SetExecutableTitle("Bind Test")
            .SetExecutableDescription("Value binding verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ScalarBindCommand>()
            .AddCommand<ConstrainedBindCommand>()
            .AddCommand<ArgumentBindCommand>()
            .AddCommand<ArrayArgumentBindCommand>()
            .AddCommand<IntArrayArgumentBindCommand>()
            .AddCommand<BooleanArgumentBindCommand>()
            .AddCommand<RequiredTestCommand>()
            .AddCommand<RequiredOptionBindCommand>()
            .AddCommand<EnvEmptyPrecedenceCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
        => await RunCapturedAsync(CreateBuilder(), args);

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
