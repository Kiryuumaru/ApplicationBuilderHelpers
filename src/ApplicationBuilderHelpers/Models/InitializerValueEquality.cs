using System;

namespace ApplicationBuilderHelpers.Models;

/// <summary>
/// Shared initializer-default value semantics: element-wise array equality and
/// defensive array cloning so later replacement cannot alias a stored copy.
/// Array-only coverage: other collections compare and snapshot by reference,
/// so unanimous/promotion agreement for non-array collections fails toward omission (fail-safe).
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

        return Equals(first, second);
    }
}
