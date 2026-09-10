using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process precedence tests for multi-value options backed by the environment.
/// An explicit command line value wins over the environment: when command line
/// values are present the environment must not append an extra element.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because both the
/// console streams and the process environment are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class OptionPrecedenceTests
{
    private const string TagsVariable = "PARKER_ENVPRECEDENCE_TAGS";

    private static readonly SemaphoreSlim EnvGate = new(1, 1);

    [Command("envdup", "Probes array environment precedence.")]
    public sealed class EnvArrayCommand : Command
    {
        [CommandOption("tags", Description = "Tags.", EnvironmentVariable = TagsVariable)]
        public string[]? Tags { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Tags: {(Tags is null ? "null" : string.Join(",", Tags))}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task ArrayOption_CliValuesExcludeEnvironmentValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["envdup", "--tags=a", "--tags=b"],
            new Dictionary<string, string?> { [TagsVariable] = "envtag" });

        Assert.Equal(0, exitCode);
        Assert.Contains("Tags: a,b", output);
        Assert.DoesNotContain("envtag", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("envprecedence-test")
            .SetExecutableTitle("EnvPrecedence Test")
            .SetExecutableDescription("Array environment precedence verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<EnvArrayCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(
        string[] args, IReadOnlyDictionary<string, string?>? environment = null)
    {
        await EnvGate.WaitAsync();
        var priors = new Dictionary<string, string?>();
        try
        {
            if (environment is not null)
            {
                foreach (var (name, value) in environment)
                {
                    priors[name] = Environment.GetEnvironmentVariable(name);
                    Environment.SetEnvironmentVariable(name, value);
                }
            }

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
            foreach (var (name, prior) in priors)
            {
                Environment.SetEnvironmentVariable(name, prior);
            }

            EnvGate.Release();
        }
    }
}
