using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Extensions;

public static class ICommandBuilderExtensions
{
    public static TICommandBuilder SetExecutableName<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableName)
        where TICommandBuilder : ICommandBuilder
    {
        commandBuilder.ExecutableName = executableName;
        return commandBuilder;
    }

    public static TICommandBuilder SetExecutableTitle<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableTitle)
        where TICommandBuilder : ICommandBuilder
    {
        commandBuilder.ExecutableTitle = executableTitle;
        return commandBuilder;
    }

    public static TICommandBuilder SetExecutableDescription<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableDescription)
        where TICommandBuilder : ICommandBuilder
    {
        commandBuilder.ExecutableDescription = executableDescription;
        return commandBuilder;
    }

    public static TICommandBuilder SetExecutableVersion<TICommandBuilder>(this TICommandBuilder commandBuilder, string executableVersion)
        where TICommandBuilder : ICommandBuilder
    {
        commandBuilder.ExecutableVersion = executableVersion;
        return commandBuilder;
    }

    public static TICommandBuilder SetHelpWidth<TICommandBuilder>(this TICommandBuilder commandBuilder, int helpWidth)
        where TICommandBuilder : ICommandBuilder
    {
        commandBuilder.HelpWidth = helpWidth;
        return commandBuilder;
    }

    public static TICommandBuilder AddCommand<TICommandBuilder>(this TICommandBuilder commandBuilder, ICommand command)
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        ArgumentNullException.ThrowIfNull(command);
        commandBuilder.Commands.Add(command);
        return commandBuilder;
    }

    public static TICommandBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommand, TICommandBuilder>(this TICommandBuilder commandBuilder)
        where TCommand : ICommand
        where TICommandBuilder : ICommandBuilder
    {
        ArgumentNullException.ThrowIfNull(commandBuilder);
        var command = Activator.CreateInstance<TCommand>();
        ArgumentNullException.ThrowIfNull(command);
        commandBuilder.Commands.Add(command);
        return commandBuilder;
    }
}
