using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.ParserTypes;
using ApplicationBuilderHelpers.Themes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    int? ICommandBuilder.HelpBorderWidth { get; set; } = null;
    IConsoleTheme? ICommandBuilder.Theme { get; set; } = DefaultConsoleTheme.Instance;
    List<ICommand> ICommandBuilder.Commands { get; } = [];
    List<IApplicationDependency> IApplicationDependencyCollection.ApplicationDependencies { get; } = [];
    Dictionary<Type, ICommandTypeParser> ICommandTypeParserCollection.TypeParsers { get; } = [];

    /// <summary>
    /// Sets the console theme for CLI help output using the specified theme type.
    /// </summary>
    /// <typeparam name="TConsoleTheme">The type of console theme that implements <see cref="IConsoleTheme"/>.</typeparam>
    /// <returns>The current <see cref="ApplicationBuilder"/> instance for method chaining.</returns>
    public ApplicationBuilder SetTheme<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TConsoleTheme>()
        where TConsoleTheme : IConsoleTheme
        => ICommandBuilderExtensions.SetTheme<TConsoleTheme, ApplicationBuilder>(this);

    /// <summary>
    /// Sets the console theme for CLI help output using the provided theme instance.
    /// </summary>
    /// <typeparam name="TConsoleTheme">The type of console theme that implements <see cref="IConsoleTheme"/>.</typeparam>
    /// <param name="consoleTheme">The console theme instance to use for CLI help output.</param>
    /// <returns>The current <see cref="ApplicationBuilder"/> instance for method chaining.</returns>
    public ApplicationBuilder SetTheme<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TConsoleTheme>(TConsoleTheme consoleTheme)
        where TConsoleTheme : IConsoleTheme
        => ICommandBuilderExtensions.SetTheme(this, consoleTheme);

    /// <summary>
    /// Adds a command of the specified type to the application builder.
    /// </summary>
    /// <typeparam name="TCommand">The type of command that implements <see cref="ICommand"/>.</typeparam>
    /// <returns>The current <see cref="ApplicationBuilder"/> instance for method chaining.</returns>
    public ApplicationBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommand>()
        where TCommand : ICommand
        => ICommandBuilderExtensions.AddCommand<TCommand, ApplicationBuilder>(this);

    /// <summary>
    /// Adds an application dependency of the specified type to the application builder.
    /// </summary>
    /// <typeparam name="TApplicationDependency">The type of application dependency that implements <see cref="IApplicationDependency"/>.</typeparam>
    /// <returns>The current <see cref="ApplicationBuilder"/> instance for method chaining.</returns>
    public ApplicationBuilder AddApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency>()
        where TApplicationDependency : IApplicationDependency
        => IApplicationDependencyCollectionExtensions.AddApplication<TApplicationDependency, ApplicationBuilder>(this);

    /// <summary>
    /// Adds a command type parser of the specified type to the application builder for parsing command-line arguments.
    /// </summary>
    /// <typeparam name="TCommandTypeParser">The type of command type parser that implements <see cref="ICommandTypeParser"/>.</typeparam>
    /// <returns>The current <see cref="ApplicationBuilder"/> instance for method chaining.</returns>
    public ApplicationBuilder AddCommandTypeParser<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommandTypeParser>()
        where TCommandTypeParser : ICommandTypeParser
        => ICommandTypeParserCollectionExtensions.AddCommandTypeParser<TCommandTypeParser, ApplicationBuilder>(this);

    /// <summary>
    /// Asynchronously runs the configured application by parsing command-line arguments and executing the appropriate command.
    /// </summary>
    /// <param name="args">The command-line arguments passed to the application. These arguments are parsed to determine which command to execute and what parameters to pass to it.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to request cancellation of the operation. When cancellation is requested, the method will attempt to gracefully stop the running command.</param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains an integer exit code:
    /// <list type="bullet">
    /// <item><description>0 - Success: The command executed successfully</description></item>
    /// <item><description>Non-zero - Error: An error occurred during command execution or argument parsing</description></item>
    /// </list>
    /// </returns>
    /// <remarks>
    /// This method serves as the main entry point for running the configured application. It:
    /// <list type="number">
    /// <item><description>Creates a command-line parser with the current application builder configuration</description></item>
    /// <item><description>Parses the provided command-line arguments to identify the target command</description></item>
    /// <item><description>Validates command arguments and options</description></item>
    /// <item><description>Executes the identified command with the parsed parameters</description></item>
    /// <item><description>Returns an appropriate exit code based on the execution result</description></item>
    /// </list>
    /// The method respects the cancellation token and will attempt to gracefully terminate execution when cancellation is requested.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="args"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when no commands have been configured or when command parsing fails due to invalid configuration.</exception>
    public async Task<int> RunAsync(string[] args, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(args, nameof(args));
        var commandLineParser = new CommandLineParser.CommandLineParser(this);
        return await commandLineParser.RunAsync(args, cancellationToken);
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ApplicationBuilder"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="ApplicationBuilder"/> class.</returns>
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
        AddCommandTypeParser<DateTimeOffsetTypeParser>();
        AddCommandTypeParser<DateTimeTypeParser>();
        AddCommandTypeParser<DecimalTypeParser>();
        AddCommandTypeParser<DoubleTypeParser>();
        AddCommandTypeParser<FloatTypeParser>();
        AddCommandTypeParser<IntTypeParser>();
        AddCommandTypeParser<LongTypeParser>();
        AddCommandTypeParser<SByteTypeParser>();
        AddCommandTypeParser<ShortTypeParser>();
        AddCommandTypeParser<StringTypeParser>();
        AddCommandTypeParser<UIntTypeParser>();
        AddCommandTypeParser<ULongTypeParser>();
        AddCommandTypeParser<UShortTypeParser>();
    }
}
