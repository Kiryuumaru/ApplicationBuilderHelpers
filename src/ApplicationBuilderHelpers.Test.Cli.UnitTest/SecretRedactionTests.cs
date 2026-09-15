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

    public enum CollectionShade
    {
        Red,
        Green,
        Blue
    }

    [Command("seccoll", "Probes secret integer array materialization.")]
    public sealed class SecretCollectionCommand : Command
    {
        [CommandOption("scores", Description = "Scores.", Secret = true)]
        public int[]? Scores { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"seccoll:{(Scores is null ? "null" : string.Join(",", Scores))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("plaincoll", "Probes plain integer array materialization.")]
    public sealed class PlainCollectionCommand : Command
    {
        [CommandOption("scores", Description = "Scores.")]
        public int[]? Scores { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"plaincoll:{(Scores is null ? "null" : string.Join(",", Scores))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("seclist", "Probes secret integer list materialization.")]
    public sealed class SecretListCommand : Command
    {
        [CommandOption("scores", Description = "Scores.", Secret = true)]
        public List<int>? Scores { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"seclist:{(Scores is null ? "null" : string.Join(",", Scores))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("plainlist", "Probes plain integer list materialization.")]
    public sealed class PlainListCommand : Command
    {
        [CommandOption("scores", Description = "Scores.")]
        public List<int>? Scores { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"plainlist:{(Scores is null ? "null" : string.Join(",", Scores))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("secmissing", "Probes secret enum array without a registered parser.")]
    public sealed class SecretMissingParserCommand : Command
    {
        [CommandOption("shades", Description = "Shades.", Secret = true)]
        public CollectionShade[]? Shades { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"secmissing:{(Shades is null ? "null" : string.Join(",", Shades))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("plainmissing", "Probes plain enum array without a registered parser.")]
    public sealed class PlainMissingParserCommand : Command
    {
        [CommandOption("shades", Description = "Shades.")]
        public CollectionShade[]? Shades { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"plainmissing:{(Shades is null ? "null" : string.Join(",", Shades))}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("novalue", "Probes --no-<name>=value dispatch for near-miss bases.")]
    public sealed class NoValueProbeCommand : Command
    {
        [CommandOption("note", Description = "Note text.")]
        public string? Note { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"novalue:{Note ?? "null"}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    [Command("clust", "Probes short-cluster unknown handling without value echo.")]
    public sealed class ClusterProbeCommand : Command
    {
        [CommandOption('a', "alpha", Description = "Alpha flag.")]
        public bool Alpha { get; set; }

        [CommandOption('b', "beta", Description = "Beta flag.")]
        public bool Beta { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationTokenSource cancellationTokenSource)
        {
            Console.WriteLine($"clust:{Alpha}:{Beta}");
            cancellationTokenSource.Cancel();
            return ValueTask.CompletedTask;
        }
    }

    /// <summary>
    /// Custom <see cref="int"/> parser whose array factory echoes the last
    /// parsed raw value in its failure, so the collection materialization
    /// path must mask it for secret options. Registered through the public
    /// <c>AddCommandTypeParser</c> entry point. Scalar parsing delegates to
    /// <see cref="int.TryParse"/>.
    /// </summary>
    public sealed class EchoingIntArrayParser : ICommandTypeParser
    {
        public static string? LastRaw;

        public Type Type => typeof(int);

        public object? Parse(string? value, out string? validateError)
        {
            LastRaw = value;
            if (int.TryParse(value, out var result))
            {
                validateError = null;
                return result;
            }

            validateError = $"Invalid Int32 value: '{value}'. Expected a valid Int32.";
            return null;
        }

        public string? GetString(object? value) => value?.ToString();

        public object? GetDefaultValue() => default(int);

        public Array CreateTypedArray(int length) => throw new InvalidOperationException($"Factory failure for '{LastRaw}'.");

        public System.Collections.IList CreateTypedList(int capacity) => throw new InvalidOperationException($"Factory failure for '{LastRaw}'.");
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
    public async Task SecretValuedString_NegatedWithValue_OmitsRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--no-secret-token=s3cr3t-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-secret-token' does not accept a value. Use bare '--no-secret-token' to set the flag to 'false'.", error);
        Assert.DoesNotContain("s3cr3t-leak", error);
    }

    [Fact]
    public async Task PlainValuedString_NegatedWithValue_EchoesRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--no-plain-token=shown"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-plain-token' does not accept a value 'shown'. Use bare '--no-plain-token' to set the flag to 'false'.", error);
        Assert.DoesNotContain("[REDACTED]", error);
    }

    [Fact]
    public async Task SecretValuedInt_NegatedWithValue_OmitsRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secnum", "--no-count=424242"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-count' does not accept a value. Use bare '--no-count' to set the flag to 'false'.", error);
        Assert.DoesNotContain("424242", error);
    }

    [Fact]
    public async Task SecretCollection_NegatedWithValue_OmitsRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateCollectionBuilder, ["seccoll", "--no-scores=777"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-scores' does not accept a value. Use bare '--no-scores' to set the flag to 'false'.", error);
        Assert.DoesNotContain("777", error);
    }

    [Fact]
    public async Task PlainCollection_NegatedWithValue_EchoesRejectedValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateCollectionBuilder, ["plaincoll", "--no-scores=777"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-scores' does not accept a value '777'. Use bare '--no-scores' to set the flag to 'false'.", error);
        Assert.DoesNotContain("[REDACTED]", error);
    }

    [Fact]
    public async Task NegatedWithValue_FirstSeparator_SplitsNameAtFirstEquals()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secflag", "--no-secure=leak=extra"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-secure' does not accept a value.", error);
        Assert.DoesNotContain("leak=extra", error);
    }

    [Fact]
    public async Task NegatedWithValue_EmptyBase_FailsClosedRedacted()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--no-=hunter2"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Option '--no-' does not accept a value.", error);
        Assert.DoesNotContain("hunter2", error);
    }

    [Fact]
    public async Task NegatedWithValue_UnknownBase_ReportsUnknownWithoutValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--no-zzzzqqqq=hunter2"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --no-zzzzqqqq", error);
        Assert.DoesNotContain("hunter2", error);
        Assert.DoesNotContain("does not accept", error);
        Assert.DoesNotContain("Did you mean", error);
    }

    [Fact]
    public async Task NegatedWithValue_UnknownNearMiss_SuggestsNameOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["novalue", "--no-not=shown"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --no-not", error);
        Assert.Contains("Did you mean '--note'?", error);
        Assert.DoesNotContain("shown", error);
    }

    [Fact]
    public async Task NegatedWithValue_CaseVariantBase_ReportsUnknownWithoutValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["secflag", "--no-SECURE=oops"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --no-SECURE", error);
        Assert.DoesNotContain("oops", error);
        Assert.DoesNotContain("does not accept", error);
    }

    [Fact]
    public async Task UnknownTypo_WithSecretValue_OmitsValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--pasword=hunter2"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --pasword", error);
        Assert.DoesNotContain("hunter2", error);
    }

    [Fact]
    public async Task UnknownOption_WithSecretValue_NamesOptionOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--unknown=SuperSecret"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --unknown", error);
        Assert.DoesNotContain("SuperSecret", error);
    }

    [Fact]
    public async Task UnknownOption_EmptyName_FailsClosedWithoutValue()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--=hunter2x9q"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --", error);
        Assert.DoesNotContain("hunter2x9q", error);
    }

    [Fact]
    public async Task UnknownClusterChar_NamesWholeToken()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["clust", "-abx"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: -abx", error);
    }

    [Fact]
    public async Task UnknownNearMiss_WithSecretValue_SuggestsNameOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--secret-toke=s3cr3t-leak"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --secret-toke", error);
        Assert.Contains("Did you mean '--secret-token'?", error);
        Assert.DoesNotContain("s3cr3t-leak", error);
    }

    [Fact]
    public async Task UnknownNonSecret_WithValue_NamesOptionOnly()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--plain-toke=shown"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option: --plain-toke", error);
        Assert.Contains("Did you mean '--plain-token'?", error);
        Assert.DoesNotContain("shown", error);
    }

    [Fact]
    public async Task UnknownOption_WithValue_ExitCodePreserved()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateBuilder, ["sechelp", "--unknown=SuperSecret"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Unknown option", error);
        Assert.DoesNotContain("SuperSecret", error);
    }

    [Fact]
    public void ParserError_CaseVariantValue_Redacts()
    {
        var exact = CommandLineParser.SecretRedaction.RedactParserError(
            "Invalid Int32 value: 'abc'. Expected a valid Int32.", "abc", true);
        Assert.Contains("[REDACTED]", exact);
        Assert.DoesNotContain("'abc'", exact);

        var folded = CommandLineParser.SecretRedaction.RedactParserError(
            "Invalid Int32 value: 'ABC'. Expected a valid Int32.", "abc", true);
        Assert.Contains("[REDACTED]", folded);
        Assert.DoesNotContain("'ABC'", folded);

        var passthrough = CommandLineParser.SecretRedaction.RedactParserError(
            "Invalid Int32 value: 'abc'. Expected a valid Int32.", "abc", false);
        Assert.Contains("'abc'", passthrough);
        Assert.DoesNotContain("[REDACTED]", passthrough);
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

    [Fact]
    public async Task SecretCollection_ArrayFactoryFailure_MasksValueKeepsNames()
    {
        EchoingIntArrayParser.LastRaw = null;
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateCollectionBuilder().AddCommandTypeParser<EchoingIntArrayParser>(),
            ["seccoll", "--scores=7"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Cannot bind", error);
        Assert.Contains("--scores", error);
        Assert.Contains(typeof(int[]).FullName!, error);
        Assert.Contains(typeof(int).FullName!, error);
        Assert.Contains("[REDACTED]", error);
        Assert.DoesNotContain("Factory failure for", error);
        if (EchoingIntArrayParser.LastRaw is not null)
        {
            Assert.DoesNotContain($"'{EchoingIntArrayParser.LastRaw}'", error);
        }
    }

    [Fact]
    public async Task PlainCollection_ArrayFactoryFailure_EchoesValue()
    {
        EchoingIntArrayParser.LastRaw = null;
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateCollectionBuilder().AddCommandTypeParser<EchoingIntArrayParser>(),
            ["plaincoll", "--scores=7"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Cannot bind", error);
        Assert.Contains("--scores", error);
        Assert.Contains(typeof(int[]).FullName!, error);
        Assert.Contains(typeof(int).FullName!, error);
        Assert.Contains("Factory failure for '7'", error);
        Assert.DoesNotContain("[REDACTED]", error);
    }

    [Fact]
    public async Task SecretCollection_ListFactoryFailure_MasksValueKeepsNames()
    {
        EchoingIntArrayParser.LastRaw = null;
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateCollectionBuilder().AddCommandTypeParser<EchoingIntArrayParser>(),
            ["seclist", "--scores=7"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Cannot bind", error);
        Assert.Contains("--scores", error);
        Assert.Contains(typeof(List<int>).FullName!, error);
        Assert.Contains(typeof(int).FullName!, error);
        Assert.Contains("[REDACTED]", error);
        Assert.DoesNotContain("Factory failure for", error);
        if (EchoingIntArrayParser.LastRaw is not null)
        {
            Assert.DoesNotContain($"'{EchoingIntArrayParser.LastRaw}'", error);
        }
    }

    [Fact]
    public async Task PlainCollection_ListFactoryFailure_EchoesValue()
    {
        EchoingIntArrayParser.LastRaw = null;
        var (exitCode, output, error) = await RunCapturedAsync(
            () => CreateCollectionBuilder().AddCommandTypeParser<EchoingIntArrayParser>(),
            ["plainlist", "--scores=7"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Cannot bind", error);
        Assert.Contains("--scores", error);
        Assert.Contains(typeof(List<int>).FullName!, error);
        Assert.Contains(typeof(int).FullName!, error);
        Assert.Contains("Factory failure for '7'", error);
        Assert.DoesNotContain("[REDACTED]", error);
    }

    [Fact]
    public async Task SecretCollection_MissingParser_KeepsNames()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateCollectionBuilder, ["secmissing", "--shades=Red"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Cannot bind", error);
        Assert.Contains("--shades", error);
        Assert.Contains(typeof(CollectionShade).FullName!, error);
        Assert.Contains("No type parser is registered", error);
        Assert.Contains("AddCommandTypeParser", error);
    }

    [Fact]
    public async Task PlainCollection_MissingParser_KeepsNames()
    {
        var (exitCode, output, error) = await RunCapturedAsync(CreateCollectionBuilder, ["plainmissing", "--shades=Red"]);

        Assert.Equal(2, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Cannot bind", error);
        Assert.Contains("--shades", error);
        Assert.Contains(typeof(CollectionShade).FullName!, error);
        Assert.Contains("No type parser is registered", error);
        Assert.Contains("AddCommandTypeParser", error);
        Assert.DoesNotContain("[REDACTED]", error);
    }

    private static ApplicationBuilder CreateCollectionBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("secret-test")
            .SetExecutableTitle("Secret Test")
            .SetExecutableDescription("Secret redaction verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<SecretCollectionCommand>()
            .AddCommand<PlainCollectionCommand>()
            .AddCommand<SecretListCommand>()
            .AddCommand<PlainListCommand>()
            .AddCommand<SecretMissingParserCommand>()
            .AddCommand<PlainMissingParserCommand>();
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
            .AddCommand<SecretFlagCommand>()
            .AddCommand<NoValueProbeCommand>()
            .AddCommand<ClusterProbeCommand>();
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
