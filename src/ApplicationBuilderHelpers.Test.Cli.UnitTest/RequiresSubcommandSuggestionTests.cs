using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for did-you-mean suggestions around the abstract <c>config</c> hub.
/// The <c>config</c> hub below is abstract (no <c>Run</c> body), so bare,
/// far-miss, and post-separator invocations stay on the abstract branch of
/// <c>ArgumentParser</c>: exit 2 with the subcommand list (kind
/// <c>RequiresSubcommand</c>). A near-miss first surplus token instead
/// reports <c>Unknown subcommand '...'</c> with a <c>Did you mean '...'? </c>
/// hint (kind <c>UnknownCommand</c>, leaf-identical, exit 2 unchanged).
/// Exit-code contract: every error scenario below fails with exit code 2
/// (usage error per the structured CommandErrorKind contract); each kind is
/// pinned via its distinct help footer.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class RequiresSubcommandSuggestionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("config", "Abstract config hub.")]
    public abstract class AbstractConfigHub : Command
    {
    }

    [Command("config get", "Gets a config value.")]
    public sealed class AbstractConfigGetCommand : AbstractConfigHub
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config set", "Sets a config value.")]
    public sealed class AbstractConfigSetCommand : AbstractConfigHub
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("config set");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task Bare_Abstract_Lists_Subcommands_Without_Hint()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("'config' requires a subcommand", error);
        Assert.Contains("Available subcommands: get, set", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'didyoumean-abstract-test config --help' to see available subcommands and options.", error);
    }

    [Fact]
    public async Task Near_Miss_Suggests_Canonical_Child()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "gett"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown subcommand 'gett'", error);
        Assert.Contains("Did you mean 'get'?", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Available subcommands", error);
        Assert.Contains("Run 'didyoumean-abstract-test config --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Near_Miss_Case_Insensitive_Suggests_Canonical_Child()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "Gett"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown subcommand 'Gett'", error);
        Assert.Contains("Did you mean 'get'?", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Available subcommands", error);
        Assert.Contains("Run 'didyoumean-abstract-test config --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Far_Miss_Stays_Silent()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "zzzqqq"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("'config' requires a subcommand", error);
        Assert.Contains("Available subcommands: get, set", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'didyoumean-abstract-test config --help' to see available subcommands and options.", error);
    }

    [Fact]
    public async Task Dash_Led_Token_Reports_Unknown_Option()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "--gett"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --gett", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'didyoumean-abstract-test config --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Dash_Led_Near_Miss_Suggests_Known_Option()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "--hepl"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --hepl", error);
        Assert.Contains("Did you mean '--help'?", error);
        Assert.Contains("Run 'didyoumean-abstract-test config --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Post_Separator_Token_Stays_Silent()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "--", "gett"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("'config' requires a subcommand", error);
        Assert.Contains("Available subcommands: get, set", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'didyoumean-abstract-test config --help' to see available subcommands and options.", error);
    }

    [Fact]
    public async Task Multi_Token_Suggests_First_Token_Only()
    {
        var (exitCode, _, error) = await RunCapturedAsync(["config", "gett", "extra"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown subcommand 'gett'", error);
        Assert.Contains("Did you mean 'get'?", error);
        Assert.DoesNotContain("requires a subcommand", error);
        Assert.DoesNotContain("Available subcommands", error);
        Assert.Contains("Run 'didyoumean-abstract-test config --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Near_Miss_With_Help_Shows_Parent_Help()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["config", "gett", "--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("Abstract config hub.", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Abstract_With_Version_Shows_Version()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["config", "--version"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("9.9.9", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Near_Miss_With_Version_Shows_Version()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["config", "gett", "--version"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("9.9.9", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("didyoumean-abstract-test")
            .SetExecutableTitle("DidYouMean Abstract Test")
            .SetExecutableDescription("Did-you-mean abstract verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<AbstractConfigGetCommand>()
            .AddCommand<AbstractConfigSetCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
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
            ConsoleGate.Release();
        }
    }
}
