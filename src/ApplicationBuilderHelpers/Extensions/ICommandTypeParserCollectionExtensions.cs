using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ICommandTypeParserCollection"/> to simplify adding command type parsers.
/// </summary>
public static class ICommandTypeParserCollectionExtensions
{
    /// <summary>
    /// Adds a command type parser to the collection.
    /// Replaces (upserts) any parser already registered for the same <see cref="ICommandTypeParser.Type"/>,
    /// so user-registered parsers override the built-in defaults.
    /// </summary>
    /// <typeparam name="TICommandTypeParserCollection">The type of the command type parser collection.</typeparam>
    /// <param name="commandTypeParserCollection">The collection to add the parser to.</param>
    /// <param name="commandTypeParser">The command type parser to add.</param>
    /// <returns>The updated command type parser collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandTypeParserCollection"/> or <paramref name="commandTypeParser"/> is null.</exception>
    public static TICommandTypeParserCollection AddCommandTypeParser<TICommandTypeParserCollection>(this TICommandTypeParserCollection commandTypeParserCollection, ICommandTypeParser commandTypeParser)
        where TICommandTypeParserCollection : ICommandTypeParserCollection
    {
        ArgumentNullException.ThrowIfNull(commandTypeParserCollection);
        ArgumentNullException.ThrowIfNull(commandTypeParser);
        commandTypeParserCollection.TypeParsers[commandTypeParser.Type] = commandTypeParser;
        return commandTypeParserCollection;
    }

    /// <summary>
    /// Adds a command type parser to the collection by creating an instance of the specified parser type.
    /// Replaces (upserts) any parser already registered for the same <see cref="ICommandTypeParser.Type"/>,
    /// so user-registered parsers override the built-in defaults.
    /// </summary>
    /// <typeparam name="TCommandTypeParser">The type of the command type parser to create and add.</typeparam>
    /// <typeparam name="TICommandTypeParserCollection">The type of the command type parser collection.</typeparam>
    /// <param name="commandTypeParserCollection">The collection to add the parser to.</param>
    /// <returns>The updated command type parser collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandTypeParserCollection"/> is null or when the created parser instance is null.</exception>
    public static TICommandTypeParserCollection AddCommandTypeParser<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommandTypeParser, TICommandTypeParserCollection>(this TICommandTypeParserCollection commandTypeParserCollection)
        where TCommandTypeParser : ICommandTypeParser
        where TICommandTypeParserCollection : ICommandTypeParserCollection
    {
        ArgumentNullException.ThrowIfNull(commandTypeParserCollection);
        var commandTypeParser = Activator.CreateInstance<TCommandTypeParser>();
        ArgumentNullException.ThrowIfNull(commandTypeParser);
        commandTypeParserCollection.TypeParsers[commandTypeParser.Type] = commandTypeParser;
        return commandTypeParserCollection;
    }
}
