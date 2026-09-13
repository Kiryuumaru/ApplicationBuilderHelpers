using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Matrix for issue #387 (type pipeline: single registry,
/// <see cref="IEnumerable{T}"/> binder, 6 scalars, FromAmong convert-then-compare).
/// Exercises the desired end-state through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point.
/// All tests below PASS on current code (scalars resolve through the
/// registered type-parser pipeline, <c>List{T}</c>/<c>IEnumerable{T}</c> bind
/// like arrays, FromAmong compares converted values, nullable arguments unwrap).
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class TypePipelineScalarMatrixTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    public enum MatrixColor
    {
        Red,
        Green,
        Blue
    }

    [Command("typematrix", "Probes the six scalar pipeline types.")]
    public sealed class ScalarMatrixCommand : Command
    {
        [CommandOption("delay", Description = "Delay value.")]
        public TimeSpan Delay { get; set; }

        [CommandOption("endpoint", Description = "Endpoint value.")]
        public Uri? Endpoint { get; set; }

        [CommandOption("api-version", Description = "API version value.")]
        public Version? ApiVersion { get; set; }

        [CommandOption("start-date", Description = "Start date value.")]
        public DateOnly StartDate { get; set; }

        [CommandOption("start-time", Description = "Start time value.")]
        public TimeOnly StartTime { get; set; }

        [CommandOption("input-file", Description = "Input file value.")]
        public FileInfo? InputFile { get; set; }

        [CommandOption("maybe-delay", Description = "Optional delay value.")]
        public TimeSpan? MaybeDelay { get; set; }

        [CommandOption("maybe-endpoint", Description = "Optional endpoint value.")]
        public Uri? MaybeEndpoint { get; set; }

        [CommandOption("maybe-api-version", Description = "Optional API version value.")]
        public Version? MaybeApiVersion { get; set; }

        [CommandOption("maybe-start-date", Description = "Optional start date value.")]
        public DateOnly? MaybeStartDate { get; set; }

        [CommandOption("maybe-start-time", Description = "Optional start time value.")]
        public TimeOnly? MaybeStartTime { get; set; }

        [CommandOption("maybe-input-file", Description = "Optional input file value.")]
        public FileInfo? MaybeInputFile { get; set; }

        [CommandOption("delays", Description = "Delay values.")]
        public TimeSpan[]? Delays { get; set; }

        [CommandOption("endpoints", Description = "Endpoint values.")]
        public Uri[]? Endpoints { get; set; }

        [CommandOption("api-versions", Description = "API version values.")]
        public Version[]? ApiVersions { get; set; }

        [CommandOption("start-dates", Description = "Start date values.")]
        public DateOnly[]? StartDates { get; set; }

        [CommandOption("start-times", Description = "Start time values.")]
        public TimeOnly[]? StartTimes { get; set; }

        [CommandOption("input-files", Description = "Input file values.")]
        public FileInfo[]? InputFiles { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Delay: {Delay}");
            Console.WriteLine($"Endpoint: {Endpoint?.ToString() ?? "null"}");
            Console.WriteLine($"ApiVersion: {ApiVersion?.ToString() ?? "null"}");
            Console.WriteLine($"StartDate: {StartDate:yyyy-MM-dd}");
            Console.WriteLine($"StartTime: {StartTime:HH:mm}");
            Console.WriteLine($"InputFile: {InputFile?.Name ?? "null"}");
            Console.WriteLine($"MaybeDelay: {MaybeDelay?.ToString() ?? "null"}");
            Console.WriteLine($"MaybeEndpoint: {MaybeEndpoint?.ToString() ?? "null"}");
            Console.WriteLine($"MaybeApiVersion: {MaybeApiVersion?.ToString() ?? "null"}");
            Console.WriteLine($"MaybeStartDate: {MaybeStartDate?.ToString("yyyy-MM-dd") ?? "null"}");
            Console.WriteLine($"MaybeStartTime: {MaybeStartTime?.ToString("HH:mm") ?? "null"}");
            Console.WriteLine($"MaybeInputFile: {MaybeInputFile?.Name ?? "null"}");
            Console.WriteLine($"Delays: {(Delays is null ? "null" : string.Join(",", Delays))}");
            Console.WriteLine($"Endpoints: {(Endpoints is null ? "null" : string.Join(",", Endpoints.Select(u => u.ToString())))}");
            Console.WriteLine($"ApiVersions: {(ApiVersions is null ? "null" : string.Join(",", ApiVersions.Select(v => v.ToString())))}");
            Console.WriteLine($"StartDates: {(StartDates is null ? "null" : string.Join(",", StartDates.Select(d => d.ToString("yyyy-MM-dd"))))}");
            Console.WriteLine($"StartTimes: {(StartTimes is null ? "null" : string.Join(",", StartTimes.Select(t => t.ToString("HH:mm"))))}");
            Console.WriteLine($"InputFiles: {(InputFiles is null ? "null" : string.Join(",", InputFiles.Select(f => f.Name)))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("listparity", "Probes List/IEnumerable parity with arrays.")]
    public sealed class CollectionParityCommand : Command
    {
        [CommandOption("scores-array", Description = "Scores as array.")]
        public int[]? ScoresArray { get; set; }

        [CommandOption("scores-list", Description = "Scores as list.")]
        public List<int>? ScoresList { get; set; }

        [CommandOption("scores-enumerable", Description = "Scores as enumerable.")]
        public IEnumerable<int>? ScoresEnumerable { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"ScoresArray: {(ScoresArray is null ? "null" : string.Join(",", ScoresArray))}");
            Console.WriteLine($"ScoresList: {(ScoresList is null ? "null" : string.Join(",", ScoresList))}");
            Console.WriteLine($"ScoresEnumerable: {(ScoresEnumerable is null ? "null" : string.Join(",", ScoresEnumerable))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceprobe", "Probes typed FromAmong entries on options.")]
    public sealed class ChoiceMatrixCommand : Command
    {
        [CommandOption("level", Description = "Level choice.", FromAmong = [1, 2, 3])]
        public int Level { get; set; }

        [CommandOption("color", Description = "Color choice.", FromAmong = [MatrixColor.Red, MatrixColor.Green])]
        public MatrixColor Color { get; set; }

        [CommandOption("delay", Description = "Delay choice.", FromAmong = ["01:00:00", "02:00:00"])]
        public TimeSpan Delay { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"Color: {Color}");
            Console.WriteLine($"Delay: {Delay}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceargs", "Probes typed FromAmong entries on arguments.")]
    public sealed class ChoiceArgumentCommand : Command
    {
        [CommandArgument("level", Description = "Level choice.", Position = 0, FromAmong = [1, 2, 3])]
        public int Level { get; set; }

        [CommandArgument("color", Description = "Color choice.", Position = 1, FromAmong = [MatrixColor.Red, MatrixColor.Green])]
        public MatrixColor Color { get; set; }

        [CommandArgument("delay", Description = "Delay choice.", Position = 2, FromAmong = ["01:00:00", "02:00:00"])]
        public TimeSpan Delay { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Level: {Level}");
            Console.WriteLine($"Color: {Color}");
            Console.WriteLine($"Delay: {Delay}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("nullablearg", "Probes nullable argument conversion.")]
    public sealed class NullableArgumentCommand : Command
    {
        [CommandArgument("count", Description = "Optional count.", Position = 0)]
        public int? Count { get; set; }

        [CommandArgument("delay", Description = "Optional delay.", Position = 1)]
        public TimeSpan? Delay { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Count: {Count?.ToString() ?? "null"}");
            Console.WriteLine($"Delay: {Delay?.ToString() ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    // ---- Per-scalar valid binding (registry, not ChangeType) ----

    [Fact]
    public async Task Scalar_TimeSpan_Valid_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--delay=01:30:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Delay: 01:30:00", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Uri_Valid_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--endpoint=https://example.com/api"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Endpoint: https://example.com/api", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Version_Valid_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--api-version=1.2.3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("ApiVersion: 1.2.3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_DateOnly_Valid_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--start-date=2024-01-15"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("StartDate: 2024-01-15", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_TimeOnly_Valid_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--start-time=12:30"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("StartTime: 12:30", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_FileInfo_Valid_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--input-file=data.txt"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("InputFile: data.txt", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // ---- Per-scalar invalid binding (registry-style error, no ChangeType fallthrough) ----

    [Fact]
    public async Task Scalar_TimeSpan_Invalid_ReportsRegistryError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--delay=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid TimeSpan value: 'abc'", error);
    }

    [Fact]
    public async Task Scalar_Uri_Invalid_ReportsRegistryError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--endpoint=:::not a uri"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Uri value: ':::not a uri'", error);
    }

    [Fact]
    public async Task Scalar_Version_Invalid_ReportsRegistryError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--api-version=not-a-version"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Version value: 'not-a-version'", error);
    }

    [Fact]
    public async Task Scalar_DateOnly_Invalid_ReportsRegistryError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--start-date=not-a-date"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid DateOnly value: 'not-a-date'", error);
    }

    [Fact]
    public async Task Scalar_TimeOnly_Invalid_ReportsRegistryError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--start-time=not-a-time"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid TimeOnly value: 'not-a-time'", error);
    }

    [Fact]
    public async Task Scalar_FileInfo_Invalid_ReportsRegistryError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--input-file="]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid FileInfo value", error);
    }

    // ---- Per-scalar nullable binding ----

    [Fact]
    public async Task Scalar_TimeSpan_Nullable_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--maybe-delay=01:00:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("MaybeDelay: 01:00:00", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Uri_Nullable_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--maybe-endpoint=https://example.com/api"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("MaybeEndpoint: https://example.com/api", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Version_Nullable_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--maybe-api-version=1.2.3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("MaybeApiVersion: 1.2.3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_DateOnly_Nullable_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--maybe-start-date=2024-01-15"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("MaybeStartDate: 2024-01-15", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_TimeOnly_Nullable_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--maybe-start-time=12:30"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("MaybeStartTime: 12:30", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_FileInfo_Nullable_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--maybe-input-file=data.txt"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("MaybeInputFile: data.txt", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // ---- Per-scalar array binding ----

    [Fact]
    public async Task Scalar_TimeSpan_Array_BindsAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--delays=01:00:00", "--delays=02:00:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Delays: 01:00:00,02:00:00", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Uri_Array_BindsAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--endpoints=https://a.example/", "--endpoints=https://b.example/"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Endpoints: https://a.example/,https://b.example/", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_Version_Array_BindsAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--api-versions=1.2.3", "--api-versions=4.5.6"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("ApiVersions: 1.2.3,4.5.6", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_DateOnly_Array_BindsAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--start-dates=2024-01-15", "--start-dates=2024-02-20"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("StartDates: 2024-01-15,2024-02-20", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_TimeOnly_Array_BindsAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--start-times=08:00", "--start-times=12:30"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("StartTimes: 08:00,12:30", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_FileInfo_Array_BindsAllValues()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--input-files=a.txt", "--input-files=b.txt"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("InputFiles: a.txt,b.txt", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Scalar_TimeSpan_Array_InvalidElement_ReportsRegistryError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["typematrix", "--delays=01:00:00", "--delays=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid TimeSpan value: 'abc'", error);
    }

    // ---- List / IEnumerable parity with arrays ----

    [Fact]
    public async Task Collection_List_BindsAllValuesLikeArray()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["listparity", "--scores-list=1", "--scores-list=2", "--scores-list=3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("ScoresList: 1,2,3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Collection_Enumerable_BindsAllValuesLikeArray()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["listparity", "--scores-enumerable=1", "--scores-enumerable=2", "--scores-enumerable=3"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("ScoresEnumerable: 1,2,3", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Collection_List_InvalidElement_ReportsError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["listparity", "--scores-list=1", "--scores-list=abc"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Int32 value: 'abc'", error);
    }

    // ---- FromAmong convert-then-compare on the option path ----

    [Fact]
    public async Task FromAmong_IntOption_EquivalentRepresentation_Accepts()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["choiceprobe", "--level=02", "--color=Red", "--delay=01:00:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Level: 2", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FromAmong_EnumOption_NumericRepresentation_Accepts()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["choiceprobe", "--level=1", "--color=0", "--delay=01:00:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Color: Red", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FromAmong_TimeSpanOption_EquivalentRepresentation_Accepts()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["choiceprobe", "--level=1", "--color=Red", "--delay=1:00:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Delay: 01:00:00", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // ---- FromAmong convert-then-compare on the argument path ----

    [Fact]
    public async Task FromAmong_IntArgument_EquivalentRepresentation_Accepts()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["choiceargs", "02", "Red", "01:00:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Level: 2", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FromAmong_EnumArgument_NumericRepresentation_Accepts()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["choiceargs", "1", "0", "01:00:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Color: Red", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task FromAmong_TimeSpanArgument_EquivalentRepresentation_Accepts()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["choiceargs", "1", "Red", "1:00:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Delay: 01:00:00", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // ---- Nullable argument conversion ----

    [Fact]
    public async Task Argument_NullableInt_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["nullablearg", "7"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Count: 7", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Argument_NullableTimeSpan_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["nullablearg", "7", "01:30:00"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Count: 7", output);
        Assert.Contains("Delay: 01:30:00", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // ---- No-ChangeType gate for known scalars (registry owns these types) ----

    [Theory]
    [InlineData("typematrix", "--delay=abc")]
    [InlineData("typematrix", "--endpoint=:::not a uri")]
    [InlineData("typematrix", "--api-version=not-a-version")]
    [InlineData("typematrix", "--start-date=not-a-date")]
    [InlineData("typematrix", "--start-time=not-a-time")]
    [InlineData("typematrix", "--input-file=")]
    public async Task KnownScalars_InvalidValue_NeverFallsThroughToChangeType(string command, string option)
    {
        var (exitCode, output, error) = await RunCapturedAsync([command, option]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.DoesNotContain("Invalid format", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("typepipeline-test")
            .SetExecutableTitle("Type Pipeline Test")
            .SetExecutableDescription("Type pipeline verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ScalarMatrixCommand>()
            .AddCommand<CollectionParityCommand>()
            .AddCommand<ChoiceMatrixCommand>()
            .AddCommand<ChoiceArgumentCommand>()
            .AddCommand<NullableArgumentCommand>();
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
