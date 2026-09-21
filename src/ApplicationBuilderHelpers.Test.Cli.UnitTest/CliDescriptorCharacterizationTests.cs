using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Interfaces;
using System.Reflection;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

// Characterization tests intentionally exercise the four [Obsolete] descriptor
// shims (FromCommandType/FromDeclaredType parity) plus the internal walks.
// Precedent: CancellationExitCodeTests.cs disables CS0809/CS0618 + restores
// around its intentional legacy-path probes.
#pragma warning disable CS0618

/// <summary>
/// Characterization (golden-master) tests for the CLI descriptor duplication surface:
/// required-keyword detection, display-name mapping, <c>FromCommandType</c> vs
/// <c>FromDeclaredType</c> scoping, and nullable-unwrap/enum-candidate snapshots across
/// <c>SubCommandOptionInfo</c>, <c>SubCommandArgumentInfo</c> and
/// <c>CommandReflectionCache</c>. Pins current outputs before refactor so a later
/// extraction can prove zero drift. Parser-suppression parity and the
/// global-promotion signature are pinned in-memory only (no console use);
/// help rendering, owner-bound inheritance scope and per-run copy identity
/// are intentionally out of scope.
/// Pure in-memory descriptor tests: no console use, no collection gate needed.
/// </summary>
public sealed class CliDescriptorCharacterizationTests
{
    public enum DescriptorShade
    {
        Red,
        Green,
        Blue
    }

    public sealed class ShadeParser : CommandTypeParser<DescriptorShade>
    {
        public override DescriptorShade ParseValue(string? value, out string? validateError)
        {
            if (Enum.TryParse<DescriptorShade>(value, ignoreCase: true, out var result))
            {
                validateError = null;
                return result;
            }

            validateError = $"'{value}' is not a valid shade.";
            return default;
        }
    }

    private sealed class RequiredMatrixHolder
    {
        public string? PlainText { get; set; }

        public required string KeywordName { get; set; }

        public string? NullableText { get; set; }

        public required string? KeywordNullableText { get; set; }

        public int? NullableNumber { get; set; }

        public required int? KeywordNullableNumber { get; set; }
    }

    private class OverridableBase
    {
        public virtual string Label { get; set; } = string.Empty;
    }

    private sealed class OverridingChild : OverridableBase
    {
        public required override string Label { get; set; }
    }

    private class InheritProbeBase
    {
        [CommandOption("code")]
        public required string Code { get; set; }
    }

    private sealed class InheritProbeChild : InheritProbeBase
    {
    }

    private sealed class DisplayTypeHolder
    {
        public string? Text { get; set; }

        public int Number { get; set; }

        public double Ratio { get; set; }

        public bool Flag { get; set; }

        public DateTime Moment { get; set; }

        public DirectoryInfo? Folder { get; set; }

        public FileInfo? File { get; set; }

        public TimeSpan Duration { get; set; }

        public Guid Token { get; set; }

        public Uri? Endpoint { get; set; }

        public int? MaybeNumber { get; set; }

        public int[] Scores { get; set; } = [];

        public List<string> Names { get; set; } = [];
    }

    private class ScopingBase
    {
        [CommandOption("base-opt")]
        public string? BaseOption { get; set; }

        [CommandArgument("base-arg", Position = 0)]
        public string? BaseArgument { get; set; }
    }

    private sealed class ScopingDerived : ScopingBase
    {
        [CommandOption("leaf-opt")]
        public string? LeafOption { get; set; }

        [CommandArgument("leaf-arg", Position = 1)]
        public string? LeafArgument { get; set; }
    }

    private sealed class OrderedArgumentHolder
    {
        [CommandArgument("second", Position = 1)]
        public string? Second { get; set; }

        [CommandArgument("first", Position = 0)]
        public string? First { get; set; }
    }

    private sealed class EnumHolder
    {
        [CommandOption("shade")]
        public DescriptorShade Shade { get; set; }

        [CommandOption("maybe-shade")]
        public DescriptorShade? MaybeShade { get; set; }

        [CommandOption("text")]
        public string? Text { get; set; }

        [CommandOption("fixed", FromAmong = ["a", "b"])]
        public DescriptorShade Fixed { get; set; }
    }

    private sealed class EnumArgumentHolder
    {
        [CommandArgument("shade", Position = 0)]
        public DescriptorShade Shade { get; set; }

        [CommandArgument("maybe-shade", Position = 1)]
        public DescriptorShade? MaybeShade { get; set; }

        [CommandArgument("text", Position = 2)]
        public string? Text { get; set; }

        [CommandArgument("fixed", Position = 3, FromAmong = ["a", "b"])]
        public DescriptorShade Fixed { get; set; }
    }

    private sealed class KeywordSplitHolder
    {
        [CommandOption("kw", Required = false)]
        public required string KeywordOption { get; set; }

        [CommandOption("ex", Required = true)]
        public string? ExplicitOption { get; set; }

        [CommandArgument("kwarg", Position = 0)]
        public required string KeywordArgument { get; set; }

        [CommandArgument("exarg", Position = 1, Required = true)]
        public string? ExplicitArgument { get; set; }
    }

    private sealed class FixedArgumentHolder
    {
        [CommandArgument("fixedarg", Position = 2, FromAmong = ["x", "y"])]
        public string? Fixed { get; set; }
    }

    private class ParityOptionBase
    {
        [CommandOption('b', "base-opt", Description = "Base option.", EnvironmentVariable = "PARITY_BASE")]
        public string? BaseOption { get; set; }

        [CommandOption("base-flag", Description = "Base flag.")]
        public bool BaseFlag { get; set; }
    }

    private sealed class ParityOptionLeaf : ParityOptionBase
    {
        [CommandOption('v', "verbose", Description = "Verbose output.")]
        public bool Verbose { get; set; }

        [CommandOption("maybe-verbose", Description = "Optional flag.")]
        public bool? MaybeVerbose { get; set; }

        [CommandOption('o', "output", Description = "Output path.", Required = true, EnvironmentVariable = "PARITY_OUTPUT", CaseSensitive = true, Secret = true, FromAmong = ["a", "b"])]
        public string? Output { get; set; }

        [CommandOption("shade", Description = "Shade value.")]
        public DescriptorShade Shade { get; set; }

        [CommandOption("maybe-shade", Description = "Optional shade.")]
        public DescriptorShade? MaybeShade { get; set; }

        [CommandOption("scores", Description = "Score list.")]
        public int[] Scores { get; set; } = [];
    }

    private class ParityArgumentBase
    {
        [CommandArgument("base-arg", Position = 3, Description = "Base argument.")]
        public string? BaseArgument { get; set; }
    }

    private sealed class ParityArgumentLeaf : ParityArgumentBase
    {
        [CommandArgument("tail", Position = 2, Description = "Tail value.", FromAmong = ["x", "y"], CaseSensitive = true, Secret = true)]
        public string? Tail { get; set; }

        [CommandArgument("head", Position = 0, Description = "Head value.", Required = true)]
        public string? Head { get; set; }

        [CommandArgument("middle", Position = 1, Description = "Middle value.")]
        public int Middle { get; set; }

        [CommandArgument("tags", Position = 4, Description = "Tag list.")]
        public List<string> Tags { get; set; } = [];
    }

    private sealed class HiddenMemberHolder
    {
        [CommandOption("open")]
        public string? Open { get; set; }

        [CommandOption("concealed")]
        private string? Concealed { get; set; }

        [CommandArgument("open-arg", Position = 1)]
        public string? OpenArg { get; set; }

        [CommandArgument("concealed-arg", Position = 0)]
        private string? ConcealedArg { get; set; }
    }

    private class OverridableOptionBase
    {
        [CommandOption("label", Description = "Base label.")]
        public virtual string Label { get; set; } = string.Empty;
    }

    private sealed class OverridingOptionChild : OverridableOptionBase
    {
        [CommandOption("label", Description = "Child label.")]
        public override string Label { get; set; } = string.Empty;
    }

    private class HiddenOptionBase
    {
        [CommandOption("name", Description = "Base name.")]
        public string Name { get; set; } = string.Empty;
    }

    private sealed class HidingOptionChild : HiddenOptionBase
    {
        [CommandOption("name", Description = "Child name.")]
        public new string Name { get; set; } = string.Empty;
    }

    private sealed class GlobalProbeAlpha
    {
        [CommandOption('s', "shared", Description = "Shared value.", FromAmong = ["json", "xml"])]
        public string? Shared { get; set; }
    }

    private sealed class GlobalProbeBeta
    {
        [CommandOption('s', "shared", Description = "Shared value.", FromAmong = ["json", "xml"])]
        public string? Shared { get; set; }
    }

    private sealed class GlobalProbeRequired
    {
        [CommandOption('s', "shared", Description = "Shared value.", Required = true, FromAmong = ["json", "xml"])]
        public string? Shared { get; set; }
    }

    private sealed class GlobalProbeSecret
    {
        [CommandOption('s', "shared", Description = "Shared value.", Secret = true, FromAmong = ["json", "xml"])]
        public string? Shared { get; set; }
    }

    private sealed class GlobalProbeShort
    {
        [CommandOption('x', "shared", Description = "Shared value.", FromAmong = ["json", "xml"])]
        public string? Shared { get; set; }
    }

    private sealed class GlobalProbeChoices
    {
        [CommandOption('s', "shared", Description = "Shared value.", FromAmong = ["json", "yaml"])]
        public string? Shared { get; set; }
    }

    private sealed class GlobalProbeType
    {
        [CommandOption('s', "shared", Description = "Shared value.")]
        public int Shared { get; set; }
    }

    private static PropertyInfo Prop<T>(string name) => typeof(T).GetProperty(name)!;

    public static TheoryData<PropertyInfo, bool, bool> RequiredCases => new()
    {
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.PlainText)), false, false },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.PlainText)), true, true },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.KeywordName)), false, true },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.KeywordName)), true, true },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.NullableText)), false, false },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.NullableText)), true, true },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.KeywordNullableText)), false, true },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.NullableNumber)), false, false },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.NullableNumber)), true, true },
        { Prop<RequiredMatrixHolder>(nameof(RequiredMatrixHolder.KeywordNullableNumber)), false, true },
        { Prop<OverridableBase>(nameof(OverridableBase.Label)), false, false },
        { Prop<OverridingChild>(nameof(OverridingChild.Label)), false, true },
        { Prop<InheritProbeBase>(nameof(InheritProbeBase.Code)), false, true },
    };

    [Theory]
    [MemberData(nameof(RequiredCases))]
    public void Option_Requiredness_FollowsAttributeOrKeyword(PropertyInfo property, bool attributeRequired, bool expected)
    {
        var info = SubCommandOptionInfo.FromProperty(
            property,
            new CommandOptionAttribute("probe") { Required = attributeRequired });

        Assert.Equal(expected, info.IsRequired);
    }

    [Theory]
    [MemberData(nameof(RequiredCases))]
    public void Argument_Requiredness_FollowsAttributeOrKeyword(PropertyInfo property, bool attributeRequired, bool expected)
    {
        var info = SubCommandArgumentInfo.FromProperty(
            property,
            new CommandArgumentAttribute("probe") { Required = attributeRequired });

        Assert.Equal(expected, info.IsRequired);
    }

    [Fact]
    public void RequiredKeyword_SeenThroughDeclaringType_WalkKeepsIt()
    {
        var options = SubCommandOptionInfo.FromCommandType(typeof(InheritProbeChild));

        Assert.True(options.Single(o => o.LongName == "code").IsRequired);
    }

    // #454: friendly help tokens (HelpTypeDisplay) — no raw CLR names (TIMESPAN/GUID/URI/NULLABLE`1).
    [Theory]
    [InlineData(nameof(DisplayTypeHolder.Text), "STRING")]
    [InlineData(nameof(DisplayTypeHolder.Number), "NUMBER")]
    [InlineData(nameof(DisplayTypeHolder.Ratio), "NUMBER")]
    [InlineData(nameof(DisplayTypeHolder.Flag), "BOOL")]
    [InlineData(nameof(DisplayTypeHolder.Moment), "DATE")]
    [InlineData(nameof(DisplayTypeHolder.Folder), "DIR")]
    [InlineData(nameof(DisplayTypeHolder.File), "FILE")]
    [InlineData(nameof(DisplayTypeHolder.Duration), "VALUE")]
    [InlineData(nameof(DisplayTypeHolder.Token), "VALUE")]
    [InlineData(nameof(DisplayTypeHolder.Endpoint), "VALUE")]
    [InlineData(nameof(DisplayTypeHolder.MaybeNumber), "NUMBER")]
    [InlineData(nameof(DisplayTypeHolder.Scores), "NUMBER")]
    [InlineData(nameof(DisplayTypeHolder.Names), "STRING")]
    public void OptionAndArgument_DisplayNames_MatchTable(string propertyName, string expected)
    {
        var property = Prop<DisplayTypeHolder>(propertyName);
        var option = SubCommandOptionInfo.FromProperty(property, new CommandOptionAttribute("probe"));
        var argument = SubCommandArgumentInfo.FromProperty(property, new CommandArgumentAttribute("probe"));

        Assert.Equal(expected, option.GetTypeName());
        Assert.Equal(expected, argument.GetTypeName());
    }

    [Fact]
    public void Options_FromCommandType_IncludesBaseFirst()
    {
        var options = SubCommandOptionInfo.FromCommandType(typeof(ScopingDerived));

        Assert.Equal(new[] { "base-opt", "leaf-opt" }, options.Select(o => o.LongName));
    }

    [Fact]
    public void Options_FromDeclaredType_ExcludesBase()
    {
        var options = SubCommandOptionInfo.FromDeclaredType(typeof(ScopingDerived));

        Assert.Equal(new[] { "leaf-opt" }, options.Select(o => o.LongName));
    }

    [Fact]
    public void Arguments_FromCommandType_IncludesBaseOrderedByPosition()
    {
        var arguments = SubCommandArgumentInfo.FromCommandType(typeof(ScopingDerived));

        Assert.Equal(new[] { "base-arg", "leaf-arg" }, arguments.Select(a => a.Name));
    }

    [Fact]
    public void Arguments_FromDeclaredType_ExcludesBase()
    {
        var arguments = SubCommandArgumentInfo.FromDeclaredType(typeof(ScopingDerived));

        Assert.Equal(new[] { "leaf-arg" }, arguments.Select(a => a.Name));
    }

    [Fact]
    public void Arguments_FromCommandType_SortsByPosition()
    {
        var arguments = SubCommandArgumentInfo.FromCommandType(typeof(OrderedArgumentHolder));

        Assert.Equal(new[] { "first", "second" }, arguments.Select(a => a.Name));
    }

    [Fact]
    public void PropertyWalk_ReturnsBaseFirstOrder()
    {
        var properties = CommandReflectionCache.Walk(typeof(ScopingDerived));
        var names = properties.Select(p => p.Name).ToList();

        Assert.Equal(4, names.Count);
        Assert.True(names.IndexOf(nameof(ScopingBase.BaseOption)) < names.IndexOf(nameof(ScopingDerived.LeafOption)));
        Assert.True(names.IndexOf(nameof(ScopingBase.BaseArgument)) < names.IndexOf(nameof(ScopingDerived.LeafArgument)));
    }

    [Fact]
    public void Option_PlainEnum_AutoPopulatesNames()
    {
        var property = Prop<EnumHolder>(nameof(EnumHolder.Shade));
        var info = SubCommandOptionInfo.FromProperty(property, new CommandOptionAttribute("shade"));

        Assert.Equal(new[] { "Red", "Green", "Blue" }, info.ValidValues!.Cast<string>());
    }

    [Fact]
    public void Option_NullableEnum_UnwrapsAndPopulatesNames()
    {
        var property = Prop<EnumHolder>(nameof(EnumHolder.MaybeShade));
        var info = SubCommandOptionInfo.FromProperty(property, new CommandOptionAttribute("maybe-shade"));

        Assert.Equal(new[] { "Red", "Green", "Blue" }, info.ValidValues!.Cast<string>());
    }

    [Fact]
    public void Option_ExplicitChoices_WinOverEnumNames()
    {
        var property = Prop<EnumHolder>(nameof(EnumHolder.Fixed));
        var info = SubCommandOptionInfo.FromProperty(
            property,
            new CommandOptionAttribute("fixed") { FromAmong = ["a", "b"] });

        Assert.Equal(new[] { "a", "b" }, info.ValidValues!.Cast<string>());
    }

    [Fact]
    public void Option_CustomParser_SuppressesAutoPopulate()
    {
        ICommandTypeParserCollection parsers = ApplicationBuilder.Create().AddCommandTypeParser<ShadeParser>();
        var property = Prop<EnumHolder>(nameof(EnumHolder.Shade));
        var info = SubCommandOptionInfo.FromProperty(property, new CommandOptionAttribute("shade"), null, parsers);

        Assert.Null(info.ValidValues);
    }

    [Fact]
    public void Option_NonEnum_YieldsNoCandidates()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumHolder));
        var text = descriptor.Options.Single(o => o.LongName == "text");

        Assert.Null(text.EnumCandidateType);
        Assert.Null(text.EnumCandidateNames);

        var property = Prop<EnumHolder>(nameof(EnumHolder.Text));
        var info = SubCommandOptionInfo.FromProperty(property, new CommandOptionAttribute("text"));

        Assert.Null(info.ValidValues);
    }

    [Fact]
    public void Cache_PreservesEnumCandidates_ForPlainAndNullableOptions()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumHolder));
        var shade = descriptor.Options.Single(o => o.LongName == "shade");
        var maybeShade = descriptor.Options.Single(o => o.LongName == "maybe-shade");

        Assert.Equal(typeof(DescriptorShade), shade.EnumCandidateType);
        Assert.Equal(new[] { "Red", "Green", "Blue" }, shade.EnumCandidateNames!);
        Assert.Equal(typeof(DescriptorShade), maybeShade.EnumCandidateType);
        Assert.Equal(new[] { "Red", "Green", "Blue" }, maybeShade.EnumCandidateNames!);
    }

    [Fact]
    public void Cache_SplitsAttributeRequired_FromKeywordRequired()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(KeywordSplitHolder));
        var keyword = descriptor.Options.Single(o => o.LongName == "kw");
        var explicitOption = descriptor.Options.Single(o => o.LongName == "ex");
        var keywordArgument = descriptor.Arguments.Single(a => a.Name == "kwarg");
        var explicitArgument = descriptor.Arguments.Single(a => a.Name == "exarg");

        Assert.False(keyword.Required);
        Assert.True(keyword.IsRequiredByKeyword);
        Assert.True(explicitOption.Required);
        Assert.False(explicitOption.IsRequiredByKeyword);
        Assert.False(keywordArgument.Required);
        Assert.True(keywordArgument.IsRequiredByKeyword);
        Assert.True(explicitArgument.Required);
        Assert.False(explicitArgument.IsRequiredByKeyword);
    }

    [Fact]
    public void OptionChoices_ResolveFromAmong_ThenCandidates()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumHolder));
        var fixedDescriptor = descriptor.Options.Single(o => o.LongName == "fixed");
        var shadeDescriptor = descriptor.Options.Single(o => o.LongName == "shade");
        var textDescriptor = descriptor.Options.Single(o => o.LongName == "text");
        ICommandTypeParserCollection parsers = ApplicationBuilder.Create().AddCommandTypeParser<ShadeParser>();

        Assert.Equal(new[] { "a", "b" }, SubCommandOptionInfo.ResolveValidValues(fixedDescriptor, null)!.Cast<string>());
        Assert.Equal(new[] { "Red", "Green", "Blue" }, SubCommandOptionInfo.ResolveValidValues(shadeDescriptor, null)!.Cast<string>());
        Assert.Null(SubCommandOptionInfo.ResolveValidValues(shadeDescriptor, parsers));
        Assert.Null(SubCommandOptionInfo.ResolveValidValues(textDescriptor, null));
    }

    [Fact]
    public void OptionFromDescriptor_MapsCachedPrimitives()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(KeywordSplitHolder));
        var keyword = descriptor.Options.Single(o => o.LongName == "kw");
        var keywordInfo = SubCommandOptionInfo.FromDescriptor(
            keyword,
            null,
            SubCommandOptionInfo.ResolveValidValues(keyword, null));

        Assert.Equal("kw", keywordInfo.LongName);
        Assert.True(keywordInfo.IsRequired);
        Assert.Equal(typeof(string), keywordInfo.PropertyType);

        var explicitDescriptor = descriptor.Options.Single(o => o.LongName == "ex");
        var explicitInfo = SubCommandOptionInfo.FromDescriptor(
            explicitDescriptor,
            null,
            SubCommandOptionInfo.ResolveValidValues(explicitDescriptor, null));

        Assert.Equal("ex", explicitInfo.LongName);
        Assert.True(explicitInfo.IsRequired);
    }

    [Fact]
    public void ArgumentFromDescriptor_MapsCachedPrimitives()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(FixedArgumentHolder));
        var fixedArgument = descriptor.Arguments.Single(a => a.Name == "fixedarg");
        var info = SubCommandArgumentInfo.FromDescriptor(
            fixedArgument,
            null,
            SubCommandArgumentInfo.ResolveValidValues(fixedArgument, null));

        Assert.Equal("fixedarg", info.Name);
        Assert.Equal(2, info.Position);
        Assert.Equal(new[] { "x", "y" }, info.ValidValues!.Cast<string>());
    }

    [Fact]
    public void Argument_PlainEnum_AutoPopulatesNames()
    {
        var property = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.Shade));
        var info = SubCommandArgumentInfo.FromProperty(property, new CommandArgumentAttribute("shade"));

        Assert.Equal(new[] { "Red", "Green", "Blue" }, info.ValidValues!.Cast<string>());

        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumArgumentHolder));
        var shade = descriptor.Arguments.Single(a => a.Name == "shade");
        var maybeShade = descriptor.Arguments.Single(a => a.Name == "maybe-shade");

        Assert.Equal(typeof(DescriptorShade), shade.EnumCandidateType);
        Assert.Equal(new[] { "Red", "Green", "Blue" }, shade.EnumCandidateNames!);
        Assert.Equal(typeof(DescriptorShade), maybeShade.EnumCandidateType);
        Assert.Equal(new[] { "Red", "Green", "Blue" }, maybeShade.EnumCandidateNames!);
    }

    [Fact]
    public void Argument_NullableEnum_UnwrapsAndPopulatesNames()
    {
        var property = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.MaybeShade));
        var info = SubCommandArgumentInfo.FromProperty(property, new CommandArgumentAttribute("maybe-shade"));

        Assert.Equal(new[] { "Red", "Green", "Blue" }, info.ValidValues!.Cast<string>());
    }

    [Fact]
    public void Argument_ExplicitChoices_WinOverEnumNames()
    {
        var property = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.Shade));
        var info = SubCommandArgumentInfo.FromProperty(
            property,
            new CommandArgumentAttribute("shade") { FromAmong = ["a", "b"] });

        Assert.Equal(new[] { "a", "b" }, info.ValidValues!.Cast<string>());
    }

    [Fact]
    public void Argument_CustomParser_SuppressesAutoPopulate()
    {
        ICommandTypeParserCollection parsers = ApplicationBuilder.Create().AddCommandTypeParser<ShadeParser>();
        var property = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.Shade));
        var info = SubCommandArgumentInfo.FromProperty(property, new CommandArgumentAttribute("shade"), null, parsers);

        Assert.Null(info.ValidValues);
    }

    [Fact]
    public void Argument_NonEnum_YieldsNoCandidates()
    {
        var property = Prop<EnumHolder>(nameof(EnumHolder.Text));
        var info = SubCommandArgumentInfo.FromProperty(property, new CommandArgumentAttribute("text"));

        Assert.Null(info.ValidValues);
    }

    [Fact]
    public void Arguments_ParserSuppression_MatchesCachedResolution()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumArgumentHolder));
        ICommandTypeParserCollection parsers = ApplicationBuilder.Create().AddCommandTypeParser<ShadeParser>();

        var shadeProperty = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.Shade));
        var shadeAttribute = shadeProperty.GetCustomAttribute<CommandArgumentAttribute>()!;
        var shadeDescriptor = descriptor.Arguments.Single(a => a.Name == "shade");

        Assert.Equal(
            new[] { "Red", "Green", "Blue" },
            SubCommandArgumentInfo.FromProperty(shadeProperty, shadeAttribute).ValidValues!.Cast<string>());
        Assert.Equal(
            new[] { "Red", "Green", "Blue" },
            SubCommandArgumentInfo.ResolveValidValues(shadeDescriptor, null)!.Cast<string>());
        Assert.Null(SubCommandArgumentInfo.FromProperty(shadeProperty, shadeAttribute, null, parsers).ValidValues);
        Assert.Null(SubCommandArgumentInfo.ResolveValidValues(shadeDescriptor, parsers));

        var maybeProperty = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.MaybeShade));
        var maybeAttribute = maybeProperty.GetCustomAttribute<CommandArgumentAttribute>()!;
        var maybeDescriptor = descriptor.Arguments.Single(a => a.Name == "maybe-shade");

        Assert.Equal(
            new[] { "Red", "Green", "Blue" },
            SubCommandArgumentInfo.FromProperty(maybeProperty, maybeAttribute).ValidValues!.Cast<string>());
        Assert.Equal(
            new[] { "Red", "Green", "Blue" },
            SubCommandArgumentInfo.ResolveValidValues(maybeDescriptor, null)!.Cast<string>());
        Assert.Null(SubCommandArgumentInfo.FromProperty(maybeProperty, maybeAttribute, null, parsers).ValidValues);
        Assert.Null(SubCommandArgumentInfo.ResolveValidValues(maybeDescriptor, parsers));

        var textProperty = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.Text));
        var textAttribute = textProperty.GetCustomAttribute<CommandArgumentAttribute>()!;
        var textDescriptor = descriptor.Arguments.Single(a => a.Name == "text");

        Assert.Null(SubCommandArgumentInfo.FromProperty(textProperty, textAttribute).ValidValues);
        Assert.Null(SubCommandArgumentInfo.ResolveValidValues(textDescriptor, null));
        Assert.Null(SubCommandArgumentInfo.FromProperty(textProperty, textAttribute, null, parsers).ValidValues);
        Assert.Null(SubCommandArgumentInfo.ResolveValidValues(textDescriptor, parsers));
    }

    [Fact]
    public void Arguments_ExplicitChoices_WinOverEnumNamesOnBothPaths()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumArgumentHolder));
        ICommandTypeParserCollection parsers = ApplicationBuilder.Create().AddCommandTypeParser<ShadeParser>();

        var property = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.Fixed));
        var fixedDescriptor = descriptor.Arguments.Single(a => a.Name == "fixed");

        Assert.Equal(
            new[] { "a", "b" },
            SubCommandArgumentInfo.FromProperty(
                property,
                new CommandArgumentAttribute("fixed") { FromAmong = ["a", "b"] },
                null,
                parsers).ValidValues!.Cast<string>());
        Assert.Equal(
            new[] { "a", "b" },
            SubCommandArgumentInfo.ResolveValidValues(fixedDescriptor, parsers)!.Cast<string>());
    }

    [Fact]
    public void Arguments_NonEnum_FrozenPathStaysNull()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumArgumentHolder));
        var text = descriptor.Arguments.Single(a => a.Name == "text");

        Assert.Null(text.EnumCandidateType);
        Assert.Null(text.EnumCandidateNames);
        Assert.Null(SubCommandArgumentInfo.ResolveValidValues(text, null));
    }

    [Fact]
    public void ArgumentChoices_ResolveFromAmong_ThenCandidates()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumArgumentHolder));
        var shadeDescriptor = descriptor.Arguments.Single(a => a.Name == "shade");
        ICommandTypeParserCollection parsers = ApplicationBuilder.Create().AddCommandTypeParser<ShadeParser>();

        Assert.Equal(new[] { "Red", "Green", "Blue" }, SubCommandArgumentInfo.ResolveValidValues(shadeDescriptor, null)!.Cast<string>());
        Assert.Null(SubCommandArgumentInfo.ResolveValidValues(shadeDescriptor, parsers));
    }

    private static bool ChoiceValuesEqual(object[]? left, object[]? right)
    {
        if (left is null && right is null)
        {
            return true;
        }

        if (left is null || right is null)
        {
            return false;
        }

        return left.SequenceEqual(right);
    }

    private static void AssertOptionParity(SubCommandOptionInfo expected, SubCommandOptionInfo actual)
    {
        Assert.Equal(expected.Property.Name, actual.Property.Name);
        Assert.Equal(expected.Property.DeclaringType, actual.Property.DeclaringType);
        Assert.Equal(expected.PropertyType, actual.PropertyType);
        Assert.Equal(expected.ShortName, actual.ShortName);
        Assert.Equal(expected.LongName, actual.LongName);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.IsRequired, actual.IsRequired);
        Assert.Equal(expected.EnvironmentVariable, actual.EnvironmentVariable);
        Assert.True(ChoiceValuesEqual(expected.ValidValues, actual.ValidValues));
        Assert.Equal(expected.IsCaseSensitive, actual.IsCaseSensitive);
        Assert.Equal(expected.IsSecret, actual.IsSecret);
        Assert.Equal(expected.IsFlag, actual.IsFlag);
        Assert.Equal(expected.IsCollection, actual.IsCollection);
    }

    private static void AssertArgumentParity(SubCommandArgumentInfo expected, SubCommandArgumentInfo actual)
    {
        Assert.Equal(expected.Property.Name, actual.Property.Name);
        Assert.Equal(expected.Property.DeclaringType, actual.Property.DeclaringType);
        Assert.Equal(expected.PropertyType, actual.PropertyType);
        Assert.Equal(expected.Name, actual.Name);
        Assert.Equal(expected.Description, actual.Description);
        Assert.Equal(expected.Position, actual.Position);
        Assert.Equal(expected.IsRequired, actual.IsRequired);
        Assert.True(ChoiceValuesEqual(expected.ValidValues, actual.ValidValues));
        Assert.Equal(expected.IsCaseSensitive, actual.IsCaseSensitive);
        Assert.Equal(expected.IsSecret, actual.IsSecret);
        Assert.Equal(expected.IsCollection, actual.IsCollection);
    }

    private static SubCommandOptionInfo LiveOption(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<CommandOptionAttribute>()!;
        return SubCommandOptionInfo.FromProperty(property, attribute);
    }

    private static SubCommandArgumentInfo LiveArgument(PropertyInfo property)
    {
        var attribute = property.GetCustomAttribute<CommandArgumentAttribute>()!;
        return SubCommandArgumentInfo.FromProperty(property, attribute);
    }

    [Fact]
    public void Options_FromProperty_MatchesFromCommandType_FieldByField()
    {
        var properties = CommandReflectionCache.Walk(typeof(ParityOptionLeaf));
        var commanded = SubCommandOptionInfo.FromCommandType(typeof(ParityOptionLeaf));

        Assert.Equal(8, commanded.Count);
        Assert.Equal(
            new[] { "base-opt", "base-flag", "verbose", "maybe-verbose", "output", "shade", "maybe-shade", "scores" },
            commanded.Select(o => o.LongName));

        var byName = commanded.ToDictionary(o => o.LongName!);
        foreach (var property in properties)
        {
            var attribute = property.GetCustomAttribute<CommandOptionAttribute>();
            if (attribute is null)
            {
                continue;
            }

            var expected = SubCommandOptionInfo.FromProperty(property, attribute);
            AssertOptionParity(expected, byName[expected.LongName!]);
        }

        var output = byName["output"];
        Assert.Equal('o', output.ShortName);
        Assert.Equal("Output path.", output.Description);
        Assert.True(output.IsRequired);
        Assert.Equal("PARITY_OUTPUT", output.EnvironmentVariable);
        Assert.Equal(new[] { "a", "b" }, output.ValidValues!.Cast<string>());
        Assert.True(output.IsCaseSensitive);
        Assert.True(output.IsSecret);
        Assert.False(output.IsFlag);

        var verbose = byName["verbose"];
        Assert.True(verbose.IsFlag);
        Assert.False(verbose.IsCollection);

        Assert.True(byName["maybe-verbose"].IsFlag);
        Assert.True(byName["scores"].IsCollection);
        Assert.False(byName["scores"].IsFlag);
    }

    [Fact]
    public void Options_FromDeclaredType_MatchesLeafDeclaredSubset()
    {
        var commanded = SubCommandOptionInfo.FromCommandType(typeof(ParityOptionLeaf));
        var declared = SubCommandOptionInfo.FromDeclaredType(typeof(ParityOptionLeaf));

        Assert.Equal(new[] { "verbose", "maybe-verbose", "output", "shade", "maybe-shade", "scores" }, declared.Select(o => o.LongName));

        var commandedByName = commanded.ToDictionary(o => o.LongName!);
        foreach (var option in declared)
        {
            Assert.Equal(typeof(ParityOptionLeaf), option.Property.DeclaringType);
            AssertOptionParity(commandedByName[option.LongName!], option);
        }
    }

    [Fact]
    public void Options_FromDescriptor_MatchesLivePaths()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(ParityOptionLeaf));
        var commanded = SubCommandOptionInfo.FromCommandType(typeof(ParityOptionLeaf)).ToDictionary(o => o.LongName!);

        Assert.Equal(commanded.Count, descriptor.Options.Length);

        foreach (var cached in descriptor.Options)
        {
            var info = SubCommandOptionInfo.FromDescriptor(
                cached,
                null,
                SubCommandOptionInfo.ResolveValidValues(cached, null));
            var live = LiveOption(cached.Property);

            AssertOptionParity(live, info);
            AssertOptionParity(commanded[info.LongName!], info);
        }
    }

    [Fact]
    public void Options_NonPublicMembers_AreIncludedOnBothWalks()
    {
        var commanded = SubCommandOptionInfo.FromCommandType(typeof(HiddenMemberHolder));
        var declared = SubCommandOptionInfo.FromDeclaredType(typeof(HiddenMemberHolder));

        Assert.Equal(new[] { "open", "concealed" }, commanded.Select(o => o.LongName));
        Assert.Equal(new[] { "open", "concealed" }, declared.Select(o => o.LongName));
        AssertOptionParity(
            commanded.Single(o => o.LongName == "concealed"),
            declared.Single(o => o.LongName == "concealed"));
    }

    [Fact]
    public void Options_OverrideMember_CharacterizesCurrentWalk()
    {
        var commanded = SubCommandOptionInfo.FromCommandType(typeof(OverridingOptionChild));

        Assert.Equal(new[] { "label", "label" }, commanded.Select(o => o.LongName));
        Assert.Equal(typeof(OverridableOptionBase), commanded[0].Property.DeclaringType);
        Assert.Equal(typeof(OverridingOptionChild), commanded[1].Property.DeclaringType);
        Assert.Equal("Base label.", commanded[0].Description);
        Assert.Equal("Child label.", commanded[1].Description);
    }

    [Fact]
    public void Options_HiddenMember_CharacterizesCurrentWalk()
    {
        var commanded = SubCommandOptionInfo.FromCommandType(typeof(HidingOptionChild));

        Assert.Equal(new[] { "name", "name" }, commanded.Select(o => o.LongName));
        Assert.Equal(typeof(HiddenOptionBase), commanded[0].Property.DeclaringType);
        Assert.Equal(typeof(HidingOptionChild), commanded[1].Property.DeclaringType);
        Assert.Equal("Base name.", commanded[0].Description);
        Assert.Equal("Child name.", commanded[1].Description);
    }

    [Fact]
    public void Options_NoDefaultValueMetadata_AllPathsOmitDefault()
    {
        // No DefaultValue snapshot exists on the node type (removed per
        // ADR-0004): defaults render at help time from live instances, never
        // from descriptors. Construction-path parity is pinned field-by-field
        // by the FromProperty/FromDescriptor tests below, so no per-path
        // loop belongs here (a self-comparison loop would always pass).
        Assert.Null(typeof(SubCommandOptionInfo).GetProperty("DefaultValue"));
    }

    [Fact]
    public void Arguments_FromProperty_MatchesFromCommandType_FieldByField()
    {
        var commanded = SubCommandArgumentInfo.FromCommandType(typeof(ParityArgumentLeaf));

        Assert.Equal(new[] { "head", "middle", "tail", "base-arg", "tags" }, commanded.Select(a => a.Name));

        var byName = commanded.ToDictionary(a => a.Name!);
        foreach (var property in CommandReflectionCache.Walk(typeof(ParityArgumentLeaf)))
        {
            var attribute = property.GetCustomAttribute<CommandArgumentAttribute>();
            if (attribute is null)
            {
                continue;
            }

            var expected = SubCommandArgumentInfo.FromProperty(property, attribute);
            AssertArgumentParity(expected, byName[expected.Name!]);
        }

        var tail = byName["tail"];
        Assert.Equal(2, tail.Position);
        Assert.Equal("Tail value.", tail.Description);
        Assert.Equal(new[] { "x", "y" }, tail.ValidValues!.Cast<string>());
        Assert.True(tail.IsCaseSensitive);
        Assert.True(tail.IsSecret);

        Assert.True(byName["head"].IsRequired);
        Assert.False(byName["middle"].IsRequired);
        Assert.True(byName["tags"].IsCollection);
    }

    [Fact]
    public void Arguments_FromDeclaredType_MatchesLeafDeclaredSubset()
    {
        var commanded = SubCommandArgumentInfo.FromCommandType(typeof(ParityArgumentLeaf));
        var declared = SubCommandArgumentInfo.FromDeclaredType(typeof(ParityArgumentLeaf));

        Assert.Equal(new[] { "head", "middle", "tail", "tags" }, declared.Select(a => a.Name));

        var commandedByName = commanded.ToDictionary(a => a.Name!);
        foreach (var argument in declared)
        {
            Assert.Equal(typeof(ParityArgumentLeaf), argument.Property.DeclaringType);
            AssertArgumentParity(commandedByName[argument.Name!], argument);
        }
    }

    [Fact]
    public void Arguments_FromDeclaredType_SortsByPosition()
    {
        var declared = SubCommandArgumentInfo.FromDeclaredType(typeof(ParityArgumentLeaf));

        Assert.Equal(new[] { 0, 1, 2, 4 }, declared.Select(a => a.Position));
    }

    [Fact]
    public void Arguments_FromDescriptor_MatchesLivePaths()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(ParityArgumentLeaf));
        var commanded = SubCommandArgumentInfo.FromCommandType(typeof(ParityArgumentLeaf)).ToDictionary(a => a.Name!);

        Assert.Equal(commanded.Count, descriptor.Arguments.Length);
        Assert.Equal(new[] { "head", "middle", "tail", "base-arg", "tags" }, descriptor.Arguments.Select(a => a.Name));

        foreach (var cached in descriptor.Arguments)
        {
            var info = SubCommandArgumentInfo.FromDescriptor(
                cached,
                null,
                SubCommandArgumentInfo.ResolveValidValues(cached, null));

            AssertArgumentParity(LiveArgument(cached.Property), info);
            AssertArgumentParity(commanded[info.Name!], info);
        }
    }

    [Fact]
    public void Arguments_NonPublicMembers_AreIncludedOnBothWalks()
    {
        var commanded = SubCommandArgumentInfo.FromCommandType(typeof(HiddenMemberHolder));
        var declared = SubCommandArgumentInfo.FromDeclaredType(typeof(HiddenMemberHolder));

        Assert.Equal(new[] { "concealed-arg", "open-arg" }, commanded.Select(a => a.Name));
        Assert.Equal(new[] { "concealed-arg", "open-arg" }, declared.Select(a => a.Name));
        AssertArgumentParity(
            commanded.Single(a => a.Name == "concealed-arg"),
            declared.Single(a => a.Name == "concealed-arg"));
    }

    [Fact]
    public void Arguments_NoDefaultValueMetadata_AllPathsOmitDefault()
    {
        // No DefaultValue snapshot exists on the node type (removed per
        // ADR-0004): defaults render at help time from live instances, never
        // from descriptors. Construction-path parity is pinned field-by-field
        // by the FromProperty/FromDescriptor tests below, so no per-path
        // loop belongs here (a self-comparison loop would always pass).
        Assert.Null(typeof(SubCommandArgumentInfo).GetProperty("DefaultValue"));
    }

    [Fact]
    public void Options_ParserSuppression_MatchesCachedResolution()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumHolder));
        ICommandTypeParserCollection parsers = ApplicationBuilder.Create().AddCommandTypeParser<ShadeParser>();

        var shadeProperty = Prop<EnumHolder>(nameof(EnumHolder.Shade));
        var shadeAttribute = shadeProperty.GetCustomAttribute<CommandOptionAttribute>()!;
        var shadeDescriptor = descriptor.Options.Single(o => o.LongName == "shade");

        Assert.Equal(
            new[] { "Red", "Green", "Blue" },
            SubCommandOptionInfo.FromProperty(shadeProperty, shadeAttribute).ValidValues!.Cast<string>());
        Assert.Equal(
            new[] { "Red", "Green", "Blue" },
            SubCommandOptionInfo.ResolveValidValues(shadeDescriptor, null)!.Cast<string>());
        Assert.Null(SubCommandOptionInfo.FromProperty(shadeProperty, shadeAttribute, null, parsers).ValidValues);
        Assert.Null(SubCommandOptionInfo.ResolveValidValues(shadeDescriptor, parsers));

        var maybeProperty = Prop<EnumHolder>(nameof(EnumHolder.MaybeShade));
        var maybeAttribute = maybeProperty.GetCustomAttribute<CommandOptionAttribute>()!;
        var maybeDescriptor = descriptor.Options.Single(o => o.LongName == "maybe-shade");

        Assert.Equal(
            new[] { "Red", "Green", "Blue" },
            SubCommandOptionInfo.FromProperty(maybeProperty, maybeAttribute).ValidValues!.Cast<string>());
        Assert.Equal(
            new[] { "Red", "Green", "Blue" },
            SubCommandOptionInfo.ResolveValidValues(maybeDescriptor, null)!.Cast<string>());
        Assert.Null(SubCommandOptionInfo.FromProperty(maybeProperty, maybeAttribute, null, parsers).ValidValues);
        Assert.Null(SubCommandOptionInfo.ResolveValidValues(maybeDescriptor, parsers));

        var textProperty = Prop<EnumHolder>(nameof(EnumHolder.Text));
        var textAttribute = textProperty.GetCustomAttribute<CommandOptionAttribute>()!;
        var textDescriptor = descriptor.Options.Single(o => o.LongName == "text");

        Assert.Null(SubCommandOptionInfo.FromProperty(textProperty, textAttribute).ValidValues);
        Assert.Null(SubCommandOptionInfo.ResolveValidValues(textDescriptor, null));
        Assert.Null(SubCommandOptionInfo.FromProperty(textProperty, textAttribute, null, parsers).ValidValues);
        Assert.Null(SubCommandOptionInfo.ResolveValidValues(textDescriptor, parsers));
    }

    [Fact]
    public void Options_ExplicitChoices_WinOverEnumNamesOnBothPaths()
    {
        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumHolder));
        ICommandTypeParserCollection parsers = ApplicationBuilder.Create().AddCommandTypeParser<ShadeParser>();

        var property = Prop<EnumHolder>(nameof(EnumHolder.Fixed));
        var fixedDescriptor = descriptor.Options.Single(o => o.LongName == "fixed");

        Assert.Equal(
            new[] { "a", "b" },
            SubCommandOptionInfo.FromProperty(
                property,
                new CommandOptionAttribute("fixed") { FromAmong = ["a", "b"] },
                null,
                parsers).ValidValues!.Cast<string>());
        Assert.Equal(
            new[] { "a", "b" },
            SubCommandOptionInfo.ResolveValidValues(fixedDescriptor, parsers)!.Cast<string>());
    }

    private static SubCommandOptionInfo SharedOption<T>() =>
        SubCommandOptionInfo.FromCommandType(typeof(T)).Single(o => o.LongName == "shared");

    private static SubCommandOptionInfo SharedOption(Type commandType) =>
        SubCommandOptionInfo.FromCommandType(commandType).Single(o => o.LongName == "shared");

    private static bool HasGlobalSignature(SubCommandOptionInfo left, SubCommandOptionInfo right) =>
        left.PropertyType == right.PropertyType &&
        left.IsRequired == right.IsRequired &&
        left.IsSecret == right.IsSecret &&
        left.ShortName == right.ShortName &&
        left.LongName == right.LongName &&
        ChoiceValuesEqual(left.ValidValues, right.ValidValues);

    [Fact]
    public void GlobalSignature_IdenticalOptions_WouldPromote()
    {
        var alpha = SharedOption<GlobalProbeAlpha>();
        var beta = SharedOption<GlobalProbeBeta>();

        Assert.True(HasGlobalSignature(alpha, beta));

        var copy = CommandHierarchyBuilder.CreateGlobalOptionCopy(alpha);

        Assert.True(copy.IsGlobal);
        Assert.True(copy.IsInherited);
        Assert.True(HasGlobalSignature(alpha, copy));
        Assert.Equal(alpha.Description, copy.Description);
        Assert.Equal(alpha.EnvironmentVariable, copy.EnvironmentVariable);
    }

    public static TheoryData<Type, string> DivergentProbes => new()
    {
        { typeof(GlobalProbeRequired), nameof(SubCommandOptionInfo.IsRequired) },
        { typeof(GlobalProbeSecret), nameof(SubCommandOptionInfo.IsSecret) },
        { typeof(GlobalProbeShort), nameof(SubCommandOptionInfo.ShortName) },
        { typeof(GlobalProbeChoices), nameof(SubCommandOptionInfo.ValidValues) },
        { typeof(GlobalProbeType), nameof(SubCommandOptionInfo.PropertyType) },
    };

    [Theory]
    [MemberData(nameof(DivergentProbes))]
    public void GlobalSignature_DivergentField_BlocksPromotion(Type divergentType, string field)
    {
        var baseline = SharedOption<GlobalProbeAlpha>();
        var divergent = SharedOption(divergentType);

        Assert.False(HasGlobalSignature(baseline, divergent), $"Expected divergence in {field} to block promotion.");
    }
}
#pragma warning restore CS0618
