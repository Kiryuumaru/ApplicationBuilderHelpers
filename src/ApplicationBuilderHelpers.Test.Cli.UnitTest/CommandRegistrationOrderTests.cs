using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process registration-order tests for the CLI command hierarchy builder.
/// Exercises the public <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/>
/// entry point: registering a parent command after its nested sub-commands keeps the
/// existing sub-command subtree reachable (re-parented onto the incoming node) in every
/// registration order; same-name registrations keep last-wins at the node while a
/// populated subtree survives under the incoming implementation; and unrelated sibling
/// sub-commands stay unaffected.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class CommandRegistrationOrderTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("deploy", "Starts a deployment.")]
    public sealed class DeployCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("deploy ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy prod", "Deploys to production.")]
    public sealed class DeployProdCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("deploy prod ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy prod rollback", "Rolls production back.")]
    public sealed class DeployProdRollbackCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("deploy prod rollback ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("monitor status", "Shows monitor status.")]
    public sealed class MonitorStatusCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("monitor status ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy", "Initial deploy implementation.")]
    public sealed class DeployInitialCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("deploy initial ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy", "Replacement deploy implementation.")]
    public sealed class DeployReplacementCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("deploy replacement ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy prod", "Replacement deploy prod implementation.")]
    public sealed class DeployProdReplacementCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("deploy prod replacement ran");
            return ValueTask.CompletedTask;
        }
    }

    public static TheoryData<string> RegistrationOrders => new()
    {
        "deploy>deploy prod>deploy prod rollback",
        "deploy>deploy prod rollback>deploy prod",
        "deploy prod>deploy>deploy prod rollback",
        "deploy prod>deploy prod rollback>deploy",
        "deploy prod rollback>deploy>deploy prod",
        "deploy prod rollback>deploy prod>deploy",
    };

    public static TheoryData<string> UnrelatedRegistrationOrders => new()
    {
        "deploy prod>monitor status",
        "monitor status>deploy prod",
    };

    [Theory]
    [MemberData(nameof(RegistrationOrders))]
    public async Task NestedCommands_EveryRegistrationOrder_KeepsAllRoutes(string registrationOrder)
    {
        var parent = await RunCapturedAsync(() => CreateOrderedBuilder(registrationOrder), ["deploy"]);
        var child = await RunCapturedAsync(() => CreateOrderedBuilder(registrationOrder), ["deploy", "prod"]);
        var grandChild = await RunCapturedAsync(() => CreateOrderedBuilder(registrationOrder), ["deploy", "prod", "rollback"]);

        Assert.Equal(0, parent.ExitCode);
        Assert.Contains("deploy ran", parent.Output);
        Assert.True(string.IsNullOrWhiteSpace(parent.Error), $"Expected empty stderr but got: {parent.Error}");
        Assert.Equal(0, child.ExitCode);
        Assert.Contains("deploy prod ran", child.Output);
        Assert.True(string.IsNullOrWhiteSpace(child.Error), $"Expected empty stderr but got: {child.Error}");
        Assert.Equal(0, grandChild.ExitCode);
        Assert.Contains("deploy prod rollback ran", grandChild.Output);
        Assert.True(string.IsNullOrWhiteSpace(grandChild.Error), $"Expected empty stderr but got: {grandChild.Error}");
    }

    [Theory]
    [MemberData(nameof(RegistrationOrders))]
    public async Task ParentHelp_EveryRegistrationOrder_ListsNestedCommands(string registrationOrder)
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateOrderedBuilder(registrationOrder), ["deploy", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("COMMANDS:", output);
        Assert.Contains("prod", output);
        Assert.Contains("<COMMAND> [ARGS...]", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ChildRegisteredBeforeParent_BothRoutesStillRun()
    {
        Func<ApplicationBuilder> childFirst = () => CreateBuilder()
            .AddCommand<DeployProdCommand>()
            .AddCommand<DeployCommand>();

        var parent = await RunCapturedAsync(childFirst, ["deploy"]);
        var child = await RunCapturedAsync(childFirst, ["deploy", "prod"]);
        var help = await RunCapturedAsync(childFirst, ["deploy", "--help"]);

        Assert.Equal(0, parent.ExitCode);
        Assert.Contains("deploy ran", parent.Output);
        Assert.Equal(0, child.ExitCode);
        Assert.Contains("deploy prod ran", child.Output);
        Assert.Equal(0, help.ExitCode);
        Assert.Contains("COMMANDS:", help.Output);
        Assert.Contains("prod", help.Output);
        Assert.True(string.IsNullOrWhiteSpace(help.Error), $"Expected empty stderr but got: {help.Error}");
    }

    [Fact]
    public async Task SameNameChildlessRegistrations_LastRegistrationWins()
    {
        var replacementLast = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DeployInitialCommand>().AddCommand<DeployReplacementCommand>(),
            ["deploy"]);
        var initialLast = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DeployReplacementCommand>().AddCommand<DeployInitialCommand>(),
            ["deploy"]);

        Assert.Equal(0, replacementLast.ExitCode);
        Assert.Contains("deploy replacement ran", replacementLast.Output);
        Assert.DoesNotContain("deploy initial ran", replacementLast.Output);
        Assert.Equal(0, initialLast.ExitCode);
        Assert.Contains("deploy initial ran", initialLast.Output);
        Assert.DoesNotContain("deploy replacement ran", initialLast.Output);
    }

    [Fact]
    public async Task ImplementationHub_SameNameCollision_LastImplementationWinsAndChildSurvives()
    {
        Func<ApplicationBuilder> builderFactory = () => CreateBuilder()
            .AddCommand<DeployCommand>()
            .AddCommand<DeployProdCommand>()
            .AddCommand<DeployReplacementCommand>();

        var hub = await RunCapturedAsync(builderFactory, ["deploy"]);
        var child = await RunCapturedAsync(builderFactory, ["deploy", "prod"]);

        Assert.Equal(0, hub.ExitCode);
        Assert.Contains("deploy replacement ran", hub.Output);
        Assert.DoesNotContain("deploy ran", hub.Output);
        Assert.True(string.IsNullOrWhiteSpace(hub.Error), $"Expected empty stderr but got: {hub.Error}");
        Assert.Equal(0, child.ExitCode);
        Assert.Contains("deploy prod ran", child.Output);
        Assert.True(string.IsNullOrWhiteSpace(child.Error), $"Expected empty stderr but got: {child.Error}");
    }

    [Fact]
    public async Task NestedImplementationHub_SameNameCollision_LastImplementationWinsAndGrandChildSurvives()
    {
        Func<ApplicationBuilder> builderFactory = () => CreateBuilder()
            .AddCommand<DeployProdCommand>()
            .AddCommand<DeployProdRollbackCommand>()
            .AddCommand<DeployProdReplacementCommand>();

        var hub = await RunCapturedAsync(builderFactory, ["deploy", "prod"]);
        var grandChild = await RunCapturedAsync(builderFactory, ["deploy", "prod", "rollback"]);

        Assert.Equal(0, hub.ExitCode);
        Assert.Contains("deploy prod replacement ran", hub.Output);
        Assert.DoesNotContain("deploy prod ran", hub.Output);
        Assert.True(string.IsNullOrWhiteSpace(hub.Error), $"Expected empty stderr but got: {hub.Error}");
        Assert.Equal(0, grandChild.ExitCode);
        Assert.Contains("deploy prod rollback ran", grandChild.Output);
        Assert.True(string.IsNullOrWhiteSpace(grandChild.Error), $"Expected empty stderr but got: {grandChild.Error}");
    }

    [Theory]
    [MemberData(nameof(UnrelatedRegistrationOrders))]
    public async Task UnrelatedSiblings_BothRegistrationOrders_KeepBothRoutes(string registrationOrder)
    {
        var deploy = await RunCapturedAsync(() => CreateOrderedBuilder(registrationOrder), ["deploy", "prod"]);
        var monitor = await RunCapturedAsync(() => CreateOrderedBuilder(registrationOrder), ["monitor", "status"]);

        Assert.Equal(0, deploy.ExitCode);
        Assert.Contains("deploy prod ran", deploy.Output);
        Assert.True(string.IsNullOrWhiteSpace(deploy.Error), $"Expected empty stderr but got: {deploy.Error}");
        Assert.Equal(0, monitor.ExitCode);
        Assert.Contains("monitor status ran", monitor.Output);
        Assert.True(string.IsNullOrWhiteSpace(monitor.Error), $"Expected empty stderr but got: {monitor.Error}");
    }

    [Fact]
    public async Task SyntheticParent_BareInvocation_RequiresSubcommandListingChild()
    {
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateBuilder().AddCommand<DeployProdCommand>(),
            ["deploy"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'deploy' requires a subcommand", error);
        Assert.Contains("Available subcommands: prod", error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("registration-order-test")
            .SetExecutableTitle("Registration Order Test")
            .SetExecutableDescription("Registration order verification CLI.")
            .SetExecutableVersion("9.9.9");
    }

    private static ApplicationBuilder CreateOrderedBuilder(string registrationOrder)
    {
        var builder = CreateBuilder();
        foreach (var registration in registrationOrder.Split('>'))
        {
            builder = registration switch
            {
                "deploy" => builder.AddCommand<DeployCommand>(),
                "deploy prod" => builder.AddCommand<DeployProdCommand>(),
                "deploy prod rollback" => builder.AddCommand<DeployProdRollbackCommand>(),
                "monitor status" => builder.AddCommand<MonitorStatusCommand>(),
                _ => throw new InvalidOperationException($"Unknown registration marker '{registration}'.")
            };
        }

        return builder;
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
