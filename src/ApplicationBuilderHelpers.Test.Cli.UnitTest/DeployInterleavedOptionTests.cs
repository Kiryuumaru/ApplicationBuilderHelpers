using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

[Collection("ConsoleDecoupling")]
public sealed class DeployInterleavedOptionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("deploy prod", "Deploy to production.")]
    public sealed class DeployProdCommand : Command
    {
        [CommandOption('f', "force", Description = "Force deployment.")]
        public bool Force { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"deploy prod:{Force}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy staging", "Deploy to staging.")]
    public sealed class DeployStagingCommand : Command
    {
        [CommandOption('f', "fast", Description = "Fast deployment.")]
        public bool Fast { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"deploy staging:{Fast}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy prod-east", "Deploy to production east.")]
    public sealed class DeployProdSharedValueCommand : Command
    {
        [CommandOption("config", Description = "Config file path.")]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"deploy prod-east:{Config}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy prod-west", "Deploy to production west.")]
    public sealed class DeployProdSharedFlagCommand : Command
    {
        [CommandOption("config", Description = "Config toggle.")]
        public bool Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"deploy prod-west:{Config}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy prod-west", "Deploy to production west.")]
    public sealed class DeployProdWestSharedValueCommand : Command
    {
        [CommandOption("config", Description = "Config file path for west.")]
        public string? Config { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"deploy prod-west:{Config}");
            return ValueTask.CompletedTask;
        }
    }

    private static ApplicationBuilder CreateSingle()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("verify634")
            .SetExecutableTitle("Verify 634")
            .SetExecutableDescription("Verify.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DeployProdCommand>();
    }

    private static ApplicationBuilder CreateTwo()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("verify634")
            .SetExecutableTitle("Verify 634")
            .SetExecutableDescription("Verify.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DeployProdCommand>()
            .AddCommand<DeployStagingCommand>();
    }

    private static ApplicationBuilder CreateSameLong()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("verify634")
            .SetExecutableTitle("Verify 634")
            .SetExecutableDescription("Verify.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DeployProdSharedValueCommand>()
            .AddCommand<DeployProdSharedFlagCommand>();
    }

    [Fact]
    public async Task Single_ParentHelp_ListsProd()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSingle, ["deploy", "--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("COMMANDS:", output);
        Assert.Contains("prod", output);
    }

    [Fact]
    public async Task Two_ParentHelp_ListsBoth_AndNoLeafOptions()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("COMMANDS:", output);
        Assert.Contains("prod", output);
        Assert.Contains("staging", output);
        Assert.DoesNotContain("--force", output);
        Assert.DoesNotContain("--fast", output);
    }

    [Fact]
    public async Task Two_DistinctInterleavedFlag_ReachesLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--force", "prod"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("deploy prod:True", output);
    }

    [Fact]
    public async Task Two_DistinctPostLeafFlag_ReachesLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "prod", "--force"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("deploy prod:True", output);
    }

    [Fact]
    public async Task Two_WrongLeafFlag_StaysUnknown()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--force", "staging"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --force", error);
        Assert.Contains("Run 'verify634 deploy prod --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Two_UnambiguousLeafFooter_NamesLeaf()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--fast", "unknownchild"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --fast", error);
        Assert.Contains("Run 'verify634 deploy staging --help' for more information on specific command options.", error);
    }

    [Fact]
    public async Task Two_AmbiguousShort_StaysUnknown()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "-f", "prod"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: -f", error);
    }

    [Fact]
    public async Task SameLong_DivergentShape_StaysUnknown()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateSameLong, ["deploy", "--config", "prod-east"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --config", error);
    }

    [Fact]
    public async Task Two_ValuedLeafOwned_Interleaved_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => ApplicationBuilder.Create()
                .SetExecutableName("verify634")
                .SetExecutableTitle("Verify 634")
                .SetExecutableDescription("Verify.")
                .SetExecutableVersion("9.9.9")
                .AddCommand<DeployProdSharedValueCommand>(),
            ["deploy", "--config", "appsettings.json", "prod-east"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("deploy prod-east:appsettings.json", output);
    }

    private static ApplicationBuilder CreateTwoSharedValue()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("verify634")
            .SetExecutableTitle("Verify 634")
            .SetExecutableDescription("Verify.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<DeployProdSharedValueCommand>()
            .AddCommand<DeployProdWestSharedValueCommand>();
    }

    [Fact]
    public async Task TwoSharedValue_ValuedInterleavedSpaceForm_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwoSharedValue, ["deploy", "--config", "v", "prod-east"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("deploy prod-east:v", output);
    }

    [Fact]
    public async Task TwoSharedValue_ValuedInterleavedEqualsForm_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwoSharedValue, ["deploy", "--config=v", "prod-east"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("deploy prod-east:v", output);
    }

    [Fact]
    public async Task TwoSharedValue_ValuedInterleaved_WrongChild_StaysUnknown()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwoSharedValue, ["deploy", "--config", "v", "unknownchild"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("Unknown option: --config", error);
    }

    [Fact]
    public async Task BareDeploy_RequiresSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy"]);
        Assert.Equal(2, exitCode);
        Assert.Contains("requires a subcommand", error);
    }

    [Fact]
    public async Task Two_LeafFlag_BeforeHelp_ReachesHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--force", "--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE", output);
    }

    [Fact]
    public async Task Two_Help_BeforeNearMiss_ReportsUnknownSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--help", "porod"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown subcommand 'porod'", error);
        Assert.Contains("Did you mean 'prod'?", error);
    }

    [Fact]
    public async Task Two_Help_BeforeFarMiss_RequiresSubcommandList()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--help", "zzzz"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("requires a subcommand", error);
        Assert.Contains("Available subcommands:", error);
        Assert.Contains("prod", error);
        Assert.Contains("staging", error);
        Assert.DoesNotContain("Did you mean", error);
    }

    [Fact]
    public async Task Two_Help_BeforeHit_ForwardsToLeafHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--help", "prod"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Deploy to production.", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task Two_ParentHelp_Usage_ListsSubcommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateTwo, ["deploy", "--help"]);
        Assert.Equal(0, exitCode);
        Assert.Contains("<COMMAND> [ARGS...]", output);
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
