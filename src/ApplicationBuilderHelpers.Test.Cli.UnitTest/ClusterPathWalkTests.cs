using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Path-walk cluster routing guards.
/// Pins through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point
/// that a combined short cluster ahead of a subcommand binds at the current
/// node only: with no look-ahead, a cluster the target node does not own ends
/// the walk, so the pending leaf word becomes surplus (<c>Unexpected
/// argument</c>, exit 2). The same cluster after the leaf keeps binding.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ClusterPathWalkTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command(description: "Cluster walk verification root.")]
    public sealed class ClusterWalkRootCommand : Command
    {
        [CommandOption('v', "verbose", Description = "Verbose output.")]
        public bool Verbose { get; set; }

        [CommandOption('c', "count", Description = "Count value.")]
        public int Count { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"root:{Verbose}:{Count}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("slow", "Runs the slow leaf.")]
    public sealed class SlowLeafCommand : Command
    {
        [CommandOption('v', "verbose", Description = "Verbose output.")]
        public bool Verbose { get; set; }

        [CommandOption('c', "count", Description = "Count value.")]
        public int Count { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"slow:{Verbose}:{Count}");
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Leaf-only cluster walk verification root.")]
    public sealed class LeafOnlyWalkRootCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("leafroot");
            return ValueTask.CompletedTask;
        }
    }

    [Command("beta", "Runs the beta leaf.")]
    public sealed class BetaLeafCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("beta");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task SpaceValuedCluster_BeforeLeaf_ReportsSurplusChild()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["-vc", "5", "slow"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unexpected argument 'slow'", error);
        Assert.Contains("Run 'cluster-walk-test --help' for more information on available commands and options.", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
    }

    [Fact]
    public async Task RepeatedFlagCluster_BeforeLeaf_ReportsSurplusChild()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["-vv", "slow"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unexpected argument 'slow'", error);
        Assert.Contains("Run 'cluster-walk-test --help' for more information on available commands and options.", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
    }

    [Fact]
    public async Task AttachedValuedCluster_BeforeLeaf_ReportsSurplusChild()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["-vc5", "slow"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unexpected argument 'slow'", error);
        Assert.Contains("Run 'cluster-walk-test --help' for more information on available commands and options.", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
    }

    [Fact]
    public async Task SpaceValuedCluster_AfterLeaf_BindsOnLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["slow", "-vc", "5"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("slow:True:5", output);
        Assert.DoesNotContain("root:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task PreChildCluster_BeforeLeaf_ReportsSurplusChild()
    {
        var (exitCode, output, error) = await RunCapturedLeafOnlyAsync(["-vc", "5", "slow"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unexpected argument 'slow'", error);
        Assert.Contains("Run 'cluster-walk-test --help' for more information on available commands and options.", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
    }

    [Fact]
    public async Task PreChildAttachedCluster_BeforeLeaf_ReportsSurplusChild()
    {
        var (exitCode, output, error) = await RunCapturedLeafOnlyAsync(["-vc5", "slow"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unexpected argument 'slow'", error);
        Assert.Contains("Run 'cluster-walk-test --help' for more information on available commands and options.", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
    }

    [Fact]
    public async Task OwnedCluster_BeforeUnowningLeaf_ReportsSurplusChild()
    {
        // Nuke re-pin (rebase onto #688): with no look-ahead the walk never
        // leaves the root, so a root-owned cluster binds at the root and the
        // following leaf word is surplus — even when that leaf owns neither
        // letter. The pre-nuke "Unknown option on the leaf" expectation
        // depended on TryConsumeClusterInWalk, which the nuke deletes.
        var (exitCode, output, error) = await RunCapturedDisagreeAsync(["-vc", "5", "beta"]);

        Assert.Equal(2, exitCode);
        Assert.Contains("Unexpected argument 'beta'", error);
        Assert.Contains("Run 'cluster-walk-test --help' for more information on available commands and options.", error);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("cluster-walk-test")
            .SetExecutableTitle("Cluster Walk Test")
            .SetExecutableDescription("Cluster path-walk verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ClusterWalkRootCommand>()
            .AddCommand<SlowLeafCommand>();
    }

    private static ApplicationBuilder CreateLeafOnlyBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("cluster-walk-test")
            .SetExecutableTitle("Cluster Walk Test")
            .SetExecutableDescription("Cluster path-walk verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<LeafOnlyWalkRootCommand>()
            .AddCommand<SlowLeafCommand>();
    }

    private static ApplicationBuilder CreateDisagreeBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("cluster-walk-test")
            .SetExecutableTitle("Cluster Walk Test")
            .SetExecutableDescription("Cluster path-walk verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ClusterWalkRootCommand>()
            .AddCommand<SlowLeafCommand>()
            .AddCommand<BetaLeafCommand>();
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

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedLeafOnlyAsync(string[] args)
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
                var exitCode = await CreateLeafOnlyBuilder().RunAsync(args);
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

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedDisagreeAsync(string[] args)
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
                var exitCode = await CreateDisagreeBuilder().RunAsync(args);
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
