using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser.TypeConversion;

internal enum CollectionKind
{
    Array,
    List,
    Enumerable,
    Collection,
    ListInterface,
}

internal static class CollectionShape
{
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

    /// <summary>Gets the collection kind; throws if not a supported shape.</summary>
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
