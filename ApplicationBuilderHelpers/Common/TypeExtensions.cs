using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.ParserTypes;
using ApplicationBuilderHelpers.ParserTypes.Enumerables;
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
    public static bool TryGetParser(this Type type, Dictionary<Type, ICommandLineTypeParser> typeParsers, bool caseSensitive, bool autoEnumerableType, [NotNullWhen(true)] out ICommandLineTypeParser? commandLineTypeParser)
    {
        if (typeParsers.TryGetValue(type, out ICommandLineTypeParser? typeParser))
        {
            commandLineTypeParser = typeParser;
            return true;
        }
        if (type.IsEnum)
        {
            commandLineTypeParser = new EnumTypeParser(type, caseSensitive);
            return true;
        }

        if (autoEnumerableType)
        {
            if (type.IsArray)
            {
                if (type.GetElementType() is Type elementType && TryGetParser(elementType, typeParsers, caseSensitive, true, out ICommandLineTypeParser? itemTypeParser))
                {
                    commandLineTypeParser = new ArrayTypeParser(type, elementType, itemTypeParser);
                    return true;
                }
            }
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                commandLineTypeParser = new DictionaryTypeParser(type);
                return true;
            }
            if (typeof(ICollection).IsAssignableFrom(type))
            {
                commandLineTypeParser = new CollectionTypeParser(type);
                return true;
            }
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                commandLineTypeParser = new EnumerableTypeParser(type);
                return true;
            }
        }

        commandLineTypeParser = null;
        return false;
    }

    public static bool IsEnumerable(this Type type)
    {
        return typeof(IEnumerable).IsAssignableFrom(type);
    }
}
