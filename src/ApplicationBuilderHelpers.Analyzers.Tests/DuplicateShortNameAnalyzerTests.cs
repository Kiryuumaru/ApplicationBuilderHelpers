using System.Collections.Immutable;
using System.Linq;
using ApplicationBuilderHelpers.Analyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ApplicationBuilderHelpers.Analyzers.Tests;

/// <summary>
/// Verifies the ABH001 compile-time gate with an in-memory
/// <see cref="CSharpCompilation"/>. The harness compiles small sources that
/// declare a local <c>CommandOptionAttribute</c> stub (same name, same ctors,
/// same settable <c>Term</c>/<c>ShortTerm</c>) so the test project never
/// constrains the analyzer to the real library: the analyzer matches by
/// attribute simple name, exactly as it does for consumers.
/// </summary>
public sealed class DuplicateShortNameAnalyzerTests
{
    private const string AttributeStub = """
        using System;
        [AttributeUsage(AttributeTargets.Property)]
        public sealed class CommandOptionAttribute : Attribute
        {
            public CommandOptionAttribute(char shortTerm, string term) { ShortTerm = shortTerm; Term = term; }
            public CommandOptionAttribute(char shortTerm) { ShortTerm = shortTerm; Term = null; }
            public CommandOptionAttribute(string term) { ShortTerm = null; Term = term; }
            public string? Term { get; set; }
            public char? ShortTerm { get; set; }
        }
        """;

    [Fact]
    public void SameClassCollision_ReportsABH001Error()
    {
        const string source = AttributeStub + """
            public sealed class CollisionCommand
            {
                [CommandOption('l', "level")]
                public string Level { get; set; } = "information";
                [CommandOption('l', "local")]
                public bool LocalOnly { get; set; }
            }
            """;

        var diagnostics = GetAnalyzerDiagnostics(source);

        var error = Assert.Single(diagnostics);
        Assert.Equal(DuplicateShortNameAnalyzer.DiagnosticId, error.Id);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("'-l'", error.GetMessage());
        Assert.Contains("-l, --level", error.GetMessage());
        Assert.Contains("-l, --local", error.GetMessage());
    }

    [Fact]
    public void BasePlusDerivedCollision_ReportsABH001Error()
    {
        const string source = AttributeStub + """
            public class BaseCommand
            {
                [CommandOption('l', "log-level")]
                public string LogLevel { get; set; } = "information";
            }
            public sealed class DerivedCommand : BaseCommand
            {
                [CommandOption('l', "local")]
                public bool LocalOnly { get; set; }
            }
            """;

        var diagnostics = GetAnalyzerDiagnostics(source, "DerivedCommand");

        var error = Assert.Single(diagnostics);
        Assert.Equal(DuplicateShortNameAnalyzer.DiagnosticId, error.Id);
        Assert.Equal(DiagnosticSeverity.Error, error.Severity);
        Assert.Contains("'-l'", error.GetMessage());
    }

    [Fact]
    public void SameKeyCopies_ReportNoDiagnostic()
    {
        const string source = AttributeStub + """
            public sealed class SameKeyCommand
            {
                [CommandOption('l', "level")]
                public string Level { get; set; } = "information";
                [CommandOption('l', "level")]
                public string LevelCopy { get; set; } = "information";
            }
            """;

        var diagnostics = GetAnalyzerDiagnostics(source);

        Assert.Empty(diagnostics);
    }

    [Fact]
    public void LongOnlyBesideShort_ReportsNoDiagnostic()
    {
        const string source = AttributeStub + """
            public sealed class LongOnlyCommand
            {
                [CommandOption('l', "level")]
                public string Level { get; set; } = "information";
                [CommandOption("local")]
                public string Local { get; set; } = "default";
            }
            """;

        var diagnostics = GetAnalyzerDiagnostics(source);

        Assert.Empty(diagnostics);
    }

    private static ImmutableArray<Diagnostic> GetAnalyzerDiagnostics(string source, string? typeName = null)
    {
        var syntaxTree = CSharpSyntaxTree.ParseText(source);
        var references = AppDomain.CurrentDomain.GetAssemblies()
            .Where(a => !a.IsDynamic && !string.IsNullOrEmpty(a.Location))
            .Select(a => MetadataReference.CreateFromFile(a.Location))
            .Cast<MetadataReference>()
            .ToList();
        var compilation = CSharpCompilation.Create(
            "AnalyzerHarness",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        var analyzer = new DuplicateShortNameAnalyzer();
        var withAnalyzers = compilation.WithAnalyzers(ImmutableArray.Create<DiagnosticAnalyzer>(analyzer));
        var diagnostics = withAnalyzers.GetAnalyzerDiagnosticsAsync().GetAwaiter().GetResult();
        var relevant = typeName is null
            ? diagnostics
            : diagnostics.Where(d => d.Location.IsInSource && d.GetMessage().Contains(typeName, StringComparison.Ordinal));
        return relevant.ToImmutableArray();
    }
}
