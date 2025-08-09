using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary>
/// Provides extension methods for <see cref="ICommandBuilder"/> implementations to enable fluent configuration of command builders.
/// </summary>
/// <remarks>
/// This static class contains extension methods that allow for method chaining when configuring command builders.
/// These methods provide a fluent API for setting executable properties, help formatting options, themes, and adding commands.
/// </remarks>
public static class ICommandBuilderExtensions
{
    /// <summary>
    /// Sets the executable name for the command builder.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="executableName">The name of the executable.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> or <paramref name="executableName"/> is null.</exception>
    public static TICommandBuilder SetExecutableName<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableName)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(executableName);
        
        commandBuilder.ExecutableName = executableName;
        return commandBuilder;
    }

    /// <summary>
    /// Sets the executable title for the command builder.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="executableTitle">The title of the executable.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> or <paramref name="executableTitle"/> is null.</exception>
    public static TICommandBuilder SetExecutableTitle<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableTitle)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(executableTitle);
        
        commandBuilder.ExecutableTitle = executableTitle;
        return commandBuilder;
    }

    /// <summary>
    /// Sets the executable description for the command builder.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="executableDescription">The description of the executable.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> or <paramref name="executableDescription"/> is null.</exception>
    public static TICommandBuilder SetExecutableDescription<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableDescription)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(executableDescription);
        
        commandBuilder.ExecutableDescription = executableDescription;
        return commandBuilder;
    }

    /// <summary>
    /// Sets the executable version for the command builder.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="executableVersion">The version of the executable.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> or <paramref name="executableVersion"/> is null.</exception>
    public static TICommandBuilder SetExecutableVersion<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableVersion)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(executableVersion);
        
        commandBuilder.ExecutableVersion = executableVersion;
        return commandBuilder;
    }

    /// <summary>
    /// Sets the help width for the command builder's help output formatting.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="helpWidth">The width for help output formatting. Must be non-negative.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="helpWidth"/> is negative.</exception>
    public static TICommandBuilder SetHelpWidth<TICommandBuilder>(this TICommandBuilder commandBuilder, int helpWidth)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        if (helpWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(helpWidth), "Help width must be non-negative.");

        commandBuilder.HelpWidth = helpWidth;
        return commandBuilder;
    }

    /// <summary>
    /// Sets the help border width for the command builder's help output formatting.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="helpBorderWidth">The border width for help output formatting. Must be non-negative.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="helpBorderWidth"/> is negative.</exception>
    public static TICommandBuilder SetHelpBorderWidth<TICommandBuilder>(this TICommandBuilder commandBuilder, int helpBorderWidth)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        if (helpBorderWidth < 0)
            throw new ArgumentOutOfRangeException(nameof(helpBorderWidth), "Help border width must be non-negative.");

        commandBuilder.HelpBorderWidth = helpBorderWidth;
        return commandBuilder;
    }

    /// <summary>
    /// Sets the console theme for CLI help output using the provided theme instance.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="theme">The console theme instance to use for CLI help output.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> or <paramref name="theme"/> is null.</exception>
    public static TICommandBuilder SetTheme<TICommandBuilder>(this TICommandBuilder commandBuilder, IConsoleTheme theme)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(theme);
        
        commandBuilder.Theme = theme;
        return commandBuilder;
    }

    /// <summary>
    /// Sets the console theme for CLI help output using the specified theme type.
    /// </summary>
    /// <typeparam name="TConsoleTheme">The type of console theme that implements <see cref="IConsoleTheme"/>.</typeparam>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> is null.</exception>
    public static TICommandBuilder SetTheme<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TConsoleTheme, TICommandBuilder>(this TICommandBuilder commandBuilder)
        where TConsoleTheme : IConsoleTheme
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        var theme = Activator.CreateInstance<TConsoleTheme>();
        ArgumentNullException.ThrowIfNull(theme);
        
        commandBuilder.Theme = theme;
        return commandBuilder;
    }

    /// <summary>
    /// Adds a command instance to the command builder.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="command">The command instance to add.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> or <paramref name="command"/> is null.</exception>
    public static TICommandBuilder AddCommand<TICommandBuilder>(this TICommandBuilder commandBuilder, ICommand command)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(command);
        commandBuilder.Commands.Add(new Models.TypedCommandHolder(command.GetType(), command));
        return commandBuilder;
    }

    /// <summary>
    /// Adds a command of the specified type to the command builder.
    /// </summary>
    /// <typeparam name="TCommand">The type of command that implements <see cref="ICommand"/>.</typeparam>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <returns>The command builder instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> is null.</exception>
    public static TICommandBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand, TICommandBuilder>(this TICommandBuilder commandBuilder)
        where TCommand : ICommand
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        var command = Activator.CreateInstance<TCommand>();
        ArgumentNullException.ThrowIfNull(command);
        commandBuilder.Commands.Add(new Models.TypedCommandHolder(typeof(TCommand), command));
        return commandBuilder;
    }
}
