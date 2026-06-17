using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Domain.SourceGenerators.Authorization.Models;
using Domain.SourceGenerators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Domain.SourceGenerators.Authorization.Parsers;

internal static class RoleIdsParser
{
    public static bool TryParseRoles(Compilation compilation, out ImmutableArray<RoleDefinitionInfo> roles, out ImmutableArray<string> errors)
    {
        var errorsBuilder = ImmutableArray.CreateBuilder<string>();

        var rolesType = compilation.GetTypeByMetadataName("Domain.Authorization.Constants.Roles");
        if (rolesType is null)
        {
            errorsBuilder.Add("Authorization source generation is enabled, but type 'Domain.Authorization.Constants.Roles' could not be found in the compilation.");

            roles = ImmutableArray<RoleDefinitionInfo>.Empty;
            errors = errorsBuilder.ToImmutable();
            return false;
        }

        if (rolesType.DeclaringSyntaxReferences.Length == 0)
        {
            errorsBuilder.Add("Unsupported role definition shape: Roles type has no syntax reference.");

            roles = ImmutableArray<RoleDefinitionInfo>.Empty;
            errors = errorsBuilder.ToImmutable();
            return false;
        }

        var syntax = rolesType.DeclaringSyntaxReferences[0].GetSyntax() as TypeDeclarationSyntax;
        if (syntax is null)
        {
            errorsBuilder.Add("Unsupported role definition shape: Roles declaration syntax not found.");

            roles = ImmutableArray<RoleDefinitionInfo>.Empty;
            errors = errorsBuilder.ToImmutable();
            return false;
        }

        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

        // Find static constructor: static Roles() { ... }
        ConstructorDeclarationSyntax? staticCtor = null;
        foreach (var member in syntax.Members)
        {
            if (member is ConstructorDeclarationSyntax ctor && ctor.Modifiers.Any(static m => m.IsKind(SyntaxKind.StaticKeyword)))
            {
                staticCtor = ctor;
                break;
            }
        }

        if (staticCtor?.Body is null)
        {
            errorsBuilder.Add("Unsupported role definition shape: Roles must define a static constructor with a body.");

            roles = ImmutableArray<RoleDefinitionInfo>.Empty;
            errors = errorsBuilder.ToImmutable();
            return false;
        }

        var found = new List<RoleDefinitionInfo>();

        foreach (var statement in staticCtor.Body.Statements)
        {
            if (statement is not ExpressionStatementSyntax exprStatement)
            {
                continue;
            }

            if (exprStatement.Expression is not AssignmentExpressionSyntax assignment)
            {
                continue;
            }

            if (assignment.Right is not ObjectCreationExpressionSyntax creation)
            {
                continue;
            }

            var symbol = semanticModel.GetSymbolInfo(creation).Symbol;
            if (symbol is not IMethodSymbol ctorSymbol)
            {
                continue;
            }

            if (!string.Equals(ctorSymbol.ContainingType.ToDisplayString(), "Domain.Authorization.Constants.RoleDefinition", StringComparison.Ordinal))
            {
                continue;
            }

            if (!TryReadRoleDefinition(semanticModel, creation, out var role, out var error))
            {
                errorsBuilder.Add($"Unsupported role definition shape: {error}");
                continue;
            }

            if (role is null)
            {
                errorsBuilder.Add("Role parsed successfully but produced no role definition.");
                continue;
            }

            found.Add(role);
        }

        if (found.Count == 0)
        {
            if (errorsBuilder.Count == 0)
            {
                errorsBuilder.Add("Unsupported role definition shape: No RoleDefinition(...) assignments found in static constructor.");
            }

            roles = ImmutableArray<RoleDefinitionInfo>.Empty;
            errors = errorsBuilder.ToImmutable();
            return false;
        }

        roles = [.. found];
        errors = errorsBuilder.ToImmutable();
        return errors.Length == 0;
    }

    private static bool TryReadRoleDefinition(
        SemanticModel semanticModel,
        ObjectCreationExpressionSyntax creation,
        out RoleDefinitionInfo? role,
        out string error)
    {
        role = null;

        string? code = null;
        string? name = null;
        var parameters = ImmutableArray<string>.Empty;

        foreach (var arg in creation.ArgumentList?.Arguments ?? default)
        {
            if (arg.NameColon is null)
            {
                continue;
            }

            var nameKey = arg.NameColon.Name.Identifier.ValueText;

            if (string.Equals(nameKey, "Code", StringComparison.Ordinal))
            {
                if (!SyntaxValueExtraction.TryGetConstantString(semanticModel, arg.Expression, out var s))
                {
                    error = "RoleDefinition Code must be a constant string";
                    return false;
                }

                code = s;
                continue;
            }

            if (string.Equals(nameKey, "Name", StringComparison.Ordinal))
            {
                if (!SyntaxValueExtraction.TryGetConstantString(semanticModel, arg.Expression, out var s))
                {
                    error = "RoleDefinition Name must be a constant string";
                    return false;
                }

                name = s;
                continue;
            }

            if (string.Equals(nameKey, "TemplateParametersOverride", StringComparison.Ordinal))
            {
                if (!SyntaxValueExtraction.TryGetStringList(semanticModel, arg.Expression, out var list))
                {
                    error = "RoleDefinition TemplateParametersOverride must be a constant string list";
                    return false;
                }

                parameters = list;
            }
        }

        if (string.IsNullOrWhiteSpace(code) || string.IsNullOrWhiteSpace(name))
        {
            error = "RoleDefinition must provide named Code and Name arguments";
            return false;
        }

        if (code is null || name is null)
        {
            error = "RoleDefinition Code/Name could not be resolved";
            return false;
        }

        role = new RoleDefinitionInfo(code, name, parameters);
        error = string.Empty;
        return true;
    }
}
