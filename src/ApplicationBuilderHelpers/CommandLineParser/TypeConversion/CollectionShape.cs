using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser.TypeConversion;

/// <summary>
/// Describes the supported collection shapes for CLI-bound properties:
/// exactly <c>T[]</c>, <c>List&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>,
/// <c>ICollection&lt;T&gt;</c>, or <c>IList&lt;T&gt;</c>.
/// </summary>
internal enum CollectionKind
{
    Array,
    List,
    Enumerable,
    Collection,
    ListInterface,
}

/// <summary>
/// Shape detection and materialization for collection-valued command properties.
/// Only exactly <c>T[]</c>, <c>List&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>,
/// <c>ICollection&lt;T&gt;</c>, and <c>IList&lt;T&gt;</c> count as collections;
/// <see cref="string"/> is never a collection.
/// </summary>
internal static class CollectionShape
{
    /// <summary>
    /// Returns true when <paramref name="propertyType"/> is exactly
    /// <c>T[]</c>, <c>List&lt;T&gt;</c>, <c>IEnumerable&lt;T&gt;</c>,
    /// <c>ICollection&lt;T&gt;</c>, or <c>IList&lt;T&gt;</c>.
    /// </summary>
    internal static bool IsCollection(Type propertyType)
    {
        if (propertyType == typeof(string))
        {
            return false;
        }

        if (propertyType.IsArray)
        {
            return propertyType.GetElementType() is not null;
        }

        if (propertyType.IsGenericType)
        {
            var definition = propertyType.GetGenericTypeDefinition();
            return definition == typeof(List<>)
                || definition == typeof(IEnumerable<>)
                || definition == typeof(ICollection<>)
                || definition == typeof(IList<>);
        }

        return false;
    }

    /// <summary>
    /// Gets the element type for a supported collection shape.
    /// Returns false (with <paramref name="elementType"/> null) for anything else.
    /// </summary>
    internal static bool TryGetElementType(Type propertyType, out Type? elementType)
    {
        elementType = null;

        if (propertyType == typeof(string))
        {
            return false;
        }

        if (propertyType.IsArray)
        {
            elementType = propertyType.GetElementType();
            return elementType is not null;
        }

        if (propertyType.IsGenericType)
        {
            var definition = propertyType.GetGenericTypeDefinition();
            if (definition == typeof(List<>)
                || definition == typeof(IEnumerable<>)
                || definition == typeof(ICollection<>)
                || definition == typeof(IList<>))
            {
                elementType = propertyType.GetGenericArguments()[0];
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the <see cref="CollectionKind"/> for a supported collection shape.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="propertyType"/> is not a supported collection shape.</exception>
    internal static CollectionKind GetKind(Type propertyType)
    {
        if (propertyType.IsArray && propertyType != typeof(string) && propertyType.GetElementType() is not null)
        {
            return CollectionKind.Array;
        }

        if (propertyType.IsGenericType)
        {
            var definition = propertyType.GetGenericTypeDefinition();
            if (definition == typeof(List<>))
            {
                return CollectionKind.List;
            }

            if (definition == typeof(IEnumerable<>))
            {
                return CollectionKind.Enumerable;
            }

            if (definition == typeof(ICollection<>))
            {
                return CollectionKind.Collection;
            }

            if (definition == typeof(IList<>))
            {
                return CollectionKind.ListInterface;
            }
        }

        throw new ArgumentException($"Type '{propertyType.FullName}' is not a supported collection shape (T[], List<T>, IEnumerable<T>, ICollection<T>, IList<T>).", nameof(propertyType));
    }

    /// <summary>
    /// Materializes the already-converted element values into the target <paramref name="propertyType"/> shape.
    /// Arrays go through the element parser's <c>CreateTypedArray</c> with an <c>object[]</c> fallback;
    /// <c>List&lt;T&gt;</c>/<c>IEnumerable&lt;T&gt;</c>/<c>ICollection&lt;T&gt;</c>/<c>IList&lt;T&gt;</c>
    /// are built via <c>List&lt;T&gt;</c> construction (a <c>List&lt;T&gt;</c> instance satisfies
    /// all four interface/class shapes).
    /// </summary>
    internal static object? Create(Type propertyType, Type elementType, IReadOnlyList<object?> converted, ICommandTypeParserCollection typeParsers)
    {
        var kind = GetKind(propertyType);

        if (kind == CollectionKind.Array)
        {
            Array array;
            if (typeParsers.TypeParsers.TryGetValue(elementType, out var parser))
            {
                try
                {
                    array = parser.CreateTypedArray(converted.Count);
                }
                catch
                {
                    array = new object?[converted.Count];
                }
            }
            else
            {
                array = new object?[converted.Count];
            }

            for (int i = 0; i < converted.Count; i++)
            {
                array.SetValue(converted[i], i);
            }

            return array;
        }

#pragma warning disable IL3050 // Element types come from command property metadata preserved for trimming; List<T>(int) is a built-in constructor.
        var listType = typeof(List<>).MakeGenericType(elementType);
        var list = (IList)Activator.CreateInstance(listType, converted.Count)!;
#pragma warning restore IL3050
        foreach (var item in converted)
        {
            list.Add(item);
        }

        return list;
    }
}
