using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Domain.SourceGenerators.Utilities;

internal static class SyntaxValueExtraction
{
    public static bool TryGetConstantString(SemanticModel semanticModel, ExpressionSyntax expression, out string value)
    {
        var constant = semanticModel.GetConstantValue(expression);
        if (constant.HasValue && constant.Value is string s)
        {
            value = s;
            return true;
        }

        if (expression is LiteralExpressionSyntax { Token.ValueText: { } literal } && expression.IsKind(SyntaxKind.StringLiteralExpression))
        {
            value = literal;
            return true;
        }

        value = string.Empty;
        return false;
    }

    public static bool TryGetStringList(SemanticModel semanticModel, ExpressionSyntax expression, out ImmutableArray<string> values)
    {
        // Supports:
        // - new[] { "a", "b" }
        // - new string[] { "a" }
        // - ["a", "b"] (collection expression)
        // - Array.Empty<string>() (treated as empty)

        if (expression is InvocationExpressionSyntax invocation)
        {
            var symbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (symbol is not null && symbol.Name == "Empty" && symbol.ContainingType?.ToDisplayString() == "System.Array")
            {
                values = ImmutableArray<string>.Empty;
                return true;
            }
        }

        var list = new List<string>();

        if (expression is ArrayCreationExpressionSyntax arrayCreation)
        {
            if (arrayCreation.Initializer is null)
            {
                values = ImmutableArray<string>.Empty;
                return true;
            }

            foreach (var expr in arrayCreation.Initializer.Expressions)
            {
                if (!TryGetConstantString(semanticModel, expr, out var s))
                {
                    values = default;
                    return false;
                }

                list.Add(s);
            }

            values = [.. list];
            return true;
        }

        if (expression is ImplicitArrayCreationExpressionSyntax implicitArray)
        {
            foreach (var expr in implicitArray.Initializer.Expressions)
            {
                if (!TryGetConstantString(semanticModel, expr, out var s))
                {
                    values = default;
                    return false;
                }

                list.Add(s);
            }

            values = [.. list];
            return true;
        }

        if (expression is CollectionExpressionSyntax collectionExpression)
        {
            foreach (var element in collectionExpression.Elements)
            {
                if (element is not ExpressionElementSyntax exprElement)
                {
                    values = default;
                    return false;
                }

                if (!TryGetConstantString(semanticModel, exprElement.Expression, out var s))
                {
                    values = default;
                    return false;
                }

                list.Add(s);
            }

            values = [.. list];
            return true;
        }

        values = default;
        return false;
    }
}
