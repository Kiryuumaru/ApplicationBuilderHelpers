using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process root-positional tests.
/// Pins the root-with-implementation positional contract through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// a bare token binds the root position 0, a bare invocation uses the default,
/// a child name routes before the root value, an argument-less root keeps the
/// unknown-command error, and global help lists the root arguments with their
/// usage signature.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class RootPositionalArgumentTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command(description: "Root positional verification root.")]
    public sealed class HelloRootCommand : Command
    {
        [CommandArgument("name", Description = "Name to greet.", Position = 0)]
        public string? Name { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Hello {Name ?? "World"}!");
            return ValueTask.CompletedTask;
        }
    }

    [Command("greet", "Runs the greet leaf.")]
    public sealed class HelloLeafCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("greet leaf ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command(description: "Argument-less verification root.")]
    public sealed class PlainRootCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("plain root ran");
            return ValueTask.CompletedTask;
        }
    }

    [Command("deploy", "Runs the deploy leaf.")]
    public sealed class PlainDeployCommand : Command
    {
        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine("deploy leaf ran");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task BareValue_BindsRootPositional()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHelloBuilder, ["Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Hello Alice!", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task BareInvocation_UsesDefaultValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHelloBuilder, []);

        Assert.Equal(0, exitCode);
        Assert.Contains("Hello World!", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task ChildName_RoutesToLeafOverRootValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHelloBuilder, ["greet"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("greet leaf ran", output);
        Assert.DoesNotContain("Hello", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnknownToken_OnArgLessRoot_SuggestsChildCommand()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreatePlainBuilder, ["deply"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("No command found for 'deply'", error);
        Assert.Contains("Did you mean 'deploy'?", error);
    }

    [Fact]
    public async Task GlobalHelp_ListsRootArguments()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHelloBuilder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("COMMANDS:", output);
        Assert.Contains("ARGUMENTS:", output);
        Assert.Contains("Name to greet.", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task GlobalHelp_UsageIncludesRootSignature()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateHelloBuilder, ["--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("[NAME]", output);
        Assert.Contains("<COMMAND>", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateHelloBuilder()
    {
        return CreateBaseBuilder("hello-root-test", "Hello Root Test", "Root positional verification CLI.")
            .AddCommand<HelloRootCommand>()
            .AddCommand<HelloLeafCommand>();
    }

    private static ApplicationBuilder CreatePlainBuilder()
    {
        return CreateBaseBuilder("plain-root-test", "Plain Root Test", "Argument-less verification CLI.")
            .AddCommand<PlainRootCommand>()
            .AddCommand<PlainDeployCommand>();
    }

    private static ApplicationBuilder CreateBaseBuilder(string name, string title, string description)
    {
        return ApplicationBuilder.Create()
            .SetExecutableName(name)
            .SetExecutableTitle(title)
            .SetExecutableDescription(description)
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
