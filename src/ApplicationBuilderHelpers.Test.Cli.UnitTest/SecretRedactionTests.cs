using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// In-process secret redaction tests for the CLI parser.
/// Exercises the <c>Secret</c> flag on options and arguments through the public
/// <see cref="ApplicationBuilder.RunAsync(string[], CancellationToken)"/> entry point:
/// help masks secret defaults while keeping the Default label and environment
/// variable name, error messages omit the provided secret value while keeping
/// the option name and valid-values list, and the global option copy preserves
/// the flag across sibling commands.
/// Joins the non-parallel <c>ConsoleDecoupling</c> collection because the
/// console streams are process-global mutable state.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class SecretRedactionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    public enum SecretKind { Fast, Slow }

    [Command("sechelp", "Probes secret and plain help defaults.")]
    public sealed class SecretHelpCommand : Command
    {
        [CommandOption("secret-token", Description = "Secret token.", EnvironmentVariable = "PARKER_SECRET_TOKEN", Secret = true)]
        public string SecretToken { get; set; } = "hunter2";

        [CommandOption("plain-token", Description = "Plain token.")]
        public string PlainToken { get; set; } = "visible";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"tokens:{SecretToken}:{PlainToken}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("secchoice", "Probes secret option allowed values.")]
    public sealed class SecretChoiceCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"], Secret = true)]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"secchoice:{Mode ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("plainchoice", "Probes plain option allowed values.")]
    public sealed class PlainChoiceCommand : Command
    {
        [CommandOption("mode", Description = "Output mode.", FromAmong = ["json", "xml"])]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"plainchoice:{Mode ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("secchoicearg", "Probes secret argument allowed values.")]
    public sealed class SecretChoiceArgumentCommand : Command
    {
        [CommandArgument("mode", Description = "Output mode.", Position = 0, FromAmong = ["json", "xml"], Secret = true)]
        public string? Mode { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"secchoicearg:{Mode ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("secnum", "Probes secret numeric option conversion.")]
    public sealed class SecretNumberCommand : Command
    {
        [CommandOption("count", Description = "Item count.", Secret = true)]
        public int Count { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"secnum:{Count}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("plainnum", "Probes plain numeric option conversion.")]
    public sealed class PlainNumberCommand : Command
    {
        [CommandOption("count", Description = "Item count.")]
        public int Count { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"plainnum:{Count}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("secnumarg", "Probes secret numeric argument conversion.")]
    public sealed class SecretNumberArgumentCommand : Command
    {
        [CommandArgument("level", Description = "Level number.", Position = 0, Secret = true)]
        public int Level { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"secnumarg:{Level}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("secenumarg", "Probes secret enum argument conversion.")]
    public sealed class SecretEnumArgumentCommand : Command
    {
        [CommandArgument("kind", Description = "Processing kind.", Position = 0, Secret = true)]
        public SecretKind Kind { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"secenumarg:{Kind}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("secflag", "Probes secret and plain flag literals.")]
    public sealed class SecretFlagCommand : Command
    {
        [CommandOption("secure", Description = "Secure flag.", Secret = true)]
        public bool Secure { get; set; }

        [CommandOption("open", Description = "Open flag.")]
        public bool Open { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"secflag:{Secure}:{Open}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("vault alpha", "First leaf with shared secret option.")]
    public sealed class VaultAlphaCommand : Command
    {
        [CommandOption("vault-token", Description = "Vault token.", FromAmong = ["red", "blue"], Secret = true)]
        public string VaultToken { get; set; } = "red";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"vault alpha:{VaultToken}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("vault beta", "Second leaf with shared secret option.")]
    public sealed class VaultBetaCommand : Command
    {
        [CommandOption("vault-token", Description = "Vault token.", FromAmong = ["red", "blue"], Secret = true)]
        public string VaultToken { get; set; } = "red";

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"vault beta:{VaultToken}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task SecretOptionHelp_MasksDefaultKeepsLabelAndEnvName()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--secret-token", output);
        Assert.Contains("Default: [REDACTED]", output);
        Assert.Contains("Environment variable: PARKER_SECRET_TOKEN", output);
        Assert.DoesNotContain("hunter2", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task PlainOptionHelp_ShowsDefaultValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--help"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("--plain-token", output);
        Assert.Contains("Default: visible", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task SecretOption_InvalidAllowedValue_OmitsValueKeepsValidList()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secchoice", "--mode", "bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value provided for option '--mode' is not valid.", error);
        Assert.Contains("Must be one of: json, xml", error);
        Assert.DoesNotContain("bogus", error);
    }

    [Fact]
    public async Task PlainOption_InvalidAllowedValue_EchoesValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["plainchoice", "--mode", "bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value 'bogus' is not valid for option '--mode'.", error);
        Assert.Contains("Must be one of: json, xml", error);
    }

    [Fact]
    public async Task SecretArgument_InvalidAllowedValue_OmitsValueKeepsValidList()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secchoicearg", "bogus"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Value provided for argument 'mode' is not valid.", error);
        Assert.Contains("Must be one of: json, xml", error);
        Assert.DoesNotContain("bogus", error);
    }

    [Fact]
    public async Task SecretOption_InvalidFormat_OmitsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secnum", "--count", "notanumber"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("[REDACTED]", error);
        Assert.DoesNotContain("notanumber", error);
    }

    [Fact]
    public async Task PlainOption_InvalidFormat_EchoesValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["plainnum", "--count", "notanumber"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("'notanumber'", error);
        Assert.DoesNotContain("[REDACTED]", error);
    }

    [Fact]
    public async Task SecretArgument_InvalidFormat_OmitsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secnumarg", "notanum"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("[REDACTED]", error);
        Assert.DoesNotContain("notanum", error);
    }

    [Fact]
    public async Task SecretEnumArgument_InvalidValue_OmitsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secenumarg", "BogusKind"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Cannot convert provided value to SecretKind for argument 'kind'", error);
        Assert.DoesNotContain("BogusKind", error);
    }

    [Fact]
    public async Task SecretFlag_InvalidLiteral_OmitsLiteral()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secflag", "--secure=maybe"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value provided for option '--secure'.", error);
        Assert.DoesNotContain("maybe", error);
    }

    [Fact]
    public async Task PlainFlag_InvalidLiteral_EchoesLiteral()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secflag", "--open=maybe"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Invalid Boolean value 'maybe' for option '--open'.", error);
    }

    [Fact]
    public async Task SecretFlag_NegatedWithValue_OmitsRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secflag", "--no-secure=oops"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-secure' does not accept a value.", error);
        Assert.DoesNotContain("oops", error);
    }

    [Fact]
    public async Task SecretStringOption_NegatedWithValue_OmitsRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--no-secret-token=s3cr3t-no-value"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-secret-token' does not accept a value.", error);
        Assert.DoesNotContain("s3cr3t-no-value", error);
    }

    [Fact]
    public async Task PlainStringOption_NegatedWithValue_EchoesRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--no-plain-token=plainoops"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-plain-token' does not accept a value 'plainoops'.", error);
    }

    [Fact]
    public async Task UnknownNegatedOption_NegatedWithValue_EchoesRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--no-suchopt=mystery"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-suchopt' does not accept a value 'mystery'.", error);
    }

    [Fact]
    public async Task SharedSecretOption_PromotedToGlobalHelpMaskedAndRedactedOnLeaves()
    {
        var (helpCode, helpOutput, helpError) = await RunCapturedAsync(CreateVaultBuilder, ["--help"]);

        Assert.Equal(0, helpCode);
        Assert.Contains("GLOBAL OPTIONS:", helpOutput);
        Assert.Contains("--vault-token", helpOutput);
        Assert.True(string.IsNullOrWhiteSpace(helpError), $"Expected empty stderr but got: {helpError}");

        var (leafCode, leafOutput, leafError) = await RunCapturedAsync(CreateVaultBuilder, ["vault", "alpha", "--help"]);
        Assert.Equal(0, leafCode);
        Assert.Contains("--vault-token", leafOutput);
        Assert.Contains("Default: [REDACTED]", leafOutput);
        Assert.DoesNotContain("Default: red", leafOutput);
        Assert.True(string.IsNullOrWhiteSpace(leafError), $"Expected empty stderr but got: {leafError}");

        var alpha = await RunCapturedAsync(CreateVaultBuilder, ["vault", "alpha", "--vault-token=bogus"]);
        Assert.Equal(2, alpha.ExitCode);
        Assert.Contains("Value provided for option '--vault-token' is not valid.", alpha.Error);
        Assert.Contains("Must be one of: red, blue", alpha.Error);
        Assert.DoesNotContain("bogus", alpha.Error);

        var beta = await RunCapturedAsync(CreateVaultBuilder, ["vault", "beta", "--vault-token=bogus"]);
        Assert.Equal(2, beta.ExitCode);
        Assert.Contains("Value provided for option '--vault-token' is not valid.", beta.Error);
        Assert.Contains("Must be one of: red, blue", beta.Error);
        Assert.DoesNotContain("bogus", beta.Error);
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("secret-test")
            .SetExecutableTitle("Secret Test")
            .SetExecutableDescription("Secret redaction verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SecretHelpCommand>()
            .AddCommand<SecretChoiceCommand>()
            .AddCommand<PlainChoiceCommand>()
            .AddCommand<SecretChoiceArgumentCommand>()
            .AddCommand<SecretNumberCommand>()
            .AddCommand<PlainNumberCommand>()
            .AddCommand<SecretNumberArgumentCommand>()
            .AddCommand<SecretEnumArgumentCommand>()
            .AddCommand<SecretFlagCommand>();
    }

    private static ApplicationBuilder CreateVaultBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("secret-test")
            .SetExecutableTitle("Secret Test")
            .SetExecutableDescription("Secret redaction verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<VaultAlphaCommand>()
            .AddCommand<VaultBetaCommand>();
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
