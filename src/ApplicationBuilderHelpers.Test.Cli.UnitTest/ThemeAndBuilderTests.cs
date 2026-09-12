using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Themes;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process coverage for Brunel top targets: console themes, command-builder
/// help/theme/command extensions, and the small public-surface APIs around them.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because help/command
/// runs capture the process-global console streams.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ThemeAndBuilderTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("theme-probe", "Probe command for builder round-trips.")]
    public sealed class ThemeProbeCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("probe-ok");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("timespan-probe", "Probe command for custom type parsers.")]
    public sealed class TimeSpanProbeCommand : Command
    {
        [CommandOption("delay", Description = "Delay value.")]
        public TimeSpan Delay { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"Delay: {Delay}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    public sealed class ProbeDependency : ApplicationDependency
    {
    }

    public sealed class IntProbeParser : CommandTypeParser<int>
    {
        public override int ParseValue(string? value, out string? validateError)
        {
            if (int.TryParse(value, out var result))
            {
                validateError = null;
                return result;
            }

            validateError = $"Invalid Int32 value: '{value}'.";
            return default;
        }
    }

    public sealed class ProbeTimeSpanParser : CommandTypeParser<TimeSpan>
    {
        public override TimeSpan ParseValue(string? value, out string? validateError)
        {
            if (TimeSpan.TryParse(value, out var result))
            {
                validateError = null;
                return result;
            }

            validateError = $"Invalid TimeSpan value: '{value}'.";
            return default;
        }
    }

    [Fact]
    public void AllThemes_ExposeExpectedColors()
    {
        AssertTheme(new DefaultConsoleTheme(), ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.Gray, ConsoleColor.Red);
        AssertTheme(new MonochromeConsoleTheme(), ConsoleColor.White, ConsoleColor.Gray, ConsoleColor.DarkGray, ConsoleColor.White, ConsoleColor.DarkGray, ConsoleColor.White);
        AssertTheme(new HighContrastConsoleTheme(), ConsoleColor.Yellow, ConsoleColor.Cyan, ConsoleColor.Magenta, ConsoleColor.White, ConsoleColor.Gray, ConsoleColor.Red);
        AssertTheme(new MinimalConsoleTheme(), ConsoleColor.Blue, ConsoleColor.DarkCyan, ConsoleColor.DarkBlue, ConsoleColor.Gray, ConsoleColor.DarkGray, ConsoleColor.DarkRed);
        AssertTheme(new DarkConsoleTheme(), ConsoleColor.Magenta, ConsoleColor.Green, ConsoleColor.Cyan, ConsoleColor.White, ConsoleColor.DarkGray, ConsoleColor.Red);
        AssertTheme(new LightConsoleTheme(), ConsoleColor.DarkBlue, ConsoleColor.DarkGreen, ConsoleColor.DarkCyan, ConsoleColor.Black, ConsoleColor.DarkGray, ConsoleColor.DarkRed);

        static void AssertTheme(
            IConsoleTheme theme,
            ConsoleColor header,
            ConsoleColor flag,
            ConsoleColor parameter,
            ConsoleColor description,
            ConsoleColor secondary,
            ConsoleColor required)
        {
            Assert.Equal(header, theme.HeaderColor);
            Assert.Equal(flag, theme.FlagColor);
            Assert.Equal(parameter, theme.ParameterColor);
            Assert.Equal(description, theme.DescriptionColor);
            Assert.Equal(secondary, theme.SecondaryColor);
            Assert.Equal(required, theme.RequiredColor);
        }
    }

    [Fact]
    public void AllThemes_InstancesAreSingletons()
    {
        Assert.Same(DefaultConsoleTheme.Instance, DefaultConsoleTheme.Instance);
        Assert.Same(MonochromeConsoleTheme.Instance, MonochromeConsoleTheme.Instance);
        Assert.Same(HighContrastConsoleTheme.Instance, HighContrastConsoleTheme.Instance);
        Assert.Same(MinimalConsoleTheme.Instance, MinimalConsoleTheme.Instance);
        Assert.Same(DarkConsoleTheme.Instance, DarkConsoleTheme.Instance);
        Assert.Same(LightConsoleTheme.Instance, LightConsoleTheme.Instance);

        Assert.IsType<DefaultConsoleTheme>(DefaultConsoleTheme.Instance);
        Assert.IsType<MonochromeConsoleTheme>(MonochromeConsoleTheme.Instance);
        Assert.IsType<HighContrastConsoleTheme>(HighContrastConsoleTheme.Instance);
        Assert.IsType<MinimalConsoleTheme>(MinimalConsoleTheme.Instance);
        Assert.IsType<DarkConsoleTheme>(DarkConsoleTheme.Instance);
        Assert.IsType<LightConsoleTheme>(LightConsoleTheme.Instance);
    }

    [Fact]
    public async Task SetTheme_PerTheme_HelpRendersSuccessfully()
    {
        IConsoleTheme[] themes =
        [
            DefaultConsoleTheme.Instance,
            MonochromeConsoleTheme.Instance,
            HighContrastConsoleTheme.Instance,
            MinimalConsoleTheme.Instance,
            DarkConsoleTheme.Instance,
            LightConsoleTheme.Instance,
        ];

        foreach (var theme in themes)
        {
            var builder = CreateBuilder().SetTheme(theme);
            var (exitCode, output, error) = await RunCapturedAsync(builder, ["--help"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("USAGE:", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Theme {theme.GetType().Name}: expected empty stderr but got: {error}");
        }
    }

    [Fact]
    public void SetTheme_ByInstance_ReturnsSameBuilder()
    {
        var builder = CreateBuilder();
        var result = ICommandBuilderExtensions.SetTheme(builder, MonochromeConsoleTheme.Instance);

        Assert.Same(builder, result);
    }

    [Fact]
    public void SetTheme_ByGeneric_ReturnsSameBuilder()
    {
        var builder = CreateBuilder();
        var result = builder.SetTheme<DarkConsoleTheme>();

        Assert.Same(builder, result);
    }

    [Fact]
    public async Task SetHelpWidth_AcceptsValueAndRejectsNegative()
    {
        var builder = CreateBuilder();
        var result = ICommandBuilderExtensions.SetHelpWidth(builder, 120);

        Assert.Same(builder, result);
        Assert.Throws<ArgumentOutOfRangeException>(() => ICommandBuilderExtensions.SetHelpWidth(CreateBuilder(), -1));

        var (exitCode, output, _) = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
    }

    [Fact]
    public async Task SetHelpBorderWidth_AcceptsValueAndRejectsNegative()
    {
        var builder = CreateBuilder();
        var result = ICommandBuilderExtensions.SetHelpBorderWidth(builder, 2);

        Assert.Same(builder, result);
        Assert.Throws<ArgumentOutOfRangeException>(() => ICommandBuilderExtensions.SetHelpBorderWidth(CreateBuilder(), -1));

        var (exitCode, output, _) = await RunCapturedAsync(builder, ["--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
    }

    [Fact]
    public void BuilderExtensions_NullBuilder_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => ICommandBuilderExtensions.SetHelpWidth<ApplicationBuilder>(null!, 80));
        Assert.Throws<ArgumentNullException>(() => ICommandBuilderExtensions.SetHelpBorderWidth<ApplicationBuilder>(null!, 2));
        Assert.Throws<ArgumentNullException>(() => ICommandBuilderExtensions.SetTheme<ApplicationBuilder>(null!, DefaultConsoleTheme.Instance));
        Assert.Throws<ArgumentNullException>(() => ICommandBuilderExtensions.AddCommand<ApplicationBuilder>(null!, new ThemeProbeCommand()));
    }

    [Fact]
    public async Task AddCommand_ByInstance_RoundTrip()
    {
        var builder = CreateBuilder();
        var result = ICommandBuilderExtensions.AddCommand(builder, new ThemeProbeCommand());

        Assert.Same(builder, result);

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["theme-probe"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("probe-ok", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task AddCommand_ByGeneric_RoundTrip()
    {
        var builder = CreateBuilder();
        var result = builder.AddCommand<ThemeProbeCommand>();

        Assert.Same(builder, result);

        var (exitCode, output, error) = await RunCapturedAsync(builder, ["theme-probe"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("probe-ok", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public void CommandTypeParserBase_GetStringHandlesNullTypedAndMismatched()
    {
        ICommandTypeParser parser = new IntProbeParser();

        Assert.Equal("0", parser.GetString(null));
        Assert.Equal("42", parser.GetString(42));
        Assert.Equal("oops", parser.GetString("oops"));
    }

    [Fact]
    public void CommandTypeParserBase_GetDefaultValueReturnsDefault()
    {
        ICommandTypeParser parser = new IntProbeParser();

        Assert.Equal(0, parser.GetDefaultValue());
        Assert.Equal(typeof(int), parser.Type);
    }

    [Fact]
    public void CommandException_IntConstructor_ExposesExitCode()
    {
        var exception = new CommandException(42);

        Assert.Equal(42, exception.ExitCode);
    }

    [Fact]
    public void CommandOptionAttribute_CharConstructor_ExposesShortTerm()
    {
        var attribute = new CommandOptionAttribute('x');

        Assert.Equal('x', attribute.ShortTerm);
        Assert.Null(attribute.Term);
    }

    [Fact]
    public async Task AddApplicationAndAddCommandTypeParser_OnRealBuilder()
    {
        var builder = CreateBuilder();
        var afterApplication = builder.AddApplication<ProbeDependency>();
        var afterParser = builder.AddCommandTypeParser<ProbeTimeSpanParser>();

        Assert.Same(builder, afterApplication);
        Assert.Same(builder, afterParser);

        builder.AddCommand<TimeSpanProbeCommand>();
        var (exitCode, output, error) = await RunCapturedAsync(builder, ["timespan-probe", "--delay=00:01:30"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Delay: 00:01:30", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("theme-builder-test")
            .SetExecutableTitle("Theme Builder Test")
            .SetExecutableDescription("Theme and builder verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ThemeProbeCommand>();
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
