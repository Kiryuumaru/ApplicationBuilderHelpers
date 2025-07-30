using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.ParserTypes;
using ApplicationBuilderHelpers.Services;
using ApplicationBuilderHelpers.Workers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Represents a builder for managing application dependencies and running the configured application.
/// </summary>
public class ApplicationBuilder : ICommandBuilder
{
    string? ICommandBuilder.ExecutableName { get; set; } = null;
    string? ICommandBuilder.ExecutableTitle { get; set; } = null;
    string? ICommandBuilder.ExecutableDescription { get; set; } = null;
    string? ICommandBuilder.ExecutableVersion { get; set; } = null;
    int? ICommandBuilder.HelpWidth { get; set; } = null;
    List<ICommand> ICommandBuilder.Commands { get; } = [];
    List<IApplicationDependency> IApplicationDependencyCollection.ApplicationDependencies { get; } = [];
    Dictionary<Type, ICommandTypeParser> ICommandTypeParserCollection.TypeParsers { get; } = new()
    {
        [typeof(bool)] = new BoolTypeParser(),
        [typeof(int)] = new IntTypeParser(),
        [typeof(string)] = new StringTypeParser(),
    };

    public ApplicationBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommand>()
        where TCommand : ICommand
        => ICommandBuilderExtensions.AddCommand<TCommand, ApplicationBuilder>(this);

    public ApplicationBuilder AddApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency>()
        where TApplicationDependency : IApplicationDependency
        => IApplicationDependencyCollectionExtensions.AddApplication<TApplicationDependency, ApplicationBuilder>(this);

    public ApplicationBuilder AddCommandTypeParser<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommandTypeParser>()
        where TCommandTypeParser : ICommandTypeParser
        => ICommandTypeParserCollectionExtensions.AddCommandTypeParser<TCommandTypeParser, ApplicationBuilder>(this);
    
    public async Task<int> RunAsync(string[] args)
    {
        return 0; // Placeholder for actual implementation
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ApplicationBuilder"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="ApplicationBuilder"/> class.</returns>
    public static ApplicationBuilder Create()
    {
        return new ApplicationBuilder();
    }
    private ApplicationBuilder() { }
}
