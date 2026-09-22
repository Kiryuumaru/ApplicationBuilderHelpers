using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process reserved-short tests for the CLI hierarchy gate.
/// The help/version gateway shorts (<c>-h</c>/<c>-V</c>) win inside combined
/// short clusters even mid-cluster, so a declared option reusing either short
/// would never bind — validation rejects the registration with a fault
/// (exit 1) instead. Only the built-in <c>--help</c> owner may hold
/// <c>-h</c>; <c>-V</c> is forbidden for all local options because no
/// built-in version node exists (version is gateway-only). Joins the
/// non-parallel <c>ConsoleDecoupling</c> collection because the console
/// streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ReservedShortNameTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("reshost", "Probes help-short reuse on a leaf command.")]
    public sealed class HelpShortProbeCommand : Command
    {
        [CommandOption('h', "host", Description = "Host address to bind to.")]
        public string Host { get; set; } = "localhost";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Host: {Host}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("resver", "Probes version-short reuse on a leaf command.")]
    public sealed class VersionShortProbeCommand : Command
    {
        [CommandOption('V', "verbose", Description = "Verbose output.")]
        public bool Verbose { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            return ValueTask.CompletedTask;
        }
    }

    [Command("reshelp", "Probes the help owner holding the help short.")]
    public sealed class HelpOwnerProbeCommand : Command
    {
        [CommandOption('h', "help", Description = "Show help information.")]
        public bool ShowHelp { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("help owner ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("resversion", "Probes version-short reuse under the version long name.")]
    public sealed class VersionNameProbeCommand : Command
    {
        [CommandOption('V', "version", Description = "Show version information.")]
        public bool ShowVersion { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("version name ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("resserve", "Probes long-only host binding next to the help gateway.")]
    public sealed class LongOnlyHostProbeCommand : Command
    {
        [CommandOption("host", Description = "Host address to bind to.")]
        public string Host { get; set; } = "localhost";

        [CommandOption('w', "watch", Description = "Enable file watching for auto-reload.")]
        public bool Watch { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Host: {Host}");
            Console.WriteLine($"Watch: {Watch}");
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Probes help-short reuse on the root command.")]
    public sealed class RootHostProbeCommand : Command
    {
        [CommandOption('h', "host", Description = "Host address to bind to.")]
        public string Host { get; set; } = "localhost";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Host: {Host}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task HostShort_ReusingHelpShort_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<HelpShortProbeCommand>(), ["reshost"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Reserved short name conflict", error);
        Assert.Contains("'-h'", error);
        Assert.Contains("--host", error);
        Assert.Contains("reserved for help", error);
    }

    [Fact]
    public async Task VerboseShort_ReusingVersionShort_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<VersionShortProbeCommand>(), ["resver"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Reserved short name conflict", error);
        Assert.Contains("'-V'", error);
        Assert.Contains("--verbose", error);
        Assert.Contains("reserved for version", error);
    }

    [Fact]
    public async Task HelpOwner_HoldingHelpShort_RunsHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<HelpOwnerProbeCommand>(), ["reshelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task VersionName_ReusingVersionShort_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<VersionNameProbeCommand>(), ["resversion"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Reserved short name conflict", error);
        Assert.Contains("'-V'", error);
        Assert.Contains("--version", error);
        Assert.Contains("reserved for version", error);
    }

    [Fact]
    public async Task LongOnlyHost_HelpShort_ShowsHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<LongOnlyHostProbeCommand>(), ["resserve", "-h"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.DoesNotContain("Host:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LongOnlyHost_LongForm_BindsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<LongOnlyHostProbeCommand>(), ["resserve", "--host", "example.com"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Host: example.com", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LongOnlyHost_CombinedCluster_RoutesToHelp()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<LongOnlyHostProbeCommand>(), ["resserve", "-hw"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("USAGE:", output);
        Assert.DoesNotContain("Host:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LongOnlyHost_VersionCluster_ShowsVersion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<LongOnlyHostProbeCommand>(), ["resserve", "-wV"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("9.9.9", output);
        Assert.DoesNotContain("Watch:", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task RootHost_ReusingHelpShort_MapsToFaultExitCode()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<RootHostProbeCommand>(), []);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Reserved short name conflict", error);
        Assert.Contains("'-h'", error);
        Assert.Contains("<root>", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("reserved-short-test")
            .SetExecutableTitle("Reserved Short Test")
            .SetExecutableDescription("Reserved short name verification CLI.")
            .SetExecutableVersion("9.9.9");
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        Func<ApplicationBuilder> builderFactory, string[] args, CancellationToken cancellationToken = default)
    {
        await ConsoleGate.WaitAsync();
        var originalOut = Console.Out;
        var originalError = Console.Error;
        using var outWriter = new StringWriter();
        using var errorWriter = new StringWriter();
        Console.SetOut(outWriter);
        Console.SetError(errorWriter);
        try
        {
            var exitCode = await builderFactory().RunAsync(args, cancellationToken);
            outWriter.Flush();
            errorWriter.Flush();
            return (exitCode, outWriter.ToString(), errorWriter.ToString());
        }
        finally
        {
            Console.SetOut(originalOut);
            Console.SetError(originalError);
            ConsoleGate.Release();
        }
    }
}
