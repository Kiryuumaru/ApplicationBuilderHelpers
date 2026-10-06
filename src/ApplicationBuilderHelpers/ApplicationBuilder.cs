using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Models;
using ApplicationBuilderHelpers.ParserTypes;
using ApplicationBuilderHelpers.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Fluent entry point that registers commands, dependencies, and type parsers, then runs the matched command.
/// </summary>
public class ApplicationBuilder : ICommandBuilder
{
    string? ICommandBuilder.ExecutableName { get; set; } = null;
    string? ICommandBuilder.ExecutableTitle { get; set; } = null;
    string? ICommandBuilder.ExecutableDescription { get; set; } = null;
    string? ICommandBuilder.ExecutableVersion { get; set; } = null;
    int? ICommandBuilder.HelpWidth { get; set; } = null;
    int? ICommandBuilder.HelpBorderWidth { get; set; } = null;
    IConsoleTheme? ICommandBuilder.Theme { get; set; } = DefaultConsoleTheme.Instance;
    bool ICommandBuilder.RejectDuplicateOptions { get; set; }
    List<TypedCommandHolder> ICommandBuilder.Commands { get; } = [];
    List<IApplicationDependency> IApplicationDependencyCollection.ApplicationDependencies { get; } = [];
    Dictionary<Type, ICommandTypeParser> ICommandTypeParserCollection.TypeParsers { get; } = [];

    private readonly CommandLineParser.CommandReflectionCache _reflectionCache = new();

    internal int ReflectionBuildCount => _reflectionCache.BuildCount;

    /// <summary>
    /// Installs the given theme type as the help-output theme and returns this builder.
    /// </summary>
    /// <typeparam name="TConsoleTheme">The theme type to instantiate.</typeparam>
    /// <returns>This builder.</returns>
    public ApplicationBuilder SetTheme<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TConsoleTheme>()
        where TConsoleTheme : IConsoleTheme
        => ICommandBuilderExtensions.SetTheme<TConsoleTheme, ApplicationBuilder>(this);

    /// <summary>
    /// Installs the supplied theme instance as the help-output theme and returns this builder.
    /// </summary>
    /// <typeparam name="TConsoleTheme">The theme instance type.</typeparam>
    /// <param name="consoleTheme">The theme instance to install.</param>
    /// <returns>This builder.</returns>
    public ApplicationBuilder SetTheme<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TConsoleTheme>(TConsoleTheme consoleTheme)
        where TConsoleTheme : IConsoleTheme
        => ICommandBuilderExtensions.SetTheme(this, consoleTheme);

    /// <summary>
    /// Registers a command type so its name routes to a fresh per-run instance, and returns this builder.
    /// </summary>
    /// <typeparam name="TCommand">The command type to construct per run.</typeparam>
    /// <returns>This builder.</returns>
    public ApplicationBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand>()
        where TCommand : ICommand
        => ICommandBuilderExtensions.AddCommand<TCommand, ApplicationBuilder>(this);

    /// <summary>
    /// Registers a dependency module appended to the host pipeline, and returns this builder.
    /// </summary>
    /// <typeparam name="TApplicationDependency">The dependency type to construct.</typeparam>
    /// <returns>This builder.</returns>
    public ApplicationBuilder AddApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency>()
        where TApplicationDependency : IApplicationDependency
        => IApplicationDependencyCollectionExtensions.AddApplication<TApplicationDependency, ApplicationBuilder>(this);

    /// <summary>
    /// Registers a custom value parser keyed by its handled type, and returns this builder.
    /// </summary>
    /// <typeparam name="TCommandTypeParser">The parser type to construct.</typeparam>
    /// <returns>This builder.</returns>
    public ApplicationBuilder AddCommandTypeParser<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommandTypeParser>()
        where TCommandTypeParser : ICommandTypeParser
        => ICommandTypeParserCollectionExtensions.AddCommandTypeParser<TCommandTypeParser, ApplicationBuilder>(this);

    /// <summary>
    /// Parses the given arguments and runs the matched command to an exit code.
    /// </summary>
    /// <param name="args">The command-line arguments to parse.</param>
    /// <param name="cancellationToken">Token requesting cancellation.</param>
    /// <returns>Exit code: 0 success/help/version/completion, 2 usage error, 130 cancellation, 1 or custom fault.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args, nameof(args));
        var commandLineParser = new CommandLineParser.CommandLineParser(this, _reflectionCache);
        return await commandLineParser.RunAsync(args, cancellationToken);
    }

    /// <summary>
    /// Creates an empty builder with the built-in parsers pre-registered.
    /// </summary>
    /// <returns>A new builder instance.</returns>
    public static ApplicationBuilder Create()
    {
        return new ApplicationBuilder();
    }

    private ApplicationBuilder()
    {
        AddCommandTypeParser<AbsolutePathTypeParser>();
        AddCommandTypeParser<BoolTypeParser>();
        AddCommandTypeParser<ByteTypeParser>();
        AddCommandTypeParser<CharTypeParser>();
        AddCommandTypeParser<DateOnlyTypeParser>();
        AddCommandTypeParser<DateTimeTypeParser>();
        AddCommandTypeParser<DateTimeOffsetTypeParser>();
        AddCommandTypeParser<DecimalTypeParser>();
        AddCommandTypeParser<DoubleTypeParser>();
        AddCommandTypeParser<FileInfoTypeParser>();
        AddCommandTypeParser<FloatTypeParser>();
        AddCommandTypeParser<GuidTypeParser>();
        AddCommandTypeParser<IntTypeParser>();
        AddCommandTypeParser<LongTypeParser>();
        AddCommandTypeParser<SByteTypeParser>();
        AddCommandTypeParser<ShortTypeParser>();
        AddCommandTypeParser<StringTypeParser>();
        AddCommandTypeParser<TimeOnlyTypeParser>();
        AddCommandTypeParser<TimeSpanTypeParser>();
        AddCommandTypeParser<UIntTypeParser>();
        AddCommandTypeParser<ULongTypeParser>();
        AddCommandTypeParser<UriTypeParser>();
        AddCommandTypeParser<UShortTypeParser>();
        AddCommandTypeParser<VersionTypeParser>();
    }
}
