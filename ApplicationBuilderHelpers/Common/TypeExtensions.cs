using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.ParserTypes;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Common;

internal static class TypeExtensions
{
    //public static bool TryGetParser(this Type type, Dictionary<Type, ICommandTypeParser> typeParsers, bool caseSensitive, bool autoEnumerableType, [NotNullWhen(true)] out ICommandTypeParser? commandLineTypeParser)
    //{
    //    if (typeParsers.TryGetValue(type, out ICommandTypeParser? typeParser))
    //    {
    //        commandLineTypeParser = typeParser;
    //        return true;
    //    }
    //    if (type.IsEnum)
    //    {
    //        commandLineTypeParser = new EnumTypeParser(type, caseSensitive);
    //        return true;
    //    }

    //    if (autoEnumerableType)
    //    {
    //        if (type.IsArray)
    //        {
    //            if (type.GetElementType() is Type elementType && TryGetParser(elementType, typeParsers, caseSensitive, true, out ICommandTypeParser? itemTypeParser))
    //            {
    //                commandLineTypeParser = new ArrayTypeParser(elementType, itemTypeParser);
    //                return true;
    //            }
    //        }
    //        if (typeof(IDictionary).IsAssignableFrom(type))
    //        {
    //            commandLineTypeParser = new DictionaryTypeParser(type);
    //            return true;
    //        }
    //        if (typeof(ICollection).IsAssignableFrom(type))
    //        {
    //            commandLineTypeParser = new CollectionTypeParser(type);
    //            return true;
    //        }
    //        if (typeof(IEnumerable).IsAssignableFrom(type))
    //        {
    //            commandLineTypeParser = new EnumerableTypeParser(type);
    //            return true;
    //        }
    //    }

    //    commandLineTypeParser = null;
    //    return false;
    //}

    //public static bool IsEnumerable(this Type type, Dictionary<Type, ICommandTypeParser> typeParsers)
    //{
    //    if (typeParsers.TryGetValue(type, out _))
    //    {
    //        return false;
    //    }
    //    if (type.IsEnum)
    //    {
    //        return false;
    //    }

    //    if (type.IsArray)
    //    {
    //        if (type.GetElementType() is Type elementType && TryGetParser(elementType, typeParsers, true, true, out ICommandTypeParser? itemTypeParser))
    //        {
    //            return true;
    //        }
    //    }
    //    if (typeof(IDictionary).IsAssignableFrom(type))
    //    {
    //        return true;
    //    }
    //    if (typeof(ICollection).IsAssignableFrom(type))
    //    {
    //        return true;
    //    }
    //    if (typeof(IEnumerable).IsAssignableFrom(type))
    //    {
    //        return true;
    //    }

    //    return false;
    //}
}
