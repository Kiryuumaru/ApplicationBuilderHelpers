using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Extensions;

public static class ICommandExtensions
{
    internal static string? GetCommandName(this ICommand command)
    {
        var commandAttribute = command.GetType().GetCustomAttribute<CommandAttribute>();
        return commandAttribute?.Name;
    }

    internal static string? GetCommandDescription(this ICommand command)
    {
        var commandAttribute = command.GetType().GetCustomAttribute<CommandAttribute>();
        return commandAttribute?.Description;
    }
}
