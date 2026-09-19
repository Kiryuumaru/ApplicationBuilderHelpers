using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Exact frozen golden tests for CLI help rendering.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point and pins the full help bytes (layout, wrapping, column sharing) so the
/// pending HelpFormatter split stays byte-identical. Captures the process-global console
/// streams, so joins the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class HelpLayoutGoldenTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    // Goldens are exact stdout bytes with \n endings. Raw-string closers sit
    // at column 0 so no line carries stray indentation; the final content
    // line has NO trailing newline in source, then + "\n" pins the single
    // (global) or double (command) trailing newline the formatter emits.

    [Command("deploy", "Deploy the application to the target environment.")]
    public sealed class DeployCommand : Command
    {
        [CommandOption('f', "format", Description = "Output format for the deployment report.", FromAmong = ["table", "json", "yaml"], EnvironmentVariable = "GOLDEN_FORMAT")]
        public string Format { get; set; } = "table";

        [CommandOption("dry-run", Description = "Preview the deployment without applying any changes.")]
        public bool DryRun { get; set; }

        [CommandArgument("target", Description = "Deployment target environment.", Position = 0, Required = true)]
        public string? Target { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"deploy:{Format}:{Target}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("nest alpha", "Runs nest alpha.")]
    public sealed class NestAlphaCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("nest alpha ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("nest beta", "Runs nest beta.")]
    public sealed class NestBetaCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine("nest beta ran");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    private const string GlobalHelpAt120 = """
        golden-test v9.9.9 - Golden Test

        USAGE:
        golden-test [OPTIONS] <COMMAND> [ARGS...]

        DESCRIPTION:
        Golden verification CLI.

        COMMANDS:
            deploy            Deploy the application to the target environment.
            nest              Commands for nest

        GLOBAL OPTIONS:
            -h, --help        Show help information

        Run 'golden-test <command> --help' for more information on specific commands.
        """ + "\n";

    private const string CommandHelpAt120 = """
        golden-test v9.9.9 - Golden Test

        USAGE:
        golden-test deploy [OPTIONS] <TARGET>

        DESCRIPTION:
        Deploy the application to the target environment.

        OPTIONS (command):
            -f, --format <STRING>  Output format for the deployment report.
                                   Possible values: table, json, yaml
                                   Environment variable: GOLDEN_FORMAT
                                   Default: table
            --dry-run              Preview the deployment without applying any changes.
                                   Default: False

        ARGUMENTS:
            <target>               Deployment target environment.

        GLOBAL OPTIONS:
            -h, --help             Show help information

        """ + "\n";

    private const string CommandHelpAt60 = """
        golden-test v9.9.9 - Golden Test

        USAGE:
        golden-test deploy [OPTIONS] <TARGET>

        DESCRIPTION:
        Deploy the application to the target environment.

        OPTIONS (command):
            -f, --format <STRING>
        Output format for the deployment report.
            Possible values: table, json, yaml
            Environment variable: GOLDEN_FORMAT
            Default: table
            --dry-run         Preview the deployment without
                              applying any
                              changes.
                              Default: False

        ARGUMENTS:
            <target>          Deployment target environment.

        GLOBAL OPTIONS:
            -h, --help        Show help information

        """ + "\n";

    [Fact]
    public async Task GlobalHelp_AtDefaultWidth_MatchesGolden()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(120), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Equal(GlobalHelpAt120, Normalize(output));
        Assert.All(
            Normalize(output).Split('\n'),
            line => Assert.True(line.Length <= 120, $"Line exceeds width 120 ({line.Length}): {line}"));
    }

    [Fact]
    public async Task CommandHelp_AtDefaultWidth_MatchesGolden()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(120), ["deploy", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        Assert.Equal(CommandHelpAt120, Normalize(output));
        Assert.All(
            Normalize(output).Split('\n'),
            line => Assert.True(line.Length <= 120, $"Line exceeds width 120 ({line.Length}): {line}"));
    }

    [Fact]
    public async Task CommandHelp_AtNarrowWidth_MatchesGolden()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(60), ["deploy", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        var normalized = Normalize(output);
        Assert.Equal(CommandHelpAt60, normalized);
        Assert.All(
            normalized.Split('\n'),
            line => Assert.True(line.Length <= 60, $"Line exceeds width 60 ({line.Length}): {line}"));
        Assert.Contains("    -f, --format <STRING>\nOutput format for the deployment report.", normalized);
    }

    [Fact]
    public async Task GlobalHelp_SectionsShareSingleLeftColumnWidth()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(120), ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        var offsets = DescriptionColumnOffsets(Normalize(output));
        Assert.True(offsets.Count >= 2, "Expected entries in at least two sections.");
        Assert.Single(offsets.Distinct());
    }

    [Fact]
    public async Task CommandHelp_SectionsShareSingleLeftColumnWidth()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder(120), ["deploy", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        var offsets = DescriptionColumnOffsets(Normalize(output));
        Assert.True(offsets.Count >= 2, "Expected entries in at least two sections.");
        Assert.Single(offsets.Distinct());
    }

    private static List<int> DescriptionColumnOffsets(string output)
    {
        var offsets = new List<int>();
        foreach (var line in output.Split('\n'))
        {
            if (!line.StartsWith("    ", StringComparison.Ordinal))
            {
                continue;
            }

            var body = line[4..].TrimEnd();
            if (body.Length == 0 || char.IsWhiteSpace(body[0]))
            {
                continue;
            }

            var firstSpace = body.IndexOf(' ');
            if (firstSpace < 0)
            {
                continue;
            }

            var descriptionStart = firstSpace;
            while (descriptionStart < body.Length && body[descriptionStart] == ' ')
            {
                descriptionStart++;
            }

            if (descriptionStart - firstSpace < 2 || descriptionStart >= body.Length)
            {
                continue;
            }

            offsets.Add(4 + descriptionStart);
        }

        return offsets;
    }

    private static ApplicationBuilder CreateBuilder(int helpWidth)
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("golden-test")
            .SetExecutableTitle("Golden Test")
            .SetExecutableDescription("Golden verification CLI.")
            .SetExecutableVersion("9.9.9")
            .SetHelpWidth(helpWidth)
            .AddCommand<DeployCommand>()
            .AddCommand<NestAlphaCommand>()
            .AddCommand<NestBetaCommand>();
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
