using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using System.Reflection;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

public sealed class HelpPlaceholderTests
{
    private enum Shade
    {
        Red,
        Green,
        Blue
    }

    private sealed class PlaceholderHolder
    {
        public byte ByteValue { get; set; }

        public sbyte SByteValue { get; set; }

        public short Int16Value { get; set; }

        public ushort UInt16Value { get; set; }

        public int Int32Value { get; set; }

        public uint UInt32Value { get; set; }

        public long Int64Value { get; set; }

        public ulong UInt64Value { get; set; }

        public float SingleValue { get; set; }

        public double DoubleValue { get; set; }

        public decimal DecimalValue { get; set; }

        public string? Text { get; set; }

        public DateTime Moment { get; set; }

        public DateOnly Day { get; set; }

        public TimeOnly Time { get; set; }

        public DateTimeOffset Stamp { get; set; }

        public FileInfo? File { get; set; }

        public AbsolutePath? Path { get; set; }

        public DirectoryInfo? Folder { get; set; }

        public TimeSpan Duration { get; set; }

        public Guid Token { get; set; }

        public Uri? Endpoint { get; set; }

        public Version? Build { get; set; }

        public char Letter { get; set; }

        public Shade Shade { get; set; }

        public int? MaybeNumber { get; set; }

        public Guid? MaybeToken { get; set; }

        public DateTime? MaybeMoment { get; set; }

        public bool? MaybeFlag { get; set; }

        public List<string> Names { get; set; } = [];

        public List<decimal> Scores { get; set; } = [];

        public List<bool> Flags { get; set; } = [];

        public List<bool?> MaybeFlags { get; set; } = [];
    }

    private static PropertyInfo Prop(string name) => typeof(PlaceholderHolder).GetProperty(name)!;

    private static SubCommandOptionInfo Option(string name) =>
        SubCommandOptionInfo.FromProperty(Prop(name), new CommandOptionAttribute("probe"));

    [Theory]
    [InlineData(nameof(PlaceholderHolder.ByteValue), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.SByteValue), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.Int16Value), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.UInt16Value), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.Int32Value), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.UInt32Value), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.Int64Value), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.UInt64Value), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.SingleValue), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.DoubleValue), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.DecimalValue), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.Text), "STRING")]
    [InlineData(nameof(PlaceholderHolder.Moment), "DATE")]
    [InlineData(nameof(PlaceholderHolder.Day), "DATE")]
    [InlineData(nameof(PlaceholderHolder.Time), "DATE")]
    [InlineData(nameof(PlaceholderHolder.Stamp), "DATE")]
    [InlineData(nameof(PlaceholderHolder.File), "FILE")]
    [InlineData(nameof(PlaceholderHolder.Path), "FILE")]
    [InlineData(nameof(PlaceholderHolder.Folder), "DIR")]
    [InlineData(nameof(PlaceholderHolder.Duration), "VALUE")]
    [InlineData(nameof(PlaceholderHolder.Token), "VALUE")]
    [InlineData(nameof(PlaceholderHolder.Endpoint), "VALUE")]
    [InlineData(nameof(PlaceholderHolder.Build), "VALUE")]
    [InlineData(nameof(PlaceholderHolder.Letter), "VALUE")]
    [InlineData(nameof(PlaceholderHolder.Shade), "VALUE")]
    [InlineData(nameof(PlaceholderHolder.MaybeNumber), "NUMBER")]
    [InlineData(nameof(PlaceholderHolder.MaybeToken), "VALUE")]
    [InlineData(nameof(PlaceholderHolder.MaybeMoment), "DATE")]
    [InlineData(nameof(PlaceholderHolder.Names), "STRING")]
    [InlineData(nameof(PlaceholderHolder.Scores), "NUMBER")]
    public void Option_TypeName_MapsToExpectedToken(string propertyName, string expected)
    {
        Assert.Equal(expected, Option(propertyName).GetTypeName());
    }

    [Fact]
    public void NullableFlag_IsFlag_And_SignatureOmitsPlaceholder()
    {
        var option = Option(nameof(PlaceholderHolder.MaybeFlag));

        Assert.True(option.IsFlag);
        Assert.Equal("--probe", option.GetSignature());
    }

    [Fact]
    public void ScalarDecimal_Signature_ShowsNumberWithoutEllipsis()
    {
        Assert.Equal("--probe <NUMBER>", Option(nameof(PlaceholderHolder.DecimalValue)).GetSignature());
    }

    [Fact]
    public void DecimalList_Signature_ShowsNumberEllipsis()
    {
        var option = Option(nameof(PlaceholderHolder.Scores));

        Assert.Equal("NUMBER", option.GetTypeName());
        Assert.Equal("--probe <NUMBER...>", option.GetSignature());
    }

    [Fact]
    public void StringList_Signature_ShowsStringEllipsis()
    {
        var option = Option(nameof(PlaceholderHolder.Names));

        Assert.Equal("STRING", option.GetTypeName());
        Assert.Equal("--probe <STRING...>", option.GetSignature());
    }

    [Theory]
    [InlineData(nameof(PlaceholderHolder.Flags))]
    [InlineData(nameof(PlaceholderHolder.MaybeFlags))]
    public void BoolList_Signature_CollapsesBoolToValueEllipsis(string propertyName)
    {
        var option = Option(propertyName);

        Assert.Equal("--probe <VALUE...>", option.GetSignature());
    }
}
