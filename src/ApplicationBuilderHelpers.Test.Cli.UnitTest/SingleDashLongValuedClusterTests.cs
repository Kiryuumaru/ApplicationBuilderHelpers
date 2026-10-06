using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Single-dash-long and valued-cluster tokenizer guards.
/// Pins the #592 policy through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// an exact single-dash-long near-miss reports the full token with a suggestion,
/// a valued short with a single-char known-short remainder is rejected by the
/// cluster rule, and compact remainders plus flag clusters keep binding.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class SingleDashLongValuedClusterTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("greet", "Greets.")]
    public sealed class ClusterRuleGreetCommand : Command
    {
        [CommandOption('a', "alpha", Description = "Alpha flag.")]
        public bool Alpha { get; set; }

        [CommandOption('b', "beta", Description = "Beta flag.")]
        public bool Beta { get; set; }

        [CommandOption('c', "charlie", Description = "Charlie flag.")]
        public bool Charlie { get; set; }

        [CommandOption('d', "data", Description = "Data value.")]
        public string? Data { get; set; }

        [CommandOption('v', "verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption('n', "nickname", Description = "Nickname value.")]
        public string? Nickname { get; set; }

        [CommandOption('l', "loud", Description = "Loud flag.")]
        public bool Loud { get; set; }

        [CommandOption('k', "secret-token", Description = "Secret token.", Secret = true)]
        public string? SecretToken { get; set; }

        [CommandArgument("person", Description = "Person.", Position = 0)]
        public string? Person { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Alpha: {Alpha}");
            Console.WriteLine($"Beta: {Beta}");
            Console.WriteLine($"Charlie: {Charlie}");
            Console.WriteLine($"Data: {Data ?? "null"}");
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Nickname: {Nickname ?? "null"}");
            Console.WriteLine($"Loud: {Loud}");
            Console.WriteLine($"Person: {Person ?? "null"}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task SingleDashLongName_ReportsFullTokenWithSuggestion()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["greet", "-verbose", "Alice"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: -verbose", error);
        Assert.Contains("Did you mean '--verbose'?", error);
        Assert.DoesNotContain("Unknown option: -e", error);
    }

    [Fact]
    public async Task ValuedShortNotLast_RejectsWithClusterRule()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["greet", "-nl", "Alice"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '-n' requires a value and must be last in a combined short cluster", error);
        Assert.DoesNotContain("-nl", error);
        Assert.DoesNotContain("Unexpected argument", error);
        Assert.DoesNotContain("Alice", error);
    }

    [Fact]
    public async Task FlagThenValued_BindsNextToken()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["greet", "-ln", "Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Loud: True", output);
        Assert.Contains("Nickname: Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task LongFlag_WithPositional_Binds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["greet", "--verbose", "Alice"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: True", output);
        Assert.Contains("Person: Alice", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CompactValuedRemainder_Binds()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["greet", "-abdvalue"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: True", output);
        Assert.Contains("Beta: True", output);
        Assert.Contains("Charlie: False", output);
        Assert.Contains("Data: value", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CombinedFlags_BindAll()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["greet", "-abc"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Alpha: True", output);
        Assert.Contains("Beta: True", output);
        Assert.Contains("Charlie: True", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task UnknownClusterChar_ReportsFailingCharOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["greet", "-zx"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: -z", error);
        Assert.DoesNotContain("-zx", error);
    }

    [Fact]
    public async Task SecretRemainder_OmitsAttachedValue()
    {
        const string secret = "Sup3rS3cretZQ";
        var token = $"-axk{secret}";
        var (exitCode, output, error) = await RunCapturedAsync(["greet", token]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: -x", error);
        Assert.DoesNotContain(secret, error);
        Assert.DoesNotContain(token, error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("cluster-rule-test")
            .SetExecutableTitle("Cluster Rule Test")
            .SetExecutableDescription("Cluster rule verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ClusterRuleGreetCommand>();
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
