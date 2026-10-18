using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Consumer-shaped guard for a consumed help token on a required typed option:
/// a required <c>DateOnly</c> option takes a following <c>--help</c> token as
/// its value, so the consumed token does not show help — conversion of the
/// consumed token fails and the command exits 2. Runs in the non-parallel
/// <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class RequiredDateOptionHelpConsumptionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("store prune", "Prunes the store before a required date.")]
    public sealed class StorePruneProbeCommand : Command
    {
        [CommandOption('b', "before", Description = "Prune before date (required).", Required = true)]
        public DateOnly Before { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"store-prune-run before={Before:yyyy-MM-dd}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task RequiredDateOption_ConsumesHelpToken_ReportsConversionError()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["store", "prune", "--before", "--help"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid value '--help' for option '--before'", error);
        Assert.Contains("Invalid DateOnly value: '--help'", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("consumer-shape-test")
            .SetExecutableTitle("Consumer Shape Test")
            .SetExecutableDescription("Consumer-shaped verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<StorePruneProbeCommand>();
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
