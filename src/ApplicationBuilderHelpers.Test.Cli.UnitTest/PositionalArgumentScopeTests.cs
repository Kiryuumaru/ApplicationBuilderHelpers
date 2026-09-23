using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process positional-argument scope tests.
/// Pins the per-command positional contract through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// a root positional is leaf-local only, so a leaf never accepts the root
/// value, never lists it in leaf help, and never collides with a leaf-local
/// positional of the same name at any depth.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class PositionalArgumentScopeTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command(description: "Scope verification root.")]
    public sealed class ScopeRootCommand : Command
    {
        [CommandArgument("target", Description = "Root target value.", Position = 0)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"scope root:{Target ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("scopeleaf", "Runs the scope leaf.")]
    public sealed class ScopeLeafCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("scope leaf ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Control verification root.")]
    public sealed class ControlRootCommand : Command
    {
        [CommandArgument("quasar", Description = "Control root value.", Position = 0)]
        public string? Quasar { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"control root:{Quasar ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("controlleaf", "Runs the control leaf.")]
    public sealed class ControlLeafCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("control leaf ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Edge verification root.")]
    public sealed class EdgeRootCommand : Command
    {
        [CommandArgument("target", Description = "Root target value.", Position = 0)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"edge root:{Target ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("edgeleaf", "Runs the edge leaf.")]
    public sealed class EdgeLeafCommand : Command
    {
        [CommandArgument("target", Description = "Leaf target value.", Position = 0)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"edge leaf:{Target ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Chain verification root.")]
    public sealed class ChainRootCommand : Command
    {
        [CommandArgument("target", Description = "Root target value.", Position = 0)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"chain root:{Target ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("chainmid leaf", "Runs the chain leaf.")]
    public sealed class ChainMidLeafCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("chain leaf ran");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task LeafSurplus_DoesNotBindRootPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateScopeBuilder, ["scopeleaf", "one"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unexpected argument 'one'", error);
    }

    [Fact]
    public async Task LeafHelp_OmitsRootPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateScopeBuilder, ["scopeleaf", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Runs the scope leaf.", output);
        Assert.DoesNotContain("TARGET", output);
        Assert.DoesNotContain("<target>", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UncommonRootPositional_ControlRejectsSurplus()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateControlBuilder, ["controlleaf", "one"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unexpected argument 'one'", error);
    }

    [Fact]
    public async Task LeafOwnSameNamedPositional_BindsLeafLocal()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateEdgeBuilder, ["edgeleaf", "myval"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("edge leaf:myval", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ThreeLevelLeafSurplus_DoesNotBindRootPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateChainBuilder, ["chainmid", "leaf", "one"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unexpected argument 'one'", error);
    }

    [Fact]
    public async Task ThreeLevelLeafHelp_OmitsRootPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateChainBuilder, ["chainmid", "leaf", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Runs the chain leaf.", output);
        Assert.DoesNotContain("TARGET", output);
        Assert.DoesNotContain("<target>", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateScopeBuilder()
    {
        return CreateBaseBuilder("scope-test", "Scope Test", "Scope verification CLI.")
            .AddCommand<ScopeRootCommand>()
            .AddCommand<ScopeLeafCommand>();
    }

    private static ApplicationBuilder CreateControlBuilder()
    {
        return CreateBaseBuilder("scope-control-test", "Scope Control Test", "Scope control verification CLI.")
            .AddCommand<ControlRootCommand>()
            .AddCommand<ControlLeafCommand>();
    }

    private static ApplicationBuilder CreateEdgeBuilder()
    {
        return CreateBaseBuilder("scope-edge-test", "Scope Edge Test", "Scope edge verification CLI.")
            .AddCommand<EdgeRootCommand>()
            .AddCommand<EdgeLeafCommand>();
    }

    private static ApplicationBuilder CreateChainBuilder()
    {
        return CreateBaseBuilder("scope-chain-test", "Scope Chain Test", "Scope chain verification CLI.")
            .AddCommand<ChainRootCommand>()
            .AddCommand<ChainMidLeafCommand>();
    }

    private static ApplicationBuilder CreateBaseBuilder(string name, string title, string description)
    {
        return ApplicationBuilder.Create()
            .SetExecutableName(name)
            .SetExecutableTitle(title)
            .SetExecutableDescription(description)
            .SetExecutableVersion("9.9.9");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args, CancellationToken cancellationToken = default)
    {
        await ConsoleGate.WaitAsync();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await builderFactory().RunAsync(args, cancellationToken);
            outWriter.Flush();
            errorWriter.Flush();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleGate.Release();
        }
    }
}
