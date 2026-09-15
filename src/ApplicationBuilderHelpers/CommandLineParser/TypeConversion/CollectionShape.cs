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
    /// Arrays go through the element parser's <c>CreateTypedArray</c> (AOT-safe:
    /// the generic <c>CommandTypeParser&lt;T&gt;</c> factory is <c>new T[length]</c>).
    /// <c>List&lt;T&gt;</c>/<c>IEnumerable&lt;T&gt;</c>/<c>ICollection&lt;T&gt;</c>/<c>IList&lt;T&gt;</c>
    /// go through the element parser's <c>CreateTypedList</c> (AOT-safe:
    /// the generic <c>CommandTypeParser&lt;T&gt;</c> factory is <c>new List&lt;T&gt;(capacity)</c>;
    /// a <c>List&lt;T&gt;</c> instance satisfies all four interface/class shapes).
    /// The only fallback is an exactly-typed <c>new List&lt;object?&gt;(capacity)</c> when the
    /// element type is <see cref="object"/> (which <em>is</em> the real element type,
    /// so it stays assignable); any other missing parser or factory failure throws
    /// a styled <see cref="ConversionErrors.CollectionMaterialization"/> error
    /// (exit 2, <c>InvalidValue</c>) instead of returning a wrong-typed collection
    /// (assigning e.g. <c>object[]</c> into an <c>int[]</c> property throws
    /// <see cref="ArgumentException"/> at the bind site).
    /// </summary>
    internal static object? Create(Type propertyType, Type elementType, IReadOnlyList<object?> converted, ICommandTypeParserCollection typeParsers, string? displayName = null, bool isSecret = false)
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
                catch (Exception) when (elementType == typeof(object))
                {
                    array = new object?[converted.Count];
                }
                catch (Exception ex)
                {
                    string tail = isSecret ? SecretRedaction.Mask : ex.Message;
                    throw ConversionErrors.CollectionMaterialization(propertyType, elementType, $"The type parser for element type '{elementType.FullName}' failed to create a typed array: {tail}", displayName, isSecret);
                }
            }
            else if (elementType == typeof(object))
            {
                array = new object?[converted.Count];
            }
            else
            {
                throw ConversionErrors.CollectionMaterialization(propertyType, elementType, $"No type parser is registered for element type '{elementType.FullName}'. Register one via AddCommandTypeParser.", displayName, isSecret);
            }

            for (int i = 0; i < converted.Count; i++)
            {
                array.SetValue(converted[i], i);
            }

            return array;
        }

        IList list;
        if (typeParsers.TypeParsers.TryGetValue(elementType, out var listParser))
        {
            try
            {
                list = listParser.CreateTypedList(converted.Count);
            }
            catch (Exception) when (elementType == typeof(object))
            {
                list = new List<object?>(converted.Count);
            }
            catch (Exception ex)
            {
                string listTail = isSecret ? SecretRedaction.Mask : ex.Message;
                throw ConversionErrors.CollectionMaterialization(propertyType, elementType, $"The type parser for element type '{elementType.FullName}' failed to create a typed list: {listTail}", displayName, isSecret);
            }
        }
        else if (elementType == typeof(object))
        {
            list = new List<object?>(converted.Count);
        }
        else
        {
            throw ConversionErrors.CollectionMaterialization(propertyType, elementType, $"No type parser is registered for element type '{elementType.FullName}'. Register one via AddCommandTypeParser.", displayName, isSecret);
        }

        foreach (var item in converted)
        {
            list.Add(item);
        }

        return list;
    }
}
