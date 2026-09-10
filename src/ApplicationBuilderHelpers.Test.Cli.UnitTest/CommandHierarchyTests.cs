using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process command hierarchy tests for the CLI hierarchy builder.
/// Exercises nesting through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// nested leaf execution, shared intermediates, root commands,
/// global option promotion, abstract intermediate reuse,
/// and allowed-value promotion.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CommandHierarchyTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("hub leaf", "Runs the nested hub leaf.")]
    public sealed class HubLeafCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("hub leaf ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub alpha", "Runs hub alpha.")]
    public sealed class HubAlphaCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("hub alpha ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("hub beta", "Runs hub beta.")]
    public sealed class HubBetaCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("hub beta ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Runs as the root command.")]
    public sealed class RootOnlyCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("root ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("shared alpha", "First leaf with shared option.")]
    public sealed class SharedAlphaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string? Shared { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"shared alpha:{Shared ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("shared beta", "Second leaf with shared option.")]
    public sealed class SharedBetaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string? Shared { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"shared beta:{Shared ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("divergent alpha", "Optional shared option leaf.")]
    public sealed class DivergentAlphaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string? Shared { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"divergent alpha:{Shared ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("divergent beta", "Required shared option leaf.")]
    public sealed class DivergentBetaCommand : Command
    {
        [CommandOption("shared", Description = "Shared value.", Required = true)]
        public string? Shared { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"divergent beta:{Shared ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("cfgbase", "Base config hub.")]
    public abstract class CfgBaseHub : Command
    {
        [CommandOption("format", Description = "Output format.")]
        public string Format { get; set; } = "table";

        [CommandArgument("key", Description = "Config key.", Position = 0)]
        public string? Key { get; set; }
    }

    [Command("cfgbase get", "Gets a config value.")]
    public sealed class CfgBaseGetCommand : CfgBaseHub
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"cfgbase get:{Format}:{Key ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("cfgbase set", "Sets a config value.")]
    public sealed class CfgBaseSetCommand : CfgBaseHub
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"cfgbase set:{Format}:{Key ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("other", "Other hub.")]
    public abstract class MissHubBase : Command
    {
        [CommandOption("other-opt", Description = "Other option.")]
        public string? OtherOpt { get; set; }
    }

    [Command("misshub get", "Gets a misshub value.")]
    public sealed class MissHubGetCommand : MissHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("misshub get ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("misshub set", "Sets a misshub value.")]
    public sealed class MissHubSetCommand : MissHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("misshub set ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceone alpha", "First identical choice leaf.")]
    public sealed class ChoiceIdenticalAlphaCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"choice identical alpha:{Mode ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceone beta", "Second identical choice leaf.")]
    public sealed class ChoiceIdenticalBetaCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"choice identical beta:{Mode ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choicelen alpha", "Choice leaf with two allowed values.")]
    public sealed class ChoiceLengthAlphaCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice length alpha ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choicelen beta", "Choice leaf with three allowed values.")]
    public sealed class ChoiceLengthBetaCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml", "yaml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice length beta ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceelem alpha", "Choice leaf allowing json and xml.")]
    public sealed class ChoiceElementAlphaCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice element alpha ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceelem beta", "Choice leaf allowing json and yaml.")]
    public sealed class ChoiceElementBetaCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "yaml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice element beta ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceopen alpha", "Choice leaf without allowed values.")]
    public sealed class ChoiceOpenAlphaCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.")]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice open alpha ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceopen beta", "Choice leaf with allowed values.")]
    public sealed class ChoiceOpenBetaCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice open beta ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choicetype alpha", "Choice leaf with text size.")]
    public sealed class ChoiceTypeAlphaCommand : Command
    {
        [CommandOption("size", Description = "Size value.")]
        public string? Size { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice type alpha ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choicetype beta", "Choice leaf with numeric size.")]
    public sealed class ChoiceTypeBetaCommand : Command
    {
        [CommandOption("size", Description = "Size value.")]
        public int Size { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice type beta ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceshort alpha", "Choice leaf with s short name.")]
    public sealed class ChoiceShortAlphaCommand : Command
    {
        [CommandOption('s', "shared", Description = "Shared value.")]
        public string? Shared { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice short alpha ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("choiceshort beta", "Choice leaf with x short name.")]
    public sealed class ChoiceShortBetaCommand : Command
    {
        [CommandOption('x', "shared", Description = "Shared value.")]
        public string? Shared { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("choice short beta ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task NestedLeaf_Runs()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<HubLeafCommand>(), ["hub", "leaf"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("hub leaf ran", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SiblingLeaves_SharingIntermediate_BothRun()
    {
        var alpha = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<HubAlphaCommand>().AddCommand<HubBetaCommand>(), ["hub", "alpha"]);
        var beta = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<HubAlphaCommand>().AddCommand<HubBetaCommand>(), ["hub", "beta"]);

        Assert.Equal(0, alpha.ExitCode);
        Assert.Contains("hub alpha ran", alpha.Output);
        Assert.Equal(0, beta.ExitCode);
        Assert.Contains("hub beta ran", beta.Output);
    }

    [Fact]
    public async Task RootCommand_RunsWithNoArgs()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<RootOnlyCommand>(), []);

        Assert.Equal(0, exitCode);
        Assert.Contains("root ran", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DuplicateRoot_ThrowsInvalidOperation()
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
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => CreateBuilder().AddCommand<RootOnlyCommand>().AddCommand<RootOnlyCommand>().RunAsync([]));
            Assert.Contains("Cannot have more than one root command", exception.Message);
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleGate.Release();
        }
    }

    [Fact]
    public async Task IdenticalSharedOption_PromotedToRootHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SharedAlphaCommand>().AddCommand<SharedBetaCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.Contains("--shared", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task IdenticalSharedOption_LeafAcceptsSharedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SharedAlphaCommand>().AddCommand<SharedBetaCommand>(),
            ["shared", "alpha", "--shared=hello"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("shared alpha:hello", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DivergentRequired_BlocksPromotionToRootHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentAlphaCommand>().AddCommand<DivergentBetaCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--shared", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DivergentLeaves_StillRunWithOwnOptions()
    {
        var alpha = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentAlphaCommand>().AddCommand<DivergentBetaCommand>(),
            ["divergent", "alpha"]);
        var beta = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentAlphaCommand>().AddCommand<DivergentBetaCommand>(),
            ["divergent", "beta", "--shared=hello"]);

        Assert.Equal(0, alpha.ExitCode);
        Assert.Contains("divergent alpha:null", alpha.Output);
        Assert.Equal(0, beta.ExitCode);
        Assert.Contains("divergent beta:hello", beta.Output);
    }

    [Fact]
    public async Task IdenticalAllowedValues_PromotedToRootHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ChoiceIdenticalAlphaCommand>().AddCommand<ChoiceIdenticalBetaCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.Contains("--mode", output);
        Assert.Contains("json, xml", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DifferentAllowedLength_BlocksPromotion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ChoiceLengthAlphaCommand>().AddCommand<ChoiceLengthBetaCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--mode", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DifferentAllowedElement_BlocksPromotion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ChoiceElementAlphaCommand>().AddCommand<ChoiceElementBetaCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--mode", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NullVersusValuedAllowed_BlocksPromotion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ChoiceOpenAlphaCommand>().AddCommand<ChoiceOpenBetaCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--mode", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DivergentOptionType_BlocksPromotion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ChoiceTypeAlphaCommand>().AddCommand<ChoiceTypeBetaCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--size", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DivergentShortName_BlocksPromotion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ChoiceShortAlphaCommand>().AddCommand<ChoiceShortBetaCommand>(), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--shared", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AbstractBase_LeavesInheritBaseOption()
    {
        var get = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<CfgBaseGetCommand>().AddCommand<CfgBaseSetCommand>().AddCommand<HubLeafCommand>(),
            ["cfgbase", "get", "--format=json"]);
        var set = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<CfgBaseGetCommand>().AddCommand<CfgBaseSetCommand>().AddCommand<HubLeafCommand>(),
            ["cfgbase", "set", "--format=yaml"]);

        Assert.Equal(0, get.ExitCode);
        Assert.Contains("cfgbase get:json:null", get.Output);
        Assert.Equal(0, set.ExitCode);
        Assert.Contains("cfgbase set:yaml:null", set.Output);
    }

    [Fact]
    public async Task AbstractBase_LeavesInheritBaseArgument()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<CfgBaseGetCommand>().AddCommand<CfgBaseSetCommand>().AddCommand<HubLeafCommand>(),
            ["cfgbase", "get", "mykey"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("cfgbase get:table:mykey", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AbstractBaseMiss_IntermediateFallsBackAndLeavesRun()
    {
        var get = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<MissHubGetCommand>().AddCommand<MissHubSetCommand>(),
            ["misshub", "get"]);
        var set = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<MissHubGetCommand>().AddCommand<MissHubSetCommand>(),
            ["misshub", "set"]);

        Assert.Equal(0, get.ExitCode);
        Assert.Contains("misshub get ran", get.Output);
        Assert.Equal(0, set.ExitCode);
        Assert.Contains("misshub set ran", set.Output);

        var intermediateHelp = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<MissHubGetCommand>().AddCommand<MissHubSetCommand>(),
            ["misshub", "--help"]);

        Assert.Equal(0, intermediateHelp.ExitCode);
        Assert.Contains("Commands for misshub", intermediateHelp.Output);
        Assert.DoesNotContain("Other hub", intermediateHelp.Output);
        Assert.DoesNotContain("--other-opt", intermediateHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(intermediateHelp.Error), $"Expected empty stderr but got: {intermediateHelp.Error}");

        var rootHelp = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<MissHubGetCommand>().AddCommand<MissHubSetCommand>(),
            ["--help"]);

        Assert.Equal(0, rootHelp.ExitCode);
        Assert.Contains("Commands for misshub", rootHelp.Output);
        Assert.DoesNotContain("Other hub", rootHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(rootHelp.Error), $"Expected empty stderr but got: {rootHelp.Error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("hier-test")
            .SetExecutableTitle("Hier Test")
            .SetExecutableDescription("Hierarchy verification CLI.")
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
