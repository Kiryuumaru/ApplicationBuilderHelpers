using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary>Parser registration for <see cref="ICommandTypeParserCollection"/>.</summary>
public static class ICommandTypeParserCollectionExtensions
{
    /// <summary>Registers <paramref name="commandTypeParser"/>, replacing any parser for the same type.</summary>
    /// <typeparam name="TICommandTypeParserCollection">The collection type.</typeparam>
    /// <param name="commandTypeParserCollection">The collection.</param>
    /// <param name="commandTypeParser">The parser.</param>
    /// <returns>The collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static TICommandTypeParserCollection AddCommandTypeParser<TICommandTypeParserCollection>(this TICommandTypeParserCollection commandTypeParserCollection, ICommandTypeParser commandTypeParser)
        where TICommandTypeParserCollection : ICommandTypeParserCollection
    {
        ArgumentNullException.ThrowIfNull(commandTypeParserCollection);
        ArgumentNullException.ThrowIfNull(commandTypeParser);
        commandTypeParserCollection.TypeParsers[commandTypeParser.Type] = commandTypeParser;
        return commandTypeParserCollection;
    }

    /// <summary>Constructs the parser type and registers it, replacing any parser for the same type.</summary>
    /// <typeparam name="TCommandTypeParser">The parser type.</typeparam>
    /// <typeparam name="TICommandTypeParserCollection">The collection type.</typeparam>
    /// <param name="commandTypeParserCollection">The collection.</param>
    /// <returns>The collection.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the collection or created instance is null.</exception>
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
