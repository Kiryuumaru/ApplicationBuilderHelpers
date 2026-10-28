using System.Collections;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process guards for the global-option initializer gate over inherited options.
/// Options declared on a shared base report the base as their declaring type, so the
/// gate compares the initializer default across the derived holders: a shared
/// base (abstract or concrete) with agreeing defaults promotes, while divergent
/// or unreadable defaults stay local. An option whose getter cannot be read
/// likewise keeps the option local instead of faulting.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class InheritedInitializerGateTests
{
    private static readonly SemaphoreSlim StateGate = new(1, 1);

    public abstract class SharedIdenticalBase : Command
    {
        [CommandOption("format", Description = "Output format.")]
        public string Format { get; set; } = "table";
    }

    [Command("identbase alpha", "First leaf inheriting the shared base option.")]
    public sealed class IdenticalBaseAlphaCommand : SharedIdenticalBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"identbase alpha:{Format}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("identbase beta", "Second leaf inheriting the shared base option.")]
    public sealed class IdenticalBaseBetaCommand : SharedIdenticalBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"identbase beta:{Format}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class SharedDivergentBase : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "base-default";
    }

    [Command("divshared alpha", "First leaf keeping the shared base default.")]
    public sealed class DivergentBaseAlphaCommand : SharedDivergentBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"divshared alpha:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("divshared beta", "Second leaf overriding the shared base default.")]
    public sealed class DivergentBaseBetaCommand : SharedDivergentBase
    {
        public DivergentBaseBetaCommand()
        {
            Shared = "divergent-default";
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"divshared beta:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("faulty alpha", "First leaf with an unreadable option.")]
    public sealed class UnreadableAlphaCommand : Command
    {
        [CommandOption("faulty", Description = "Unreadable value.")]
        public string? Faulty
        {
            get => throw new InvalidOperationException("Unreadable option.");
            set { }
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("faulty alpha ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("faulty beta", "Second leaf with an unreadable option.")]
    public sealed class UnreadableBetaCommand : Command
    {
        [CommandOption("faulty", Description = "Unreadable value.")]
        public string? Faulty
        {
            get => throw new InvalidOperationException("Unreadable option.");
            set { }
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("faulty beta ran");
            return ValueTask.CompletedTask;
        }
    }

    public class SharedConcreteBase : Command
    {
        [CommandOption("unified", Description = "Unified value.")]
        public string Unified { get; set; } = "unified-default";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"unified base:{Unified}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("sharedconcrete alpha", "First leaf inheriting the shared concrete base option.")]
    public sealed class ConcreteBaseAlphaCommand : SharedConcreteBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"unified alpha:{Unified}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("sharedconcrete beta", "Second leaf inheriting the shared concrete base option.")]
    public sealed class ConcreteBaseBetaCommand : SharedConcreteBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"unified beta:{Unified}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class SharedCollectionBase : Command
    {
        [CommandOption("tag", Description = "Tag list.")]
        public List<string> Tags { get; set; } = new();

        [CommandOption("mark", Description = "Mark array.")]
        public string[] Marks { get; set; } = ["a"];
    }

    [Command("list", "Leaf listing tags inherited from the shared collection base.")]
    public sealed class SharedCollectionListCommand : SharedCollectionBase
    {
        [CommandOption("local", Description = "Leaf-local value.")]
        public string? Local { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"list tags={string.Join(",", Tags)} local={Local ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("array", "Leaf listing marks inherited from the shared collection base.")]
    public sealed class SharedCollectionArrayCommand : SharedCollectionBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"array marks={string.Join(",", Marks)}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class DivergentListBase : Command
    {
        [CommandOption("tag", Description = "Tag list.")]
        public List<string> Tags { get; set; } = new();
    }

    [Command("divlist alpha", "First leaf with a divergent materialized list default.")]
    public sealed class DivergentListAlphaCommand : DivergentListBase
    {
        public DivergentListAlphaCommand()
        {
        }

        internal DivergentListAlphaCommand(string[]? tags)
        {
            Tags = tags?.ToList()!;
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"divlist alpha tags={(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("divlist beta", "Second leaf with a divergent materialized list default.")]
    public sealed class DivergentListBetaCommand : DivergentListBase
    {
        public DivergentListBetaCommand()
        {
        }

        internal DivergentListBetaCommand(string[]? tags)
        {
            Tags = tags?.ToList()!;
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"divlist beta tags={(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class TypeDivergentListBase : Command
    {
        [CommandOption("tag", Description = "Tag list.")]
        public List<string> Tags { get; set; } = ["a"];
    }

    [Command("typediv alpha", "First type-registered leaf keeping the shared list default.")]
    public sealed class TypeDivergentListAlphaCommand : TypeDivergentListBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"typediv alpha tags={string.Join(",", Tags)}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("typediv beta", "Second type-registered leaf overriding the shared list default.")]
    public sealed class TypeDivergentListBetaCommand : TypeDivergentListBase
    {
        public TypeDivergentListBetaCommand()
        {
            Tags = ["b"];
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"typediv beta tags={string.Join(",", Tags)}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class SharedLazyEnumerableBase : Command
    {
        [CommandOption("tag", Description = "Tag list.")]
        public IEnumerable<string> Tags { get; set; } = new CountingFiniteEnumerable();
    }

    [Command("lazybase alpha", "First leaf inheriting a finite lazy enumerable default.")]
    public sealed class LazyEnumerableAlphaCommand : SharedLazyEnumerableBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"lazybase alpha tags={string.Join(",", Tags)}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("lazybase beta", "Second leaf inheriting a finite lazy enumerable default.")]
    public sealed class LazyEnumerableBetaCommand : SharedLazyEnumerableBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"lazybase beta tags={string.Join(",", Tags)}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class SharedInterfaceCollectionBase : Command
    {
        [CommandOption("ilist", Description = "IList tags.")]
        public IList<string> IListTags { get; set; } = new List<string>();

        [CommandOption("icoll", Description = "ICollection tags.")]
        public ICollection<string> ICollectionTags { get; set; } = new List<string>();

        [CommandOption("ienum", Description = "IEnumerable tags.")]
        public IEnumerable<string> IEnumerableTags { get; set; } = new List<string>();
    }

    [Command("iface alpha", "First leaf inheriting interface-typed collection options.")]
    public sealed class InterfaceCollectionAlphaCommand : SharedInterfaceCollectionBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"iface alpha ilist={string.Join(",", IListTags)} icoll={string.Join(",", ICollectionTags)} ienum={string.Join(",", IEnumerableTags)}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("iface beta", "Second leaf inheriting interface-typed collection options.")]
    public sealed class InterfaceCollectionBetaCommand : SharedInterfaceCollectionBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"iface beta ilist={string.Join(",", IListTags)} icoll={string.Join(",", ICollectionTags)} ienum={string.Join(",", IEnumerableTags)}");
            return ValueTask.CompletedTask;
        }
    }

    public abstract class SingleCommandListBase : Command
    {
        [CommandOption("tag", Description = "Tag list.")]
        public List<string> Tags { get; set; } = new();
    }

    [Command("solo", "Single concrete command inheriting the list option.")]
    public sealed class SingleCommandListLeaf : SingleCommandListBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"solo tags={string.Join(",", Tags)}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("separate alpha", "First command declaring its own materialized list default.")]
    public sealed class SeparateListAlphaCommand : Command
    {
        [CommandOption("tag", Description = "Tag list.")]
        public List<string> Tags { get; set; } = new();

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"separate alpha tags={string.Join(",", Tags)}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("separate beta", "Second command declaring its own materialized list default.")]
    public sealed class SeparateListBetaCommand : Command
    {
        [CommandOption("tag", Description = "Tag list.")]
        public List<string> Tags { get; set; } = new();

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"separate beta tags={string.Join(",", Tags)}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task IdenticalBaseInitializer_Promotes()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<IdenticalBaseAlphaCommand>().AddCommand<IdenticalBaseBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.Contains("--format", output);
        Assert.Contains("Default: table", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var (alphaCode, alphaOutput, alphaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<IdenticalBaseAlphaCommand>().AddCommand<IdenticalBaseBetaCommand>(),
            ["identbase", "alpha", "--help"]);

        Assert.Equal(0, alphaCode);
        Assert.Contains("--format", alphaOutput);
        Assert.Contains("Default: table", alphaOutput);
        Assert.True(string.IsNullOrWhiteSpace(alphaError), $"Expected empty stderr but got: {alphaError}");

        var (betaCode, betaOutput, betaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<IdenticalBaseAlphaCommand>().AddCommand<IdenticalBaseBetaCommand>(),
            ["identbase", "beta", "--help"]);

        Assert.Equal(0, betaCode);
        Assert.Contains("--format", betaOutput);
        Assert.Contains("Default: table", betaOutput);
        Assert.True(string.IsNullOrWhiteSpace(betaError), $"Expected empty stderr but got: {betaError}");
    }

    [Fact]
    public async Task DivergentBaseInitializer_StaysLocal()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentBaseAlphaCommand>().AddCommand<DivergentBaseBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.DoesNotContain("--shared", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");

        var (alphaCode, alphaOutput, alphaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentBaseAlphaCommand>().AddCommand<DivergentBaseBetaCommand>(),
            ["divshared", "alpha", "--help"]);

        Assert.Equal(0, alphaCode);
        Assert.Contains("--shared", alphaOutput);
        Assert.Contains("Default: base-default", alphaOutput);
        Assert.DoesNotContain("divergent-default", alphaOutput);
        Assert.True(string.IsNullOrWhiteSpace(alphaError), $"Expected empty stderr but got: {alphaError}");

        var (betaCode, betaOutput, betaError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentBaseAlphaCommand>().AddCommand<DivergentBaseBetaCommand>(),
            ["divshared", "beta", "--help"]);

        Assert.Equal(0, betaCode);
        Assert.Contains("--shared", betaOutput);
        Assert.Contains("Default: divergent-default", betaOutput);
        Assert.DoesNotContain("Default: base-default", betaOutput);
        Assert.True(string.IsNullOrWhiteSpace(betaError), $"Expected empty stderr but got: {betaError}");
    }

    [Fact]
    public async Task SharedConcreteBaseInitializer_Promotes()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<ConcreteBaseAlphaCommand>().AddCommand<ConcreteBaseBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.Contains("--unified", output);
        Assert.Contains("Default: unified-default", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnreadableInitializer_StaysLocalWithoutFault()
    {
        var (typeCode, typeOutput, typeError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<UnreadableAlphaCommand>().AddCommand<UnreadableBetaCommand>(),
            ["--help"]);

        Assert.Equal(0, typeCode);
        Assert.Contains("GLOBAL OPTIONS:", typeOutput);
        Assert.DoesNotContain("--faulty", typeOutput);
        Assert.True(string.IsNullOrWhiteSpace(typeError), $"Expected empty stderr but got: {typeError}");

        var (typeRunCode, typeRunOutput, typeRunError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<UnreadableAlphaCommand>().AddCommand<UnreadableBetaCommand>(),
            ["faulty", "alpha"]);

        Assert.Equal(0, typeRunCode);
        Assert.Contains("faulty alpha ran", typeRunOutput);
        Assert.True(string.IsNullOrWhiteSpace(typeRunError), $"Expected empty stderr but got: {typeRunError}");

        var (instanceCode, instanceOutput, instanceError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand(new UnreadableAlphaCommand()).AddCommand(new UnreadableBetaCommand()),
            ["--help"]);

        Assert.Equal(0, instanceCode);
        Assert.Contains("GLOBAL OPTIONS:", instanceOutput);
        Assert.DoesNotContain("--faulty", instanceOutput);
        Assert.True(string.IsNullOrWhiteSpace(instanceError), $"Expected empty stderr but got: {instanceError}");

        var (instanceRunCode, instanceRunOutput, instanceRunError) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand(new UnreadableAlphaCommand()).AddCommand(new UnreadableBetaCommand()),
            ["faulty", "beta"]);

        Assert.Equal(0, instanceRunCode);
        Assert.Contains("faulty beta ran", instanceRunOutput);
        Assert.True(string.IsNullOrWhiteSpace(instanceRunError), $"Expected empty stderr but got: {instanceRunError}");
    }

    [Fact]
    public async Task SharedBaseMaterializedListInitializer_PromotesToRootScope()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SharedCollectionListCommand>().AddCommand<SharedCollectionArrayCommand>(),
            ["--tag", "x", "list"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("list tags=x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SharedBaseMaterializedListInitializer_LeafScopeBinds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SharedCollectionListCommand>().AddCommand<SharedCollectionArrayCommand>(),
            ["list", "--tag", "x"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("list tags=x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SharedBaseMaterializedListInitializer_RepeatedValuesAccumulateInOrder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SharedCollectionListCommand>().AddCommand<SharedCollectionArrayCommand>(),
            ["--tag", "x", "--tag", "y", "list"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("list tags=x,y", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SharedBaseArrayInitializer_RemainsPromotedControl()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<SharedCollectionListCommand>().AddCommand<SharedCollectionArrayCommand>(),
            ["--mark", "y", "array"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("array marks=y", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SharedBaseCollectionPromotion_HelpKeepsGlobalOptionsInBothScopes()
    {
        Func<ApplicationBuilder> builderFactory = () => CreateBuilder()
            .AddCommand<SharedCollectionListCommand>()
            .AddCommand<SharedCollectionArrayCommand>();

        var root = await RunCapturedAsync(builderFactory, ["--help"]);

        Assert.Equal(0, root.ExitCode);
        Assert.Contains("--tag", HelpSection(root.Output, "GLOBAL OPTIONS:"));
        Assert.Contains("--mark", HelpSection(root.Output, "GLOBAL OPTIONS:"));
        Assert.DoesNotContain("--tag", HelpSection(root.Output, "COMMANDS:"));
        Assert.True(string.IsNullOrWhiteSpace(root.Error), $"Expected empty stderr but got: {root.Error}");

        var leaf = await RunCapturedAsync(builderFactory, ["list", "--help"]);

        Assert.Equal(0, leaf.ExitCode);
        Assert.Contains("--tag", HelpSection(leaf.Output, "GLOBAL OPTIONS:"));
        Assert.Contains("--local", HelpSection(leaf.Output, "OPTIONS (command):"));
        Assert.DoesNotContain("--tag", HelpSection(leaf.Output, "OPTIONS (command):"));
        Assert.True(string.IsNullOrWhiteSpace(leaf.Error), $"Expected empty stderr but got: {leaf.Error}");
    }

    [Fact]
    public Task DivergentListElementValues_StayLocal() =>
        AssertDivergentListDefaultsStayLocal(["a"], ["b"]);

    [Fact]
    public Task DivergentListCounts_StayLocal() =>
        AssertDivergentListDefaultsStayLocal(["a"], ["a", "b"]);

    [Fact]
    public Task EmptyAndNonEmptyListDefaults_StayLocal() =>
        AssertDivergentListDefaultsStayLocal([], ["a"]);

    [Fact]
    public Task NullAndEmptyListDefaults_StayLocal() =>
        AssertDivergentListDefaultsStayLocal(null, []);

    [Fact]
    public Task NullAndNonEmptyListDefaults_StayLocal() =>
        AssertDivergentListDefaultsStayLocal(null, ["a"]);

    [Fact]
    public async Task SharedBaseInterfaceTypedMaterializedListInitializers_Promote()
    {
        Func<ApplicationBuilder> builderFactory = () => CreateBuilder()
            .AddCommand<InterfaceCollectionAlphaCommand>()
            .AddCommand<InterfaceCollectionBetaCommand>();

        var help = await RunCapturedAsync(builderFactory, ["--help"]);

        Assert.Equal(0, help.ExitCode);
        var globalOptions = HelpSection(help.Output, "GLOBAL OPTIONS:");
        Assert.Contains("--ilist", globalOptions);
        Assert.Contains("--icoll", globalOptions);
        Assert.Contains("--ienum", globalOptions);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var root = await RunCapturedAsync(
            builderFactory,
            ["--ilist", "x", "--icoll", "y", "--ienum", "z", "iface", "alpha"]);

        Assert.Equal(0, root.ExitCode);
        Assert.Contains("iface alpha ilist=x icoll=y ienum=z", root.Output);
        Assert.True(string.IsNullOrWhiteSpace(root.Error), $"Expected empty stderr but got: {root.Error}");

        var leaf = await RunCapturedAsync(builderFactory, ["iface", "beta", "--ilist", "x"]);

        Assert.Equal(0, leaf.ExitCode);
        Assert.Contains("iface beta ilist=x icoll= ienum=", leaf.Output);
        Assert.True(string.IsNullOrWhiteSpace(leaf.Error), $"Expected empty stderr but got: {leaf.Error}");
    }

    [Fact]
    public async Task SingleConcreteCommandInheritedListInitializer_Promotes()
    {
        Func<ApplicationBuilder> builderFactory = () => CreateBuilder().AddCommand<SingleCommandListLeaf>();

        var help = await RunCapturedAsync(builderFactory, ["--help"]);

        Assert.Equal(0, help.ExitCode);
        Assert.Contains("--tag", HelpSection(help.Output, "GLOBAL OPTIONS:"));
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builderFactory, ["--tag", "x", "solo"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("solo tags=x", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task SeparateCommandsWithIdenticalMaterializedListDefaults_Promote()
    {
        Func<ApplicationBuilder> builderFactory = () => CreateBuilder()
            .AddCommand<SeparateListAlphaCommand>()
            .AddCommand<SeparateListBetaCommand>();

        var help = await RunCapturedAsync(builderFactory, ["--help"]);

        Assert.Equal(0, help.ExitCode);
        Assert.Contains("--tag", HelpSection(help.Output, "GLOBAL OPTIONS:"));
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var run = await RunCapturedAsync(builderFactory, ["--tag", "x", "separate", "alpha"]);

        Assert.Equal(0, run.ExitCode);
        Assert.Contains("separate alpha tags=x", run.Output);
        Assert.True(string.IsNullOrWhiteSpace(run.Error), $"Expected empty stderr but got: {run.Error}");
    }

    [Fact]
    public async Task TypeRegisteredDivergentListDefaults_StayLocal()
    {
        Func<ApplicationBuilder> builderFactory = () => CreateBuilder()
            .AddCommand<TypeDivergentListAlphaCommand>()
            .AddCommand<TypeDivergentListBetaCommand>();

        var help = await RunCapturedAsync(builderFactory, ["--help"]);

        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("--tag", HelpSection(help.Output, "GLOBAL OPTIONS:"));
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var root = await RunCapturedAsync(builderFactory, ["--tag", "x", "typediv", "alpha"]);

        Assert.Equal(2, root.ExitCode);
        Assert.Contains("Unknown option: --tag", root.Error);

        var leaf = await RunCapturedAsync(builderFactory, ["typediv", "beta", "--tag", "x"]);

        Assert.Equal(0, leaf.ExitCode);
        Assert.Contains("typediv beta tags=x", leaf.Output);
        Assert.True(string.IsNullOrWhiteSpace(leaf.Error), $"Expected empty stderr but got: {leaf.Error}");
    }

    [Fact]
    public async Task SharedBaseLazyEnumerableInitializer_StaysLocalWithoutEnumeration()
    {
        CountingFiniteEnumerable.ResetEnumerationCount();

        Func<ApplicationBuilder> builderFactory = () => CreateBuilder()
            .AddCommand<LazyEnumerableAlphaCommand>()
            .AddCommand<LazyEnumerableBetaCommand>();

        var rootHelp = await RunCapturedAsync(builderFactory, ["--help"]);

        Assert.Equal(0, rootHelp.ExitCode);
        Assert.DoesNotContain("--tag", HelpSection(rootHelp.Output, "GLOBAL OPTIONS:"));
        Assert.True(string.IsNullOrWhiteSpace(rootHelp.Error), $"Expected empty stderr but got: {rootHelp.Error}");
        Assert.Equal(0, CountingFiniteEnumerable.EnumerationCount);

        var leafHelp = await RunCapturedAsync(builderFactory, ["lazybase", "alpha", "--help"]);

        Assert.Equal(0, leafHelp.ExitCode);
        Assert.Contains("--tag", leafHelp.Output);
        Assert.True(string.IsNullOrWhiteSpace(leafHelp.Error), $"Expected empty stderr but got: {leafHelp.Error}");
        Assert.Equal(0, CountingFiniteEnumerable.EnumerationCount);

        var root = await RunCapturedAsync(builderFactory, ["--tag", "x", "lazybase", "alpha"]);

        Assert.Equal(2, root.ExitCode);
        Assert.Contains("Unknown option: --tag", root.Error);
        Assert.Equal(0, CountingFiniteEnumerable.EnumerationCount);

        var leaf = await RunCapturedAsync(builderFactory, ["lazybase", "beta", "--tag", "x"]);

        Assert.Equal(0, leaf.ExitCode);
        Assert.Contains("lazybase beta tags=x", leaf.Output);
        Assert.True(string.IsNullOrWhiteSpace(leaf.Error), $"Expected empty stderr but got: {leaf.Error}");
        Assert.Equal(0, CountingFiniteEnumerable.EnumerationCount);

        Assert.Equal("a,b", string.Join(",", new CountingFiniteEnumerable()));
        Assert.Equal(1, CountingFiniteEnumerable.EnumerationCount);
    }

    private static async Task AssertDivergentListDefaultsStayLocal(string[]? alphaDefault, string[]? betaDefault)
    {
        Func<ApplicationBuilder> builderFactory = () => CreateBuilder()
            .AddCommand(new DivergentListAlphaCommand(alphaDefault))
            .AddCommand(new DivergentListBetaCommand(betaDefault));

        var help = await RunCapturedAsync(builderFactory, ["--help"]);

        Assert.Equal(0, help.ExitCode);
        Assert.DoesNotContain("--tag", HelpSection(help.Output, "GLOBAL OPTIONS:"));
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");

        var root = await RunCapturedAsync(builderFactory, ["--tag", "x", "divlist", "alpha"]);

        Assert.Equal(2, root.ExitCode);
        Assert.Contains("Unknown option: --tag", root.Error);

        var leaf = await RunCapturedAsync(builderFactory, ["divlist", "beta", "--tag", "x"]);

        Assert.Equal(0, leaf.ExitCode);
        Assert.Contains("divlist beta tags=x", leaf.Output);
        Assert.True(string.IsNullOrWhiteSpace(leaf.Error), $"Expected empty stderr but got: {leaf.Error}");
    }

    private sealed class CountingFiniteEnumerable : IEnumerable<string>
    {
        public static int EnumerationCount { get; private set; }

        public static void ResetEnumerationCount() => EnumerationCount = 0;

        public IEnumerator<string> GetEnumerator()
        {
            EnumerationCount++;
            return YieldTags().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        private static IEnumerable<string> YieldTags()
        {
            yield return "a";
            yield return "b";
        }
    }

    private static string HelpSection(string output, string heading)
    {
        var lines = output.Replace("\r\n", "\n").Split('\n');
        var start = Array.FindIndex(lines, line => string.Equals(line.Trim(), heading, StringComparison.Ordinal));
        Assert.True(start >= 0, $"Expected help section '{heading}' in:{Environment.NewLine}{output}");

        var body = new List<string>();
        for (var index = start + 1; index < lines.Length; index++)
        {
            if (string.IsNullOrWhiteSpace(lines[index]) || !char.IsWhiteSpace(lines[index][0]))
                break;
            body.Add(lines[index]);
        }

        return string.Join("\n", body);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("inheritedgate-test")
            .SetExecutableTitle("InheritedGate Test")
            .SetExecutableDescription("Inherited initializer gate verification CLI.")
            .SetExecutableVersion("9.9.9");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args)
    {
        await StateGate.WaitAsync();
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
                var exitCode = await builderFactory().RunAsync(args);
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
            StateGate.Release();
        }
    }
}
