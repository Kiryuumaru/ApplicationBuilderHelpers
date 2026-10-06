using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Hosting;

namespace ApplicationBuilderHelpers.Test.Cli.UnitTest;

/// <summary>
/// Response-file expansion tests for the pre-parse splice in
/// CommandLineParser.cs:55.
/// Pins uniform splice success, quote/newline grammar reuse, <c>@@</c> escape,
/// lone-<c>@</c> fault, post-<c>--</c> pass-through, nesting/depth/cycle limits,
/// missing/cap faults (exit 1), CWD-relative versus absolute resolution, the
/// completion cursor-on-<c>@</c> skip, completion probe-argument expansion, and
/// help ordering relative to expansion.
/// Runs in the non-parallel <c>ConsoleDecoupling</c> collection.
/// </summary>
[Collection("ConsoleDecoupling")]
public sealed class ResponseFileExpansionTests
{
    private static readonly SemaphoreSlim ConsoleGate = new(1, 1);

    [Command("expand", "Expansion probe command.")]
    public sealed class ExpansionProbeCommand : Command
    {
        [CommandOption("verbose", Description = "Verbose flag.")]
        public bool Verbose { get; set; }

        [CommandOption("data", Description = "Data value.")]
        public string? Data { get; set; }

        [CommandArgument("items", Description = "Items.", Position = 0)]
        public string[]? Items { get; set; }

        protected override ValueTask Run(ApplicationHost<HostApplicationBuilder> applicationHost, CancellationToken cancellationToken)
        {
            Console.WriteLine($"Verbose: {Verbose}");
            Console.WriteLine($"Data: {Data ?? "null"}");
            Console.WriteLine($"Items: {(Items is null ? "null" : string.Join(",", Items))}");
            return ValueTask.CompletedTask;
        }
    }

    [Fact]
    public async Task SpliceBasicOptions_Succeeds()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "expand --verbose --data hello");

            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: True", output);
            Assert.Contains("Data: hello", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task SpliceOptionsOnlyFile_Succeeds()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "--verbose --data hello");

            var (exitCode, output, error) = await RunCapturedAsync(["expand", "@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: True", output);
            Assert.Contains("Data: hello", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task SpliceQuotedValue_PreservesSpaces()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "expand --data \"hello world\"");

            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Data: hello world", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task SpliceSingleQuotedValue_PreservesSpaces()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "expand --data 'hello world'");

            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Data: hello world", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task SpliceNewline_IsWhitespace()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "expand\n--verbose\n--data\nhello");

            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: True", output);
            Assert.Contains("Data: hello", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task InteriorAt_IsLiteral()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "expand user@example.com");

            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Items: user@example.com", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task DoubleAtTopLevel_StripsOneAt()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["expand", "@@--verbose"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.Contains("Items: @--verbose", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task DoubleAtInFile_StripsOneAt()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "expand @@--verbose");

            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: False", output);
            Assert.Contains("Items: @--verbose", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task LoneAt_ErrorsExitOne()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["expand", "@"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error:", error);
        Assert.Contains("names no file", error);
    }

    [Fact]
    public async Task AfterSeparator_ResponseReferenceIsLiteral()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "--verbose");
            var reference = "@" + file;

            var (exitCode, output, error) = await RunCapturedAsync(["expand", "--", reference]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: False", output);
            Assert.Contains($"Items: {reference}", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task AfterSeparator_DoubleAtIsLiteral()
    {
        var (exitCode, output, error) = await RunCapturedAsync(["expand", "--", "@@lit"]);

        Assert.Equal(0, exitCode);
        Assert.Contains("Verbose: False", output);
        Assert.Contains("Items: @@lit", output);
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task NestedReference_Splices()
    {
        var dir = CreateTempDir();
        try
        {
            var inner = WriteFile(dir, "inner.rsp", "--verbose --data nested");
            var outer = WriteFile(dir, "outer.rsp", "expand @" + inner);

            var (exitCode, output, error) = await RunCapturedAsync(["@" + outer]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: True", output);
            Assert.Contains("Data: nested", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task DepthBoundary_AllowsEightLevels()
    {
        var dir = CreateTempDir();
        try
        {
            var entry = BuildChain(dir, 8, "--verbose");

            var (exitCode, output, error) = await RunCapturedAsync(["expand", "@" + entry]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: True", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task DepthExceeded_ErrorsExitOne()
    {
        var dir = CreateTempDir();
        try
        {
            var entry = BuildChain(dir, 9, "--verbose");

            var (exitCode, output, error) = await RunCapturedAsync(["expand", "@" + entry]);

            Assert.Equal(1, exitCode);
            Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
            Assert.Contains("Error:", error);
            Assert.Contains("nesting depth", error);
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task SelfReference_ErrorsCycleExitOne()
    {
        var dir = CreateTempDir();
        try
        {
            var path = Path.Combine(dir, "self.rsp");
            File.WriteAllText(path, "@" + path);

            var (exitCode, output, error) = await RunCapturedAsync(["expand", "@" + path]);

            Assert.Equal(1, exitCode);
            Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
            Assert.Contains("Error:", error);
            Assert.Contains("cycle", error);
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task TwoFileCycle_ErrorsCycleExitOne()
    {
        var dir = CreateTempDir();
        try
        {
            var left = Path.Combine(dir, "left.rsp");
            var right = Path.Combine(dir, "right.rsp");
            File.WriteAllText(left, "@" + right);
            File.WriteAllText(right, "@" + left);

            var (exitCode, output, error) = await RunCapturedAsync(["expand", "@" + left]);

            Assert.Equal(1, exitCode);
            Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
            Assert.Contains("Error:", error);
            Assert.Contains("cycle", error);
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task DiamondReuse_Succeeds()
    {
        var dir = CreateTempDir();
        try
        {
            var common = WriteFile(dir, "common.rsp", "--verbose");
            var left = WriteFile(dir, "left.rsp", "@" + common);
            var right = WriteFile(dir, "right.rsp", "@" + common);

            var (exitCode, output, error) = await RunCapturedAsync(["expand", "@" + left, "@" + right]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: True", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task MissingFile_ErrorsExitOne()
    {
        var missing = Path.Combine(Path.GetTempPath(), "rsp-missing-" + Guid.NewGuid().ToString("N") + ".rsp");

        var (exitCode, output, error) = await RunCapturedAsync(["expand", "@" + missing]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error:", error);
        Assert.Contains("not found", error);
    }

    [Fact]
    public async Task SingleFileOversize_ErrorsExitOne()
    {
        var dir = CreateTempDir();
        try
        {
            var path = Path.Combine(dir, "big.rsp");
            File.WriteAllText(path, "expand --verbose");
            using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
            {
                stream.SetLength(1024 * 1024 + 1);
            }

            var (exitCode, output, error) = await RunCapturedAsync(["@" + path]);

            Assert.Equal(1, exitCode);
            Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
            Assert.Contains("Error:", error);
            Assert.Contains("exceeds size limit", error);
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task TotalSizeOversize_ErrorsExitOne()
    {
        var dir = CreateTempDir();
        try
        {
            var references = new List<string>();
            for (var i = 0; i < 5; i++)
            {
                var path = Path.Combine(dir, $"chunk{i}.rsp");
                File.WriteAllText(path, "--verbose");
                using (var stream = new FileStream(path, FileMode.Open, FileAccess.Write, FileShare.None))
                {
                    stream.SetLength(900 * 1024);
                }

                references.Add("@" + path);
            }

            var args = new List<string> { "expand" };
            args.AddRange(references);
            var (exitCode, output, error) = await RunCapturedAsync([.. args]);

            Assert.Equal(1, exitCode);
            Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
            Assert.Contains("Error:", error);
            Assert.Contains("total size limit", error);
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task ArgCountOversize_ErrorsExitOne()
    {
        var dir = CreateTempDir();
        try
        {
            var content = string.Join(" ", Enumerable.Repeat("x", 10001));
            var file = WriteFile(dir, "many.rsp", content);

            var (exitCode, output, error) = await RunCapturedAsync(["expand", "@" + file]);

            Assert.Equal(1, exitCode);
            Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
            Assert.Contains("Error:", error);
            Assert.Contains("10000", error);
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task RelativePath_ResolvesAgainstCurrentDirectory()
    {
        var dir = CreateTempDir();
        var previous = Directory.GetCurrentDirectory();
        await ConsoleGate.WaitAsync();
        try
        {
            File.WriteAllText(Path.Combine(dir, "local.rsp"), "--verbose");
            Directory.SetCurrentDirectory(dir);

            var (exitCode, output, error) = await RunCapturedInnerAsync(["expand", "@local.rsp"]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: True", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            ConsoleGate.Release();
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task AbsolutePath_ResolvesRegardlessOfCurrentDirectory()
    {
        var fileDir = CreateTempDir();
        var cwdDir = CreateTempDir();
        var previous = Directory.GetCurrentDirectory();
        await ConsoleGate.WaitAsync();
        try
        {
            var file = WriteFile(fileDir, "args.rsp", "--verbose");
            Directory.SetCurrentDirectory(cwdDir);

            var (exitCode, output, error) = await RunCapturedInnerAsync(["expand", "@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Verbose: True", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            Directory.SetCurrentDirectory(previous);
            ConsoleGate.Release();
            DeleteDir(fileDir);
            DeleteDir(cwdDir);
        }
    }

    [Fact]
    public async Task CompletionCursorOnAt_YieldsNoCandidates()
    {
        var line = "rsp-test @partial";

        var (exitCode, output, error) = await RunCapturedAsync(["complete", "--position", line.Length.ToString(), line]);

        Assert.Equal(0, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
    }

    [Fact]
    public async Task CompletionProbeArgs_AreExpanded()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "probe.rsp", "expand");
            var line = $"rsp-test @{file} --ver";

            var (exitCode, output, error) = await RunCapturedAsync(["complete", "--position", line.Length.ToString(), line]);

            Assert.Equal(0, exitCode);
            Assert.Contains("--verbose", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task HelpFromFile_RendersHelpExitZero()
    {
        var dir = CreateTempDir();
        try
        {
            var file = WriteFile(dir, "args.rsp", "expand --help");

            var (exitCode, output, error) = await RunCapturedAsync(["@" + file]);

            Assert.Equal(0, exitCode);
            Assert.Contains("Expansion probe command.", output);
            Assert.True(string.IsNullOrWhiteSpace(error), $"Expected empty stderr but got: {error}");
        }
        finally
        {
            DeleteDir(dir);
        }
    }

    [Fact]
    public async Task MissingWithHelp_StillFaultsExitOne()
    {
        var missing = Path.Combine(Path.GetTempPath(), "rsp-missing-" + Guid.NewGuid().ToString("N") + ".rsp");

        var (exitCode, output, error) = await RunCapturedAsync(["@" + missing, "--help"]);

        Assert.Equal(1, exitCode);
        Assert.True(string.IsNullOrWhiteSpace(output), $"Expected empty stdout but got: {output}");
        Assert.Contains("Error:", error);
    }

    private static string BuildChain(string dir, int length, string tailContent)
    {
        var tail = WriteFile(dir, $"chain{length - 1}.rsp", tailContent);
        for (var i = length - 2; i >= 0; i--)
        {
            tail = WriteFile(dir, $"chain{i}.rsp", "@" + tail);
        }

        return tail;
    }

    private static string CreateTempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "rsp-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static string WriteFile(string dir, string name, string content)
    {
        var path = Path.Combine(dir, name);
        File.WriteAllText(path, content);
        return path;
    }

    private static void DeleteDir(string dir)
    {
        try { Directory.Delete(dir, recursive: true); } catch { }
    }

    private static ApplicationBuilder CreateBuilder()
    {
        return ApplicationBuilder.Create()
            .SetExecutableName("rsp-test")
            .SetExecutableTitle("Response File Test")
            .SetExecutableDescription("Response file expansion verification CLI.")
            .SetExecutableVersion("9.9.9")
            .AddCommand<ExpansionProbeCommand>();
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedAsync(string[] args)
    {
        await ConsoleGate.WaitAsync();
        try
        {
            return await RunCapturedInnerAsync(args);
        }
        finally
        {
            ConsoleGate.Release();
        }
    }

    private static async Task<(int ExitCode, string Output, string Error)> RunCapturedInnerAsync(string[] args)
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
}
