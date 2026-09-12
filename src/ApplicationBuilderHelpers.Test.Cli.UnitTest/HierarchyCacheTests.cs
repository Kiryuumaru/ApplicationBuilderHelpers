using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process hierarchy-cache tests for repeated runs on one builder.
/// Exercises the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point with the same builder instance across runs: repeated runs stay
/// correct, commands added between runs are visible, bound values do not leak
/// across runs, and type parsers added between runs apply to later runs.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HierarchyCacheTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("cacheprobe", "Probes repeated same-builder runs.")]
    public sealed class CacheProbeCommand : Command
    {
        [CommandOption("text", Description = "Text value.")]
        public string? Text { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Text: {Text ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("lated", "Probes late registration between runs.")]
    public sealed class LateRegisteredCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("late ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("cachedur", "Probes late parser registration between runs.")]
    public sealed class CacheDurationCommand : Command
    {
        [CommandOption("duration", Description = "Duration value.")]
        public TimeSpan Duration { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Duration: {Duration}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("instprobe", "Probes instance-registration identity across runs.")]
    public sealed class InstanceIdentityProbeCommand : Command
    {
        public static readonly List<InstanceIdentityProbeCommand> RunInstances = [];

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            RunInstances.Add(this);
            Console.WriteLine("instance ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    public enum CacheSeverity
    {
        Low,
        High
    }

    [Command("cachesev", "Probes late enum parser registration between runs.")]
    public sealed class CacheSeverityCommand : Command
    {
        [CommandOption("level", Description = "Severity value.")]
        public CacheSeverity Level { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Level: {Level}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Custom <see cref="CacheSeverity"/> parser registered between runs through
    /// the public <c>AddCommandTypeParser</c> entry point (see
    /// <c>docs/custom-type-parsers.md</c>). Accepts the out-of-enum token
    /// <c>Custom</c>, mapping it onto <see cref="CacheSeverity.High"/>,
    /// alongside the declared enum names.
    /// </summary>
    public sealed class CacheSeverityParser : ICommandTypeParser
    {
        public Type Type => typeof(CacheSeverity);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            if (string.Equals(value, "Custom", StringComparison.OrdinalIgnoreCase))
                return CacheSeverity.High;
            if (Enum.TryParse<CacheSeverity>(value, ignoreCase: true, out var result))
                return result;
            validateError = $"'{value}' is not a valid severity";
            return null;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => CacheSeverity.Low;

        public Array CreateTypedArray(int length) => new CacheSeverity[length];
    }

    /// <summary>
    /// Custom <see cref="TimeSpan"/> parser registered between runs through the
    /// public <c>AddCommandTypeParser</c> entry point (see
    /// <c>docs/custom-type-parsers.md</c>).
    /// </summary>
    public sealed class CacheTimeSpanParser : ICommandTypeParser
    {
        public Type Type => typeof(TimeSpan);

        public object? Parse(string? value, out string? validateError)
        {
            validateError = null;
            if (TimeSpan.TryParse(value, out var result))
                return result;
            validateError = $"'{value}' is not a valid time span";
            return null;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => TimeSpan.Zero;

        public Array CreateTypedArray(int length) => new TimeSpan[length];
    }

    [Fact]
    public async Task RepeatedRuns_OnSameBuilder_StayCorrect()
    {
        var builder = CreateBuilder().AddCommand<CacheProbeCommand>();

        var first = await RunCapturedAsync(builder, ["cacheprobe", "--text=one"]);
        var second = await RunCapturedAsync(builder, ["cacheprobe", "--text=two"]);
        var third = await RunCapturedAsync(builder, ["cacheprobe", "--text=three"]);

        Assert.Equal(0, first.ExitCode);
        Assert.Contains("Text: one", first.Output);
        Assert.True(string.IsNullOrWhiteSpace(first.Error), $"Expected empty stderr but got: {first.Error}");
        Assert.Equal(0, second.ExitCode);
        Assert.Contains("Text: two", second.Output);
        Assert.True(string.IsNullOrWhiteSpace(second.Error), $"Expected empty stderr but got: {second.Error}");
        Assert.Equal(0, third.ExitCode);
        Assert.Contains("Text: three", third.Output);
        Assert.True(string.IsNullOrWhiteSpace(third.Error), $"Expected empty stderr but got: {third.Error}");
    }

    [Fact]
    public async Task AddedCommands_AreVisibleToLaterRuns()
    {
        var builder = CreateBuilder().AddCommand<CacheProbeCommand>();

        var first = await RunCapturedAsync(builder, ["cacheprobe", "--text=one"]);

        Assert.Equal(0, first.ExitCode);
        Assert.Contains("Text: one", first.Output);

        builder.AddCommand<LateRegisteredCommand>();

        var second = await RunCapturedAsync(builder, ["lated"]);

        Assert.Equal(0, second.ExitCode);
        Assert.Contains("late ran", second.Output);
        Assert.True(string.IsNullOrWhiteSpace(second.Error), $"Expected empty stderr but got: {second.Error}");
    }

    [Fact]
    public async Task SequentialRuns_DoNotLeakBoundValues()
    {
        var builder = CreateBuilder().AddCommand<CacheProbeCommand>();

        var first = await RunCapturedAsync(builder, ["cacheprobe", "--text=alpha"]);

        Assert.Equal(0, first.ExitCode);
        Assert.Contains("Text: alpha", first.Output);

        var second = await RunCapturedAsync(builder, ["cacheprobe"]);

        Assert.Equal(0, second.ExitCode);
        Assert.Contains("Text: null", second.Output);
        Assert.True(string.IsNullOrWhiteSpace(second.Error), $"Expected empty stderr but got: {second.Error}");
    }

    [Fact]
    public async Task InstanceRegistrations_ReuseSameReferenceAcrossRuns()
    {
        InstanceIdentityProbeCommand.RunInstances.Clear();
        var registered = new InstanceIdentityProbeCommand();
        var builder = CreateBuilder();
        builder.AddCommand(registered);

        var first = await RunCapturedAsync(builder, ["instprobe"]);
        var second = await RunCapturedAsync(builder, ["instprobe"]);

        Assert.Equal(0, first.ExitCode);
        Assert.Equal(0, second.ExitCode);
        Assert.Equal(2, InstanceIdentityProbeCommand.RunInstances.Count);
        Assert.All(InstanceIdentityProbeCommand.RunInstances, instance => Assert.Same(registered, instance));
    }

    [Fact]
    public async Task AddedTypeParsers_ApplyToLaterRuns()
    {
        var builder = CreateBuilder().AddCommand<CacheDurationCommand>();

        var first = await RunCapturedAsync(builder, ["cachedur", "--duration=01:02:03"]);

        Assert.Equal(2, first.ExitCode);
        Assert.Contains("Invalid format for value '01:02:03' of type System.TimeSpan", first.Error);

        builder.AddCommandTypeParser<CacheTimeSpanParser>();

        var second = await RunCapturedAsync(builder, ["cachedur", "--duration=01:02:03"]);

        Assert.Equal(0, second.ExitCode);
        Assert.Contains("Duration: 01:02:03", second.Output);
        Assert.True(string.IsNullOrWhiteSpace(second.Error), $"Expected empty stderr but got: {second.Error}");
    }

    [Fact]
    public async Task AddedEnumParsers_ApplyToLaterRuns()
    {
        var builder = CreateBuilder().AddCommand<CacheSeverityCommand>();

        var first = await RunCapturedAsync(builder, ["cachesev", "--level=Custom"]);

        Assert.Equal(2, first.ExitCode);
        Assert.Contains("Value 'Custom' is not valid for option '--level'", first.Error);
        Assert.Contains("Must be one of:", first.Error);

        builder.AddCommandTypeParser<CacheSeverityParser>();

        var second = await RunCapturedAsync(builder, ["cachesev", "--level=Custom"]);

        Assert.Equal(0, second.ExitCode);
        Assert.Contains("Level: High", second.Output);
        Assert.True(string.IsNullOrWhiteSpace(second.Error), $"Expected empty stderr but got: {second.Error}");
    }

    [Fact]
    public async Task RepeatedRuns_BuildDescriptorsOncePerType()
    {
        var builder = CreateBuilder().AddCommand<CacheProbeCommand>();

        var first = await RunCapturedAsync(builder, ["cacheprobe", "--text=one"]);
        var second = await RunCapturedAsync(builder, ["cacheprobe", "--text=two"]);
        var third = await RunCapturedAsync(builder, ["cacheprobe", "--text=three"]);

        Assert.Equal(0, first.ExitCode);
        Assert.Contains("Text: one", first.Output);
        Assert.Equal(0, second.ExitCode);
        Assert.Contains("Text: two", second.Output);
        Assert.Equal(0, third.ExitCode);
        Assert.Contains("Text: three", third.Output);

        Assert.Equal(1, builder.ReflectionBuildCount);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("cache-test")
            .SetExecutableTitle("Cache Test")
            .SetExecutableDescription("Hierarchy cache verification CLI.")
            .SetExecutableVersion("9.9.9");
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
