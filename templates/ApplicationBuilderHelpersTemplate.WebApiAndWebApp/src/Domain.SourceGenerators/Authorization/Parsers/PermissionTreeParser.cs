using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Domain.SourceGenerators.Authorization.Models;
using Domain.SourceGenerators.Utilities;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Domain.SourceGenerators.Authorization.Parsers;

internal static class PermissionTreeParser
{
    public static bool TryParsePermissionRoots(Compilation compilation, out ImmutableArray<PermissionTreeNode> roots, out ImmutableArray<string> errors)
    {
        var errorsBuilder = ImmutableArray.CreateBuilder<string>();

        var permissionsType = compilation.GetTypeByMetadataName("Domain.Authorization.Constants.Permissions");
        if (permissionsType is null)
        {
            errorsBuilder.Add("Authorization source generation is enabled, but type 'Domain.Authorization.Constants.Permissions' could not be found in the compilation.");

            roots = ImmutableArray<PermissionTreeNode>.Empty;
            errors = errorsBuilder.ToImmutable();
            return false;
        }

        // Dynamically discover root builder methods from BuildPermissionRoots()
        var rootMethodNames = DiscoverRootMethodNames(compilation, permissionsType, errorsBuilder);
        if (rootMethodNames.Length == 0)
        {
            if (errorsBuilder.Count == 0)
            {
                errorsBuilder.Add("No root builder method invocations found in BuildPermissionRoots().");
            }

            roots = ImmutableArray<PermissionTreeNode>.Empty;
            errors = errorsBuilder.ToImmutable();
            return false;
        }

        var rootList = ImmutableArray.CreateBuilder<PermissionTreeNode>();
        foreach (var methodName in rootMethodNames)
        {
            if (TryParseRootMethod(compilation, permissionsType, methodName, errorsBuilder, out var node))
            {
                if (node is null)
                {
                    errorsBuilder.Add($"{methodName} parsed successfully but produced no root.");
                }
                else
                {
                    rootList.Add(node);
                }
            }
        }

        if (rootList.Count == 0)
        {
            if (errorsBuilder.Count == 0)
            {
                errorsBuilder.Add("No permission roots were successfully parsed.");
            }

            roots = ImmutableArray<PermissionTreeNode>.Empty;
            errors = errorsBuilder.ToImmutable();
            return false;
        }

        roots = rootList.ToImmutable();
        errors = errorsBuilder.ToImmutable();
        return errors.Length == 0;
    }

    private static ImmutableArray<string> DiscoverRootMethodNames(
        Compilation compilation,
        INamedTypeSymbol permissionsType,
        ImmutableArray<string>.Builder errors)
    {
        var method = FindMethod(permissionsType, "BuildPermissionRoots");
        if (method is null)
        {
            errors.Add("Could not find BuildPermissionRoots method in Permissions class.");
            return ImmutableArray<string>.Empty;
        }

        if (method.DeclaringSyntaxReferences.Length == 0)
        {
            errors.Add("BuildPermissionRoots has no syntax reference.");
            return ImmutableArray<string>.Empty;
        }

        var syntax = method.DeclaringSyntaxReferences[0].GetSyntax() as MethodDeclarationSyntax;
        if (syntax?.Body is null)
        {
            errors.Add("BuildPermissionRoots must have a method body.");
            return ImmutableArray<string>.Empty;
        }

        var methodNames = ImmutableArray.CreateBuilder<string>();

        // Find all method invocations that match Build*Root pattern
        foreach (var node in syntax.Body.DescendantNodes())
        {
            if (node is InvocationExpressionSyntax invocation)
            {
                var name = GetInvocationName(invocation.Expression);
                if (name is not null && name.StartsWith("Build", StringComparison.Ordinal) && name.EndsWith("Root", StringComparison.Ordinal))
                {
                    if (!methodNames.Contains(name))
                    {
                        methodNames.Add(name);
                    }
                }
            }
        }

        return methodNames.ToImmutable();
    }

    private static bool TryParseRootMethod(
        Compilation compilation,
        INamedTypeSymbol permissionsType,
        string methodName,
        ImmutableArray<string>.Builder errors,
        out PermissionTreeNode? root)
    {
        root = null;

        var method = FindMethod(permissionsType, methodName);
        if (method is null)
        {
            return false;
        }

        if (method.DeclaringSyntaxReferences.Length == 0)
        {
            errors.Add($"Unsupported permission definition shape: {permissionsType.ToDisplayString()}.{methodName} has no syntax reference.");
            return false;
        }

        var syntax = method.DeclaringSyntaxReferences[0].GetSyntax() as MethodDeclarationSyntax;
        if (syntax is null)
        {
            errors.Add($"Unsupported permission definition shape: {permissionsType.ToDisplayString()}.{methodName} is not a method declaration.");
            return false;
        }

        var semanticModel = compilation.GetSemanticModel(syntax.SyntaxTree);

        ExpressionSyntax? expression = null;

        if (syntax.ExpressionBody is not null)
        {
            expression = syntax.ExpressionBody.Expression;
        }
        else
        {
            foreach (var statement in syntax.Body?.Statements ?? default)
            {
                if (statement is ReturnStatementSyntax returnStatement)
                {
                    expression = returnStatement.Expression;
                    break;
                }
            }
        }

        if (expression is not InvocationExpressionSyntax invocation)
        {
            errors.Add($"Unsupported permission definition shape: {methodName} must return a Node(...) invocation.");
            return false;
        }

        if (!TryParsePermissionInvocation(semanticModel, invocation, out var parsed, out var error))
        {
            errors.Add($"Unsupported permission definition shape: {error}");
            return false;
        }

        root = parsed;
        return true;
    }

    private static IMethodSymbol? FindMethod(INamedTypeSymbol type, string name)
    {
        foreach (var member in type.GetMembers(name))
        {
            if (member is IMethodSymbol method && method.MethodKind == MethodKind.Ordinary)
            {
                return method;
            }
        }

        return null;
    }

    private static bool TryParsePermissionInvocation(
        SemanticModel semanticModel,
        InvocationExpressionSyntax invocation,
        out PermissionTreeNode? node,
        out string error)
    {
        node = null;

        var name = GetInvocationName(invocation.Expression);
        if (name is null)
        {
            error = "Unable to resolve invocation name.";
            return false;
        }

        if (!string.Equals(name, "Node", StringComparison.Ordinal) &&
            !string.Equals(name, "RLeaf", StringComparison.Ordinal) &&
            !string.Equals(name, "WLeaf", StringComparison.Ordinal))
        {
            error = $"Unsupported invocation '{name}'. Expected Node/RLeaf/WLeaf.";
            return false;
        }

        var args = invocation.ArgumentList.Arguments;

        if (string.Equals(name, "Node", StringComparison.Ordinal))
        {
            if (args.Count < 3)
            {
                error = "Node(...) must have at least 3 arguments.";
                return false;
            }

            if (!SyntaxValueExtraction.TryGetConstantString(semanticModel, args[0].Expression, out var identifier) ||
                !SyntaxValueExtraction.TryGetConstantString(semanticModel, args[1].Expression, out var description) ||
                !SyntaxValueExtraction.TryGetConstantString(semanticModel, args[2].Expression, out var scopeLabel))
            {
                error = "Node(...) identifier/description/scopeLabel must be constant strings.";
                return false;
            }

            var localParameters = ImmutableArray<string>.Empty;
            var children = ImmutableArray.CreateBuilder<PermissionTreeNode>();

            for (var i = 3; i < args.Count; i++)
            {
                var expr = args[i].Expression;

                if (expr is InvocationExpressionSyntax childInvocation)
                {
                    if (!TryParsePermissionInvocation(semanticModel, childInvocation, out var childNode, out error))
                    {
                        return false;
                    }

                    if (childNode is null)
                    {
                        error = "Child node parsed successfully but produced no node.";
                        return false;
                    }

                    children.Add(childNode);
                    continue;
                }

                if (SyntaxValueExtraction.TryGetStringList(semanticModel, expr, out var parameters))
                {
                    localParameters = parameters;
                    continue;
                }

                error = $"Node(...) params element is unsupported: {expr.Kind()}";
                return false;
            }

            // Inject implicit _read/_write scopes (these are real permissions).
            children.Insert(0, new PermissionTreeNode(
                kind: PermissionNodeKind.Scope,
                access: PermissionAccess.Read,
                identifier: "_read",
                description: $"Read access to {scopeLabel}.",
                scopeLabel: null,
                localParameters: ImmutableArray<string>.Empty,
                children: ImmutableArray<PermissionTreeNode>.Empty));

            children.Insert(1, new PermissionTreeNode(
                kind: PermissionNodeKind.Scope,
                access: PermissionAccess.Write,
                identifier: "_write",
                description: $"Write access to {scopeLabel}.",
                scopeLabel: null,
                localParameters: ImmutableArray<string>.Empty,
                children: ImmutableArray<PermissionTreeNode>.Empty));

            node = new PermissionTreeNode(
                kind: PermissionNodeKind.Node,
                access: PermissionAccess.Unspecified,
                identifier: identifier,
                description: description,
                scopeLabel: scopeLabel,
                localParameters: localParameters,
                children: children.ToImmutable());

            error = string.Empty;
            return true;
        }

        // Leaf
        if (args.Count < 2)
        {
            error = $"{name}(...) must have at least 2 arguments.";
            return false;
        }

        if (!SyntaxValueExtraction.TryGetConstantString(semanticModel, args[0].Expression, out var leafIdentifier) ||
            !SyntaxValueExtraction.TryGetConstantString(semanticModel, args[1].Expression, out var leafDescription))
        {
            error = $"{name}(...) identifier/description must be constant strings.";
            return false;
        }

        var leafParameters = ImmutableArray<string>.Empty;
        if (args.Count >= 3)
        {
            if (!SyntaxValueExtraction.TryGetStringList(semanticModel, args[2].Expression, out leafParameters))
            {
                error = $"{name}(...) parameters must be a constant string list.";
                return false;
            }
        }

        node = new PermissionTreeNode(
            kind: PermissionNodeKind.Leaf,
            access: string.Equals(name, "RLeaf", StringComparison.Ordinal) ? PermissionAccess.Read : PermissionAccess.Write,
            identifier: leafIdentifier,
            description: leafDescription,
            scopeLabel: null,
            localParameters: leafParameters,
            children: ImmutableArray<PermissionTreeNode>.Empty);

        error = string.Empty;
        return true;
    }

    private static string? GetInvocationName(ExpressionSyntax expression)
    {
        return expression switch
        {
            IdentifierNameSyntax identifier => identifier.Identifier.ValueText,
            MemberAccessExpressionSyntax member => member.Name.Identifier.ValueText,
            _ => null
        };
    }
}
