using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Interfaces;
using System.Reflection;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Characterization (golden-master) tests for the CLI descriptor duplication surface:
/// required-keyword detection, display-name mapping, <c>FromCommandType</c> vs
/// <c>FromDeclaredType</c> scoping, and nullable-unwrap/enum-candidate snapshots across
/// <c>SubCommandOptionInfo</c>, <c>SubCommandArgumentInfo</c> and
/// <c>CommandReflectionCache</c>. Pins current outputs before refactor so a later
/// extraction can prove zero drift. Help rendering, inheritance flags, parser-lifetime
/// and per-run copy semantics are intentionally out of scope.
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

    [Theory]
    [InlineData(nameof(DisplayTypeHolder.Text), "TEXT")]
    [InlineData(nameof(DisplayTypeHolder.Number), "NUMBER")]
    [InlineData(nameof(DisplayTypeHolder.Ratio), "NUMBER")]
    [InlineData(nameof(DisplayTypeHolder.Flag), "BOOL")]
    [InlineData(nameof(DisplayTypeHolder.Moment), "DATE")]
    [InlineData(nameof(DisplayTypeHolder.Folder), "DIR")]
    [InlineData(nameof(DisplayTypeHolder.File), "FILE")]
    [InlineData(nameof(DisplayTypeHolder.Duration), "TIMESPAN")]
    [InlineData(nameof(DisplayTypeHolder.Token), "GUID")]
    [InlineData(nameof(DisplayTypeHolder.Endpoint), "URI")]
    [InlineData(nameof(DisplayTypeHolder.MaybeNumber), "NULLABLE`1")]
    [InlineData(nameof(DisplayTypeHolder.Scores), "NUMBER")]
    [InlineData(nameof(DisplayTypeHolder.Names), "TEXT")]
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
        var properties = CommandReflectionCache.GetAllProperties(typeof(ScopingDerived));
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
        var info = SubCommandArgumentInfo.FromDescriptor(fixedArgument, null);

        Assert.Equal("fixedarg", info.Name);
        Assert.Equal(2, info.Position);
        Assert.Equal(new[] { "x", "y" }, info.ValidValues!.Cast<string>());
    }

    [Fact]
    public void Argument_PlainEnum_LeavesValidValuesUnset()
    {
        var property = Prop<EnumArgumentHolder>(nameof(EnumArgumentHolder.Shade));
        var info = SubCommandArgumentInfo.FromProperty(property, new CommandArgumentAttribute("shade"));

        Assert.Null(info.ValidValues);

        var descriptor = new CommandReflectionCache().GetOrAdd(typeof(EnumArgumentHolder));
        var shade = descriptor.Arguments.Single(a => a.Name == "shade");
        var maybeShade = descriptor.Arguments.Single(a => a.Name == "maybe-shade");

        Assert.Equal(typeof(DescriptorShade), shade.EnumCandidateType);
        Assert.Equal(new[] { "Red", "Green", "Blue" }, shade.EnumCandidateNames!);
        Assert.Equal(typeof(DescriptorShade), maybeShade.EnumCandidateType);
        Assert.Equal(new[] { "Red", "Green", "Blue" }, maybeShade.EnumCandidateNames!);
    }
}
