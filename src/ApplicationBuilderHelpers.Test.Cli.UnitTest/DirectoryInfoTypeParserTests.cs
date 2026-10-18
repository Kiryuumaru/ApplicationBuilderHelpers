using ApplicationBuilderHelpers.Abstracts;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process coverage for the built-in <c>DirectoryInfo</c> type parser.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point: help renders the <c>DIR</c> placeholder, every CLI value form
/// (long/short, space/equals/attached, order-swapped, relative, nonexistent)
/// binds without an existence check, empty or whitespace input stays on the
/// InvalidValue path (exit 2), and a user-registered <c>DirectoryInfo</c>
/// parser replaces the built-in.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class DirectoryInfoTypeParserTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("diropt", "Probes DirectoryInfo option binding.")]
    public sealed class DirectoryOptionCommand : Command
    {
        [CommandOption('d', "dir", Description = "Directory path.")]
        public DirectoryInfo? Dir { get; set; }

        [CommandOption('e', "extra", Description = "Extra value.")]
        public string? Extra { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Dir: {Dir?.ToString() ?? "null"}");
            Console.WriteLine($"Extra: {Extra ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("dirpos", "Probes DirectoryInfo positional binding.")]
    public sealed class DirectoryArgumentCommand : Command
    {
        [CommandArgument("pos", Description = "Positional directory.", Position = 0)]
        public DirectoryInfo? Pos { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Pos: {Pos?.ToString() ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Custom <see cref="DirectoryInfo"/> parser registered through the public
    /// <c>AddCommandTypeParser</c> entry point (see <c>docs/custom-type-parsers.md</c>).
    /// Binds the alias <c>workspace</c> to <c>custom-workspace</c> and rejects
    /// every other value, so replacing the built-in parser is observable.
    /// </summary>
    public sealed class AliasDirectoryParser : CommandTypeParser<DirectoryInfo>
    {
        public override DirectoryInfo? ParseValue(string? value, out string? validateError)
        {
            if (string.Equals(value, "workspace", StringComparison.Ordinal))
            {
                validateError = null;
                return new DirectoryInfo("custom-workspace");
            }

            validateError = $"'{value}' is not a workspace alias.";
            return default;
        }
    }

    public static TheoryData<string[], string> OptionForms => new()
    {
        { ["diropt", "--dir", "/tmp"], "Dir: /tmp" },
        { ["diropt", "--dir=/tmp"], "Dir: /tmp" },
        { ["diropt", "-d", "/tmp"], "Dir: /tmp" },
        { ["diropt", "-d/tmp"], "Dir: /tmp" },
        { ["diropt", "-d=/tmp"], "Dir: /tmp" },
        { ["diropt", "--dir=./rel"], "Dir: ./rel" },
        { ["diropt", "--dir=rel"], "Dir: rel" },
        { ["diropt", "--dir=."], "Dir: ." },
    };

    [Fact]
    public async Task Help_DirectoryInfoOption_RendersDirPlaceholder()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["diropt", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Contains("    -d, --dir <DIR>", Normalize(output));
    }

    [Theory]
    [MemberData(nameof(OptionForms))]
    public async Task OptionValueForms_BindGivenDirectory(string[] args, string expected)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), args);

        Assert.Equal(0, exitCode);
        Assert.Contains(expected, output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task OptionValueForms_OrderSwapped_BindBothOptions()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["diropt", "--extra", "x", "--dir", "/tmp"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Dir: /tmp", output);
        Assert.Contains("Extra: x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task OptionValueForms_ReverseOrder_BindBothOptions()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["diropt", "--dir", "/tmp", "--extra", "x"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Dir: /tmp", output);
        Assert.Contains("Extra: x", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NonexistentDirectory_BindsWithoutExistenceCheck()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["diropt", "--dir=/no-such-dir-9f3c1a"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Dir: /no-such-dir-9f3c1a", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Positional_AbsoluteDirectory_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["dirpos", "/tmp"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Pos: /tmp", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Positional_RelativeDirectory_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["dirpos", "./rel"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Pos: ./rel", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Theory]
    [InlineData("--dir=")]
    [InlineData("-d=")]
    public async Task EmptyEqualsValue_ReportsRegistryError(string token)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["diropt", token]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value '' for option '--dir'", error);
        Assert.Contains("Expected a valid DirectoryInfo.", error);
    }

    [Theory]
    [InlineData("--dir")]
    [InlineData("-d")]
    public async Task EmptySeparatedValue_ReportsRegistryError(string token)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["diropt", token, ""]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value '' for option '--dir'", error);
        Assert.Contains("Expected a non-empty file path.", error);
    }

    [Theory]
    [InlineData("--dir")]
    [InlineData("-d")]
    public async Task WhitespaceValue_ReportsRegistryError(string token)
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["diropt", token, "   "]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value '   ' for option '--dir'", error);
        Assert.Contains("Expected a non-empty file path.", error);
    }

    [Fact]
    public async Task WhitespaceEqualsValue_ReportsRegistryError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(), ["diropt", "--dir=   "]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value '   ' for option '--dir'", error);
        Assert.Contains("Expected a non-empty file path.", error);
    }

    [Fact]
    public async Task RegisteredCustomParser_TakesPrecedenceOverBuiltInParser()
    {
        var builder = CreateBuilder();

        var builtIn = await RunCapturedAsync(builder, ["diropt", "--dir=workspace"]);

        Assert.Equal(0, builtIn.ExitCode);
        Assert.Contains("Dir: workspace", builtIn.Output);
        Assert.True(string.IsNullOrWhiteSpace(builtIn.Error), $"Expected empty stderr but got: {builtIn.Error}");

        builder.AddCommandTypeParser<AliasDirectoryParser>();

        var overridden = await RunCapturedAsync(builder, ["diropt", "--dir=workspace"]);

        Assert.Equal(0, overridden.ExitCode);
        Assert.Contains("Dir: custom-workspace", overridden.Output);
        Assert.True(string.IsNullOrWhiteSpace(overridden.Error), $"Expected empty stderr but got: {overridden.Error}");

        var rejected = await RunCapturedAsync(builder, ["diropt", "--dir=/tmp"]);

        Assert.Equal(2, rejected.ExitCode);
        Assert.True(string.IsNullOrWhiteSpace(rejected.Output), $"Expected empty stdout but got: {rejected.Output}");
        Assert.Contains("'/tmp' is not a workspace alias.", rejected.Error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("directory-parser-test")
            .SetExecutableTitle("Directory Parser Test")
            .SetExecutableDescription("DirectoryInfo parser verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DirectoryOptionCommand>()
            .AddCommand<DirectoryArgumentCommand>();
    }

    private static string Normalize(string text) =>
        text.Replace("\r\n", "\n", StringComparison.Ordinal);

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
