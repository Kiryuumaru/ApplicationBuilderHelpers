// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;

namespace RestfulHelpers;

/// <summary>
/// Internal polyfill for <see cref="System.ArgumentException"/>.
/// </summary>
internal class ArgumentException
{
    /// <summary>
    /// Throws an <see cref="System.ArgumentException"/> if <paramref name="argument"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="argument">The reference type argument to validate as non-<see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEmpty([NotNull] string argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (string.IsNullOrEmpty(argument))
        {
            Throw($"\"{paramName}\" is empty.");
        }
    }

    /// <summary>
    /// Throws an <see cref="System.ArgumentException"/> if <paramref name="argument"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="argument">The reference type argument to validate as non-<see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfEmpty<T>([NotNull] IEnumerable<T> argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument.Count() == 0)
        {
            Throw($"\"{paramName}\" is empty.");
        }
    }

    /// <summary>
    /// Throws an <see cref="System.ArgumentException"/> if <paramref name="argument"/> is <see langword="null"/>.
    /// </summary>
    /// <param name="argument">The reference type argument to validate as non-<see langword="null"/>.</param>
    /// <param name="paramName">The name of the parameter with which <paramref name="argument"/> corresponds.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void ThrowIfHasNullOrEmpty<T>([NotNull] IEnumerable<T> argument, [CallerArgumentExpression("argument")] string? paramName = null)
    {
        if (argument.Count() == 0)
        {
            Throw($"\"{paramName}\" is empty.");
        }

        foreach (var val in argument)
        {
            if (val == null)
            {
                Throw($"\"{paramName}\" is has null element.");
            }
            if (val is string strVal && string.IsNullOrEmpty(strVal))
            {
                Throw($"\"{paramName}\" is has empty element.");
            }
        }
    }

    /// <summary>
    /// Throws an <see cref="System.ArgumentException"/>.
    /// </summary>
    /// <param name="message">The message of the exception.</param>
    [DoesNotReturn]
    public static void Throw(string message)
    {
        throw new System.ArgumentException(message);
    }
}
