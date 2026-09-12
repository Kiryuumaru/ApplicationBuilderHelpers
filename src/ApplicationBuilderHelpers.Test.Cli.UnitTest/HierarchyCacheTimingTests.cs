using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using Xunit.Abstractions;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-test timing harness for the S9 benchmark gate.
/// Builds a 10-command tree (root + varied subcommands mirroring
/// <c>Test.Cli/Commands</c>: single-level leaves, a <c>config get</c> /
/// <c>config set</c> pair behind an abstract intermediate, and a
/// <c>remote add</c> leaf behind an auto-created intermediate). Every command
/// carries 3 options + 1 argument: one inherited <c>--verbose</c> from the
/// shared base (so global detection has exactly one candidate), one
/// <see cref="LogLevel"/> enum option, and one leaf-specific option.
/// Exercises a root help run, a deep leaf run, a deep error run, plus N=50
/// repeated same-builder runs measuring wall time and calling-thread
/// allocated bytes (<see cref="GC.GetAllocatedBytesForCurrentThread"/>).
/// Gates only on descriptor build-count (BuildCount == 10); timings are
/// reported, never asserted, so the gate stays non-flaky.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HierarchyCacheTimingTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);
    private const int RepeatCount = 50;

    private static readonly string[] HelpArgs = ["--help"];
    private static readonly string[] LeafArgs = ["config", "get", "mykey"];
    private static readonly string[] ErrorArgs = ["config", "get", "mykey", "--get-level=Bogus"];

    private readonly ITestOutputHelper _output;

    public HierarchyCacheTimingTests(ITestOutputHelper output)
    {
        _output = output;
    }

    public abstract class BenchBaseCommand : Command
    {
        [CommandOption('v', "verbose", Description = "Enable verbose output.")]
        public bool Verbose { get; set; }
    }

    [Command("config", "Bench configuration values.")]
    public abstract class BenchConfigBase : BenchBaseCommand
    {
    }

    [Command(description: "Bench root command.")]
    public sealed class BenchRootCommand : BenchBaseCommand
    {
        [CommandOption("root-level", Description = "Root log level.")]
        public LogLevel RootLevel { get; set; } = LogLevel.Information;

        [CommandOption("root-config", Description = "Root config path.")]
        public string? RootConfig { get; set; }

        [CommandArgument("root-target", Description = "Root target.", Position = 0)]
        public string? RootTarget { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"bench root:{RootTarget ?? "default"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("build", "Build the bench project.")]
    public sealed class BenchBuildCommand : BenchBaseCommand
    {
        [CommandOption("build-level", Description = "Build log level.")]
        public LogLevel BuildLevel { get; set; } = LogLevel.Information;

        [CommandOption("build-output", Description = "Build output directory.")]
        public string? BuildOutput { get; set; }

        [CommandArgument("project", Description = "Project file to build.", Position = 0, Required = true)]
        public required string Project { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"bench build:{Project}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("serve", "Serve the bench app.")]
    public sealed class BenchServeCommand : BenchBaseCommand
    {
        [CommandOption("serve-level", Description = "Serve log level.")]
        public LogLevel ServeLevel { get; set; } = LogLevel.Information;

        [CommandOption("serve-port", Description = "Port to serve on.")]
        public int ServePort { get; set; } = 5000;

        [CommandArgument("target", Description = "Target to serve.", Position = 0)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"bench serve:{Target ?? "default"}:{ServePort}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("test", "Run bench tests.")]
    public sealed class BenchTestCommand : BenchBaseCommand
    {
        [CommandOption("test-level", Description = "Test log level.")]
        public LogLevel TestLevel { get; set; } = LogLevel.Information;

        [CommandOption("test-filter", Description = "Test filter expression.")]
        public string? TestFilter { get; set; }

        [CommandArgument("target", Description = "Target to test.", Position = 0)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"bench test:{Target ?? "default"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy", "Deploy the bench app.")]
    public sealed class BenchDeployCommand : BenchBaseCommand
    {
        [CommandOption("deploy-level", Description = "Deploy log level.")]
        public LogLevel DeployLevel { get; set; } = LogLevel.Information;

        [CommandOption("deploy-env", Description = "Deployment environment.")]
        public string? DeployEnv { get; set; }

        [CommandArgument("target", Description = "Target to deploy.", Position = 0)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"bench deploy:{Target ?? "default"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("plugin", "Manage bench plugins.")]
    public sealed class BenchPluginCommand : BenchBaseCommand
    {
        [CommandOption("plugin-level", Description = "Plugin log level.")]
        public LogLevel PluginLevel { get; set; } = LogLevel.Information;

        [CommandOption("plugin-source", Description = "Plugin source feed.")]
        public string? PluginSource { get; set; }

        [CommandArgument("plugin", Description = "Plugin to manage.", Position = 0)]
        public string? Plugin { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"bench plugin:{Plugin ?? "default"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("enum-test", "Bench enum probe with limited choices.")]
    public sealed class BenchEnumCommand : BenchBaseCommand
    {
        [CommandOption("limited-level", Description = "Limited log level.", FromAmong = ["Trace", "Debug", "Information"])]
        public LogLevel LimitedLevel { get; set; } = LogLevel.Information;

        [CommandOption("details", Description = "Show enum details.")]
        public bool Details { get; set; }

        [CommandArgument("target", Description = "Target to process.", Position = 0)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"bench enum:{Target ?? "default"}:{LimitedLevel}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config get", "Get bench configuration values.")]
    public sealed class BenchConfigGetCommand : BenchConfigBase
    {
        [CommandOption("get-level", Description = "Get log level.")]
        public LogLevel GetLevel { get; set; } = LogLevel.Information;

        [CommandOption("get-all", Description = "Show all configuration values.")]
        public bool GetAll { get; set; }

        [CommandArgument("key", Description = "Configuration key to retrieve.", Position = 0)]
        public string? Key { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"config get:{Key ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config set", "Set bench configuration values.")]
    public sealed class BenchConfigSetCommand : BenchConfigBase
    {
        [CommandOption("set-level", Description = "Set log level.")]
        public LogLevel SetLevel { get; set; } = LogLevel.Information;

        [CommandOption("set-value", Description = "Configuration value to set.")]
        public string? SetValue { get; set; }

        [CommandArgument("key", Description = "Configuration key to set.", Position = 0, Required = true)]
        public required string Key { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"config set:{Key}={SetValue ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("remote add", "Add a bench remote.")]
    public sealed class BenchRemoteAddCommand : BenchBaseCommand
    {
        [CommandOption("remote-level", Description = "Remote log level.")]
        public LogLevel RemoteLevel { get; set; } = LogLevel.Information;

        [CommandOption("remote-fetch", Description = "Fetch the remote immediately after adding.")]
        public bool RemoteFetch { get; set; }

        [CommandArgument("name", Description = "Remote name.", Position = 0, Required = true)]
        public required string Name { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"remote add:{Name}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task S9Gate_TimingProbe_ReportsColdVsWarm()
    {
        // Correctness phase on one builder: help, deep leaf, deep error.
        var builder = CreateBuilder();

        var help = await TimedRunCapturedAsync(builder, HelpArgs);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("USAGE:", help.Output);
        Assert.Contains("build", help.Output);
        Assert.Contains("config", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var leaf = await TimedRunCapturedAsync(builder, LeafArgs);
        Assert.Equal(0, leaf.ExitCode);
        Assert.Contains("config get:mykey", leaf.Output);
        Assert.True(string.IsNullOrWhiteSpace(leaf.Error), $"Expected empty stderr but got: {leaf.Error}");

        var error = await TimedRunCapturedAsync(builder, ErrorArgs);
        Assert.Equal(2, error.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(error.Output), $"Expected empty stdout but got: {error.Output}");
        Assert.Contains("Value 'Bogus' is not valid for option '--get-level'", error.Error);
        Assert.Contains("Must be one of:", error.Error);

        Assert.Equal(10, GetBuildCount(builder));

        // Timing phase on a fresh builder so iteration 0 is genuinely cold
        // (reflection cache empty) and iterations 1..N-1 are warm (cache hits).
        var timingBuilder = CreateBuilder();
        var elapsedMs = new double[RepeatCount];
        var allocatedBytes = new long[RepeatCount];

        for (var i = 0; i < RepeatCount; i++)
        {
            var run = await TimedRunCapturedAsync(timingBuilder, LeafArgs);
            Assert.Equal(0, run.ExitCode);
            Assert.Contains("config get:mykey", run.Output);
            elapsedMs[i] = run.Elapsed.TotalMilliseconds;
            allocatedBytes[i] = run.AllocatedBytes;
        }

        // Gate on build-count only: one descriptor build per command type,
        // reused across all 50 same-builder runs. No wall-time assertions.
        Assert.Equal(10, GetBuildCount(timingBuilder));

        var warmMs = elapsedMs.Skip(1).ToArray();
        var warmBytes = allocatedBytes.Skip(1).Select(b => (double)b).ToArray();
        var report =
            $"[BENCH] s9-gate commands=10 runs={RepeatCount} " +
            $"cold={elapsedMs[0]:F2}ms warm_avg={warmMs.Average():F2}ms " +
            $"warm_min={warmMs.Min():F2}ms warm_max={warmMs.Max():F2}ms " +
            $"total={elapsedMs.Sum():F2}ms " +
            $"alloc_cold={allocatedBytes[0]}B alloc_warm_avg={warmBytes.Average():F0}B " +
            $"alloc_warm_max={(long)warmBytes.Max()}B " +
            $"buildcount={GetBuildCount(timingBuilder)} " +
            $"(calling-thread bytes; async continuations may allocate on other threads)";
        _output.WriteLine(report);
        Console.WriteLine(report);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("bench-test")
            .SetExecutableTitle("Bench Test")
            .SetExecutableDescription("S9 benchmark gate verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<BenchRootCommand>()
            .AddCommand<BenchBuildCommand>()
            .AddCommand<BenchServeCommand>()
            .AddCommand<BenchTestCommand>()
            .AddCommand<BenchDeployCommand>()
            .AddCommand<BenchPluginCommand>()
            .AddCommand<BenchEnumCommand>()
            .AddCommand<BenchConfigGetCommand>()
            .AddCommand<BenchConfigSetCommand>()
            .AddCommand<BenchRemoteAddCommand>();
    }

    private static int GetBuildCount(ApplicationBuilder builder)
    {
        return builder.ReflectionBuildCount;
    }

    private static async Task<(int ExitCode, string Output, string Error, TimeSpan Elapsed, long AllocatedBytes)> TimedRunCapturedAsync(
        ApplicationBuilder builder, string[] args)
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
                var startBytes = GC.GetAllocatedBytesForCurrentThread();
                var stopwatch = Stopwatch.StartNew();
                var exitCode = await builder.RunAsync(args);
                stopwatch.Stop();
                var endBytes = GC.GetAllocatedBytesForCurrentThread();
                outWriter.Flush();
                errorWriter.Flush();
                return (exitCode, outWriter.ToString(), errorWriter.ToString(), stopwatch.Elapsed, endBytes - startBytes);
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
