using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process property-default tests for the CLI parser.
/// A property initializer is the declared fallback when neither the command line
/// nor the environment supplies a value: omitting the option preserves it.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class OptionDefaultTests
{
    private const string ConfigVariable = "PARKER_OPTIONDEFAULT_CONFIG";

    private static readonly SemaphoreSlim EnvGate = new(1, 1);

    [Command("optdefault", "Probes property initializer preservation.")]
    public sealed class InitializerDefaultCommand : Command
    {
        [CommandOption("text", Description = "Text value.", EnvironmentVariable = ConfigVariable)]
        public string Text { get; set; } = "fallback";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Text: {Text}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task OmittedOption_PreservesPropertyInitializer()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["optdefault"],
            new Dictionary<string, string?> { [ConfigVariable] = null });

        Assert.Equal(0, exitCode);
        Assert.Contains("Text: fallback", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    // White-box exception: DefaultValue metadata has no public surface (help text
    // reads the live property initializer instead), so this pins it directly via
    // reflection. The public fallback behavior is covered above.
    [Fact]
    public void OmittedOption_DefaultValueMetadataStaysUnset()
    {
        var assembly = typeof(ApplicationBuilder).Assembly;
        var infoType = assembly.GetType("ApplicationBuilderHelpers.CommandLineParser.SubCommandOptionInfo")!;
        var property = typeof(InitializerDefaultCommand).GetProperty(nameof(InitializerDefaultCommand.Text))!;
        var attribute = (CommandOptionAttribute)Attribute.GetCustomAttribute(property, typeof(CommandOptionAttribute))!;
        var fromProperty = infoType.GetMethod("FromProperty")!;
        var metadata = fromProperty.Invoke(null, [property, attribute, null, null])!;
        var defaultValue = infoType.GetProperty("DefaultValue")!.GetValue(metadata);

        Assert.Null(defaultValue);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("optdefault-test")
            .SetExecutableTitle("OptDefault Test")
            .SetExecutableDescription("Property default verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<InitializerDefaultCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        string[] args, IReadOnlyDictionary<string, string?>? environment = null)
    {
        await EnvGate.WaitAsync();
        var priors = new Dictionary<string, string?>();
        try
        {
            if (environment is not null)
            {
                foreach (var (name, value) in environment)
                {
                    priors[name] = Environment.GetEnvironmentVariable(name);
                    Environment.SetEnvironmentVariable(name, value);
                }
            }

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
            foreach (var (name, prior) in priors)
            {
                Environment.SetEnvironmentVariable(name, prior);
            }

            EnvGate.Release();
        }
    }
}
