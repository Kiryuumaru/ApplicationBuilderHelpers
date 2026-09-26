using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace ApplicationBuilderHelpers.Analyzers;

[DiagnosticAnalyzer(LanguageNames.CSharp)]
public sealed class DuplicateShortNameAnalyzer : DiagnosticAnalyzer
{
    public const string DiagnosticId = "ABH001";

    private static readonly DiagnosticDescriptor Rule = new(
        DiagnosticId,
        "Duplicate short option name",
        "Duplicate short name conflict: '-{0}' on options '{1}' and '{2}' in command '{3}'. Rename or remove one of the short names.",
        "Usage",
        DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(compilationContext =>
        {
            compilationContext.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
        });
    }

    private static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        if (context.Symbol is not INamedTypeSymbol namedType)
            return;

        var options = CollectOptions(namedType);
        if (options.Count < 2)
            return;

        foreach (var group in options.GroupBy(o => o.ShortName))
        {
            var distinct = group
                .GroupBy(o => o.CanonicalKey, StringComparer.Ordinal)
                .Select(g => g.First())
                .ToList();
            if (distinct.Count < 2)
                continue;

            var first = distinct[0];
            for (var i = 1; i < distinct.Count; i++)
            {
                var duplicate = distinct[i];
                context.ReportDiagnostic(Diagnostic.Create(
                    Rule,
                    duplicate.Location,
                    group.Key.ToString(),
                    first.DisplayName,
                    duplicate.DisplayName,
                    namedType.Name));
            }
        }
    }

    private static List<OptionEntry> CollectOptions(INamedTypeSymbol namedType)
    {
        var options = new List<OptionEntry>();
        var current = namedType;
        while (current is not null && current.SpecialType != SpecialType.System_Object)
        {
            foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
            {
                if (!SymbolEqualityComparer.Default.Equals(property.ContainingType, current))
                    continue;

                var attribute = property.GetAttributes().FirstOrDefault(
                    a => string.Equals(a.AttributeClass?.Name, "CommandOptionAttribute", StringComparison.Ordinal));
                if (attribute is null)
                    continue;

                if (!TryReadShortTerm(attribute, out var shortName) || shortName is null)
                    continue;

                var term = ReadTerm(attribute);
                var canonicalKey = term ?? property.Name.ToLowerInvariant();
                var displayName = $"-{shortName.Value}, --{canonicalKey}";
                var location = attribute.ApplicationSyntaxReference?.GetSyntax().GetLocation()
                    ?? property.Locations.FirstOrDefault();
                if (location is null)
                    continue;

                options.Add(new OptionEntry(shortName.Value, canonicalKey, displayName, location));
            }

            current = current.BaseType;
        }

        return options;
    }

    private static bool TryReadShortTerm(AttributeData attribute, out char? shortName)
    {
        char? positional = null;
        var args = attribute.ConstructorArguments;
        if (args.Length == 2)
            positional = AsChar(args[0]);
        else if (args.Length == 1 && args[0].Value is not string)
            positional = AsChar(args[0]);

        shortName = positional;
        foreach (var named in attribute.NamedArguments)
        {
            if (string.Equals(named.Key, "ShortTerm", StringComparison.Ordinal))
                shortName = AsChar(named.Value);
        }

        return true;
    }

    private static string? ReadTerm(AttributeData attribute)
    {
        string? positional = null;
        var args = attribute.ConstructorArguments;
        if (args.Length == 2)
            positional = AsString(args[1]);
        else if (args.Length == 1 && args[0].Value is string)
            positional = AsString(args[0]);

        var term = positional;
        foreach (var named in attribute.NamedArguments)
        {
            if (string.Equals(named.Key, "Term", StringComparison.Ordinal))
                term = AsString(named.Value);
        }

        return term;
    }

    private static char? AsChar(TypedConstant constant)
    {
        if (constant.IsNull)
            return null;
        return constant.Value is char c ? c : null;
    }

    private static string? AsString(TypedConstant constant)
    {
        if (constant.IsNull)
            return null;
        return constant.Value as string;
    }

    private sealed class OptionEntry(char shortName, string canonicalKey, string displayName, Location location)
    {
        public char ShortName { get; } = shortName;
        public string CanonicalKey { get; } = canonicalKey;
        public string DisplayName { get; } = displayName;
        public Location Location { get; } = location;
    }
}
