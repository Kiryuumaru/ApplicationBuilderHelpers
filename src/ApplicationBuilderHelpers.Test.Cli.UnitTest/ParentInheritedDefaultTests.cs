using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Parent help must show the <c>Default:</c> line for an inherited own option
/// with an explicit initializer, matching leaf help. When derived leaves
/// disagree on the initializer, the parent omits <c>Default:</c> rather than
/// picking one. Required options never show <c>Default:</c>.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ParentInheritedDefaultTests
{
    private static readonly SemaphoreSlim StateGate = new(1, 1);

    [Command("fmtnest", "Format hub.")]
    public abstract class FormatHubBase : Command
    {
        [CommandOption("format", Description = "Output format.")]
        public string Format { get; set; } = "table";
    }

    [Command("fmtnest alpha", "First leaf inheriting the format option.")]
    public sealed class FormatNestAlphaCommand : FormatHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"fmtnest alpha:{Format}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("fmtnest beta", "Second leaf inheriting the format option.")]
    public sealed class FormatNestBetaCommand : FormatHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"fmtnest beta:{Format}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("divnest", "Divergent hub.")]
    public abstract class DivergentHubBase : Command
    {
        [CommandOption("shared", Description = "Shared value.")]
        public string Shared { get; set; } = "base-default";
    }

    [Command("divnest alpha", "First leaf keeping the shared base default.")]
    public sealed class DivergentNestAlphaCommand : DivergentHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"divnest alpha:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("divnest beta", "Second leaf overriding the shared base default.")]
    public sealed class DivergentNestBetaCommand : DivergentHubBase
    {
        public DivergentNestBetaCommand()
        {
            Shared = "divergent-default";
        }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"divnest beta:{Shared}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("reqnest", "Required hub.")]
    public abstract class RequiredHubBase : Command
    {
        [CommandOption("count", Description = "Item count.", Required = true)]
        public int Count { get; set; }
    }

    [Command("reqnest alpha", "First leaf with required base option.")]
    public sealed class RequiredNestAlphaCommand : RequiredHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"reqnest alpha:{Count}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("reqnest beta", "Second leaf with required base option.")]
    public sealed class RequiredNestBetaCommand : RequiredHubBase
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"reqnest beta:{Count}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ParentHelp_ShowsInheritedInitializerDefault()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<FormatNestAlphaCommand>().AddCommand<FormatNestBetaCommand>(),
            ["fmtnest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--format", output);
        Assert.Contains("Default: table", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ParentHelp_OmitsDefaultWhenLeavesDisagree()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DivergentNestAlphaCommand>().AddCommand<DivergentNestBetaCommand>(),
            ["divnest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--shared", output);
        Assert.DoesNotContain("Default:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ParentHelp_OmitsDefaultForRequiredOption()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<RequiredNestAlphaCommand>().AddCommand<RequiredNestBetaCommand>(),
            ["reqnest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--count", output);
        Assert.Contains("(required)", output);
        Assert.DoesNotContain("Default:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task InstanceRegistration_ParentHelpShowsRegistrationDefaultAfterMutation()
    {
        var builder = CreateBuilder();
        builder.AddCommand(new FormatNestAlphaCommand());
        builder.AddCommand(new FormatNestBetaCommand());

        var baseline = await RunCapturedAsync(() => builder, ["fmtnest", "--help"]);

        Assert.Equal(0, baseline.ExitCode);
        Assert.Contains("Default: table", baseline.Output);

        var mutate = await RunCapturedAsync(() => builder, ["fmtnest", "alpha", "--format=json"]);

        Assert.Equal(0, mutate.ExitCode);
        Assert.Contains("fmtnest alpha:json", mutate.Output);

        var (exitCode, output, error) = await RunCapturedAsync(() => builder, ["fmtnest", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--format", output);
        Assert.Contains("Default: table", output);
        Assert.DoesNotContain("Default: json", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("parentdefault-test")
            .SetExecutableTitle("ParentDefault Test")
            .SetExecutableDescription("Parent inherited default verification CLI.")
            .SetExecutableVersion("9.9.9");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args)
    {
        await StateGate.WaitAsync();
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
                var exitCode = await builderFactory().RunAsync(args);
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
            StateGate.Release();
        }
    }
}
