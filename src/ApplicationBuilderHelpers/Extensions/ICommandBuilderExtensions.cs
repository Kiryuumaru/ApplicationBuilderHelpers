using ApplicationBuilderHelpers.CommandLineParser;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary>Fluent setters for <see cref="ICommandBuilder"/> implementations.</summary>
public static class ICommandBuilderExtensions
{
    /// <summary>Stores the executable name shown in help and error footers.</summary>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="executableName">The executable name; overrides auto-detection.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static TICommandBuilder SetExecutableName<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableName)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(executableName);

        commandBuilder.ExecutableName = executableName;
        return commandBuilder;
    }

    /// <summary>Stores the executable title shown in help headers.</summary>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="executableTitle">The title; overrides auto-detection.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static TICommandBuilder SetExecutableTitle<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableTitle)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(executableTitle);

        commandBuilder.ExecutableTitle = executableTitle;
        return commandBuilder;
    }

    /// <summary>Stores the executable description shown in help.</summary>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="executableDescription">The description; overrides auto-detection.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static TICommandBuilder SetExecutableDescription<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableDescription)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(executableDescription);

        commandBuilder.ExecutableDescription = executableDescription;
        return commandBuilder;
    }

    /// <summary>Stores the executable version shown by <c>--version</c>.</summary>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="executableVersion">The version; overrides auto-detection.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static TICommandBuilder SetExecutableVersion<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableVersion)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(executableVersion);

        commandBuilder.ExecutableVersion = executableVersion;
        return commandBuilder;
    }

    /// <summary>Stores the help width; unset renders at the <see cref="HelpWidths.Default"/> default, values are floored to <see cref="HelpWidths.Minimum"/> and capped at <see cref="HelpWidths.Maximum"/>.</summary>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="helpWidth">The width; must be within the range accepted by <see cref="HelpWidths"/>.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="helpWidth"/> is outside the range accepted by <see cref="HelpWidths"/>.</exception>
    public static TICommandBuilder SetHelpWidth<TICommandBuilder>(this TICommandBuilder commandBuilder, int helpWidth)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        HelpWidths.ThrowIfOutOfRange(helpWidth);

        commandBuilder.HelpWidth = helpWidth;
        return commandBuilder;
    }

    /// <summary>Stores the help border width; zero removes the padding.</summary>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="helpBorderWidth">The border width; must be non-negative.</param>
    /// <returns>The command builder instance.</returns>
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

    /// <summary>Stores the theme instance used for CLI help output.</summary>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="theme">The console theme instance.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static TICommandBuilder SetTheme<TICommandBuilder>(this TICommandBuilder commandBuilder, IConsoleTheme theme)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(theme);

        commandBuilder.Theme = theme;
        return commandBuilder;
    }

    /// <summary>Constructs the theme type and stores it for CLI help output.</summary>
    /// <typeparam name="TConsoleTheme">The console theme type.</typeparam>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <returns>The command builder instance.</returns>
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
    /// Sets whether repeated scalar valued options are rejected as duplicates.
    /// </summary>
    /// <typeparam name="TICommandBuilder">The type of command builder that implements <see cref="ICommandBuilder"/>.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="rejectDuplicateOptions">True to reject repeated scalar valued options.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> is null.</exception>
    public static TICommandBuilder SetRejectDuplicateOptions<TICommandBuilder>(this TICommandBuilder commandBuilder, bool rejectDuplicateOptions)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);

        commandBuilder.RejectDuplicateOptions = rejectDuplicateOptions;
        return commandBuilder;
    }

    /// <summary>Appends a caller-supplied command instance; it keeps identity across runs.</summary>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <param name="command">The command instance.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when an argument is null.</exception>
    public static TICommandBuilder AddCommand<TICommandBuilder>(this TICommandBuilder commandBuilder, ICommand command)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(command);
        commandBuilder.Commands.Add(new Models.TypedCommandHolder(command.GetType(), command, isInstanceRegistration: true));
        return commandBuilder;
    }

    /// <summary>Constructs a command of the given type and appends it; each run gets a fresh instance.</summary>
    /// <typeparam name="TCommand">The command type.</typeparam>
    /// <typeparam name="TICommandBuilder">The command builder type.</typeparam>
    /// <param name="commandBuilder">The command builder instance.</param>
    /// <returns>The command builder instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandBuilder"/> is null.</exception>
    public static TICommandBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TCommand, TICommandBuilder>(this TICommandBuilder commandBuilder)
        where TCommand : ICommand
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        var command = Activator.CreateInstance<TCommand>();
        ArgumentNullException.ThrowIfNull(command);
        commandBuilder.Commands.Add(new Models.TypedCommandHolder(typeof(TCommand), command, isInstanceRegistration: false));
        return commandBuilder;
    }
}
