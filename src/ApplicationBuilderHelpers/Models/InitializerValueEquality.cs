using System;
using System.Collections;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.Models;

/// <summary>
/// Shared initializer-default value semantics.
/// </summary>
internal static class InitializerValueEquality
{
    internal static object? CloneIfArray(object? value) =>
        value is Array array ? (Array)array.Clone() : value;

    internal static bool ValuesEqual(object? first, object? second)
    {
        if (first is Array firstArray && second is Array secondArray)
        {
            if (firstArray.Length != secondArray.Length)
                return false;
            for (var i = 0; i < firstArray.Length; i++)
            {
                if (!Equals(firstArray.GetValue(i), secondArray.GetValue(i)))
                    return false;
            }

            return true;
        }

        if (first is Array || second is Array)
            return false;

        if (first is not null && second is not null
            && IsExactList(first) && IsExactList(second))
        {
            if (first.GetType() != second.GetType())
                return false;

            var firstList = (IList)first;
            var secondList = (IList)second;
            if (firstList.Count != secondList.Count)
                return false;
            for (var i = 0; i < firstList.Count; i++)
            {
                if (!Equals(firstList[i], secondList[i]))
                    return false;
            }

            return true;
        }

        if (IsOtherEnumerable(first) || IsOtherEnumerable(second))
            return false;

        return Equals(first, second);
    }

    private static bool IsExactList(object value)
    {
        var type = value.GetType();
        return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>);
    }

    private static bool IsOtherEnumerable(object? value) =>
        value is IEnumerable && value is not string;
}
