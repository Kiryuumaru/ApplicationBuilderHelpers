using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Extensions;

public static class ICommandTypeParserCollectionExtensions
{
    public static TICommandTypeParserCollection AddCommandTypeParser<TICommandTypeParserCollection>(this TICommandTypeParserCollection commandTypeParserCollection, ICommandTypeParser commandTypeParser)
        where TICommandTypeParserCollection : ICommandTypeParserCollection
    {
        ArgumentNullException.ThrowIfNull(commandTypeParserCollection);
        ArgumentNullException.ThrowIfNull(commandTypeParser);
        commandTypeParserCollection.TypeParsers.Add(commandTypeParser.Type, commandTypeParser);
        return commandTypeParserCollection;
    }

    public static TICommandTypeParserCollection AddCommandTypeParser<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommandTypeParser, TICommandTypeParserCollection>(this TICommandTypeParserCollection commandTypeParserCollection)
        where TCommandTypeParser : ICommandTypeParser
        where TICommandTypeParserCollection : ICommandTypeParserCollection
    {
        ArgumentNullException.ThrowIfNull(commandTypeParserCollection);
        var commandTypeParser = Activator.CreateInstance<TCommandTypeParser>();
        ArgumentNullException.ThrowIfNull(commandTypeParser);
        commandTypeParserCollection.TypeParsers.Add(commandTypeParser.Type, commandTypeParser);
        return commandTypeParserCollection;
    }
}
