using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Tests for the implementation-less (abstract) root path: a CLI that registers
/// only leaf subcommands has no root implementation, so bare or help-first
/// invocations stay on the abstract-root branch of <c>ArgumentParser</c>.
/// Bare invocations fail with exit 2 naming <c>'&lt;root&gt;'</c> plus a global
/// footer; leading <c>--help</c> renders the global model (COMMANDS section),
/// never a command-scoped view; unknown and post-separator tokens keep their
/// error kinds. Term validation (<c>SubCommandInfo.FromCommand</c> plus the
/// hierarchy build) pins the empty/whitespace/dash-led guard and the
/// whitespace-split normalization shared by both call sites.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class AbstractRootRequiresSubcommandTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets the specified name.")]
    public sealed class RootlessGreetCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("Hello!");
            return ValueTask.CompletedTask;
        }
    }

    [Command("config  hub", "Spaced hub.")]
    public abstract class SpacedHubBase : Command
    {
    }

    [Command("config hub get", "Gets a value.")]
    public sealed class SpacedHubGetCommand : SpacedHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub get");
            return ValueTask.CompletedTask;
        }
    }

    [Command("--hub", "Dash hub.")]
    public abstract class DashLedHubBase : Command
    {
    }

    [Command("hub get", "Gets a value.")]
    public sealed class DashLedHubGetCommand : DashLedHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("hub get");
            return ValueTask.CompletedTask;
        }
    }

    [Command]
    public sealed class NullTermCommand : Command
    {
    }

    [Command("", "Empty term.")]
    public sealed class EmptyTermCommand : Command
    {
    }

    [Command("   ", "Whitespace term.")]
    public sealed class WhitespaceTermCommand : Command
    {
    }

    [Command("\t", "Tab term.")]
    public sealed class TabTermCommand : Command
    {
    }

    [Command("--bogus", "Dash-led term.")]
    public sealed class DashLedTermCommand : Command
    {
    }

    [Command("a  b", "Double-spaced term.")]
    public sealed class DoubleSpacedTermCommand : Command
    {
    }

    [Fact]
    public async Task Bare_Root_Requires_Subcommand_With_Global_Footer()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, []);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.DoesNotContain("Did you mean", error);
        Assert.Contains("Run 'abstract-root-test --help' to see available commands and options.", error);
        Assert.Contains("Run 'abstract-root-test --version' to show version information.", error);
    }

    [Fact]
    public async Task Help_First_With_Command_Name_Shows_Global_Help()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--help", "greet"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.Contains("<COMMAND>", output);
        Assert.Contains("COMMANDS:", output);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.Contains("Run 'abstract-root-test <command> --help' for more information on specific commands.", output);
        Assert.DoesNotContain("abstract-root-test greet", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Help_First_With_Unknown_Flag_Shows_Global_Help()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--help", "--bogus"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.Contains("<COMMAND>", output);
        Assert.Contains("COMMANDS:", output);
        Assert.Contains("GLOBAL OPTIONS:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Unknown_Command_With_Help_Errors_Unknown_Command()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["bogus", "--help"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found for 'bogus'", error);
        Assert.Contains("Run 'abstract-root-test --version' to show version information.", error);
    }

    [Fact]
    public async Task Separator_Before_Help_Keeps_Requires_Subcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["--", "--help"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'<root>' requires a subcommand", error);
        Assert.Contains("Available subcommands: greet", error);
        Assert.Contains("Run 'abstract-root-test --help' to see available commands and options.", error);
    }

    [Fact]
    public void Null_Term_Merges_At_Root()
    {
        var info = SubCommandInfo.FromCommand(typeof(NullTermCommand));

        Assert.Empty(info.CommandParts);
    }

    [Fact]
    public void Empty_Term_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SubCommandInfo.FromCommand(typeof(EmptyTermCommand)));

        Assert.Contains("term must not be empty or whitespace", ex.Message);
    }

    [Fact]
    public void Whitespace_Term_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SubCommandInfo.FromCommand(typeof(WhitespaceTermCommand)));

        Assert.Contains("term must not be empty or whitespace", ex.Message);
    }

    [Fact]
    public void Tab_Term_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SubCommandInfo.FromCommand(typeof(TabTermCommand)));

        Assert.Contains("term must not be empty or whitespace", ex.Message);
    }

    [Fact]
    public void Dash_Led_Term_Throws()
    {
        var ex = Assert.Throws<InvalidOperationException>(() => SubCommandInfo.FromCommand(typeof(DashLedTermCommand)));

        Assert.Contains("command names must not start with '-'", ex.Message);
    }

    [Fact]
    public void Double_Space_Term_Normalizes()
    {
        var info = SubCommandInfo.FromCommand(typeof(DoubleSpacedTermCommand));

        Assert.Equal(["a", "b"], info.CommandParts);
        Assert.Equal("a b", info.FullCommandName);
    }

    [Fact]
    public async Task Spaced_Abstract_Base_Matches_Normalized_Path()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSpacedBuilder, ["config", "hub", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Spaced hub.", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Dash_Led_Abstract_Base_Fails_Build()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateDashLedBuilder, []);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid command term", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("abstract-root-test")
            .SetExecutableTitle("Abstract Root Test")
            .SetExecutableDescription("Abstract root verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<RootlessGreetCommand>();
    }

    private static ApplicationBuilder CreateSpacedBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("abstract-root-test")
            .SetExecutableTitle("Abstract Root Test")
            .SetExecutableDescription("Abstract root verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SpacedHubGetCommand>();
    }

    private static ApplicationBuilder CreateDashLedBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("abstract-root-test")
            .SetExecutableTitle("Abstract Root Test")
            .SetExecutableDescription("Abstract root verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DashLedHubGetCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(Func<ApplicationBuilder> create, string[] args)
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
                var exitCode = await create().RunAsync(args);
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
