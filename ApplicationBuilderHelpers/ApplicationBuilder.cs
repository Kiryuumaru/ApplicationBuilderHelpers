using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Services;
using ApplicationBuilderHelpers.Workers;
using ApplicationBuilderHelpers.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Represents a builder for managing application dependencies and running the configured application.
/// </summary>
public class ApplicationBuilder : ICommandBuilder
{
    string? ICommandBuilder.ExecutableName { get; set; } = null;
    string? ICommandBuilder.ExecutableTitle { get; set; } = null;
    string? ICommandBuilder.ExecutableDescription { get; set; } = null;
    List<ICommand> ICommandBuilder.Commands { get; } = [];
    Dictionary<Type, ICommandTypeParser> ICommandTypeParserCollection.TypeParsers { get; } = [];
    List<IApplicationDependency> IApplicationDependencyCollection.ApplicationDependencies { get; } = [];

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
        try
        {
            // Setup cancellation handling
            var cancellationTokenSource = new CancellationTokenSource();
            Console.CancelKeyPress += (sender, e) =>
            {
                cancellationTokenSource.Cancel();
                e.Cancel = true;
            };

            // Access internal members through interface casting
            var commandBuilder = (this as ICommandBuilder)!;
            var applicationDependencyCollection = (this as IApplicationDependencyCollection)!;
            var typeParserCollection = (this as ICommandTypeParserCollection)!;

            // Run command preparation for all dependencies
            foreach (var dependency in applicationDependencyCollection.ApplicationDependencies)
            {
                dependency.CommandPreparation(this);
            }

            // Find and parse the appropriate command
            ICommand? targetCommand = null;
            var remainingArgs = args.ToList();

            // Handle special cases first
            if (args.Length == 1 && args[0].Equals("--version", StringComparison.InvariantCultureIgnoreCase))
            {
                var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
                var version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
                Console.WriteLine(version);
                return 0;
            }

            if (args.Contains("--help") || args.Contains("-h"))
            {
                ShowHelp(commandBuilder.Commands);
                return 0;
            }

            // Find the appropriate command
            targetCommand = FindMatchingCommand(commandBuilder.Commands, ref remainingArgs);

            if (targetCommand == null)
            {
                // No specific command found, try to find a root command (command with null/empty name)
                targetCommand = commandBuilder.Commands.FirstOrDefault(c => c.GetCommandName() == null || string.IsNullOrEmpty(c.GetCommandName()));
                
                if (targetCommand == null)
                {
                    Console.WriteLine("No command found.");
                    ShowHelp(commandBuilder.Commands);
                    return 1;
                }
            }

            // Parse command options and arguments
            if (!TryParseCommandArguments(targetCommand, remainingArgs, typeParserCollection.TypeParsers))
            {
                return 1;
            }

            // Run command preparation
            targetCommand.CommandPreparationInternal(this);

            // Execute the command
            return await ExecuteCommand(targetCommand, applicationDependencyCollection.ApplicationDependencies, cancellationTokenSource);
        }
        catch (CommandException ex)
        {
            if (!string.IsNullOrEmpty(ex.Message))
            {
                Console.WriteLine(ex.Message);
            }
            return ex.ExitCode;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Unexpected error: {ex.Message}");
            return 1;
        }
    }

    private static ICommand? FindMatchingCommand(List<ICommand> commands, ref List<string> args)
    {
        if (args.Count == 0)
            return null;

        foreach (var command in commands)
        {
            var commandName = command.GetCommandName();
            if (string.IsNullOrEmpty(commandName))
                continue;

            var commandParts = commandName.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (commandParts.Length <= args.Count)
            {
                bool matches = true;
                for (int i = 0; i < commandParts.Length; i++)
                {
                    if (!string.Equals(commandParts[i], args[i], StringComparison.OrdinalIgnoreCase))
                    {
                        matches = false;
                        break;
                    }
                }

                if (matches)
                {
                    // Remove command parts from args
                    args.RemoveRange(0, commandParts.Length);
                    return command;
                }
            }
        }

        return null;
    }

    private static bool TryParseCommandArguments(ICommand command, List<string> args, Dictionary<Type, ICommandTypeParser> typeParsers)
    {
        var properties = GetCommandProperties(command);
        var parsedOptions = new HashSet<string>();
        var argumentValues = new List<string>();

        for (int i = 0; i < args.Count; i++)
        {
            var arg = args[i];

            if (arg.StartsWith("--") || arg.StartsWith("-"))
            {
                // This is an option
                if (!TryParseOption(command, properties, arg, args, ref i, typeParsers, parsedOptions))
                {
                    return false;
                }
            }
            else
            {
                // This is an argument
                argumentValues.Add(arg);
            }
        }

        // Parse positional arguments
        return TryParseArguments(command, properties, argumentValues, typeParsers);
    }

    private static bool TryParseOption(ICommand command, List<PropertyInfo> properties, string optionName, List<string> args, ref int index, Dictionary<Type, ICommandTypeParser> typeParsers, HashSet<string> parsedOptions)
    {
        var optionProperties = properties.Where(p => p.GetCustomAttribute<CommandOptionAttribute>() != null);

        foreach (var property in optionProperties)
        {
            var optionAttr = property.GetCustomAttribute<CommandOptionAttribute>()!;
            
            bool matches = false;
            if (optionAttr.ShortTerm != null && optionName == $"-{optionAttr.ShortTerm}")
                matches = true;
            else if (!string.IsNullOrEmpty(optionAttr.Term) && optionName == $"--{optionAttr.Term}")
                matches = true;

            if (matches)
            {
                var optionKey = $"{optionAttr.ShortTerm}:{optionAttr.Term}";
                if (parsedOptions.Contains(optionKey))
                {
                    Console.WriteLine($"Option {optionName} specified multiple times.");
                    return false;
                }
                parsedOptions.Add(optionKey);

                var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
                
                if (propertyType == typeof(bool))
                {
                    property.SetValue(command, true);
                    return true;
                }

                // Get value for non-boolean options
                if (index + 1 >= args.Count)
                {
                    Console.WriteLine($"Option {optionName} requires a value.");
                    return false;
                }

                var value = args[index + 1];
                index++; // Skip the value in next iteration

                if (typeParsers.TryGetValue(propertyType, out var parser))
                {
                    var parsedValue = parser.Parse([value], out var error);
                    if (error != null)
                    {
                        Console.WriteLine($"Invalid value for option {optionName}: {error}");
                        return false;
                    }
                    property.SetValue(command, parsedValue);
                }
                else
                {
                    Console.WriteLine($"No parser found for option type {propertyType.Name}");
                    return false;
                }

                return true;
            }
        }

        Console.WriteLine($"Unknown option: {optionName}");
        return false;
    }

    private static bool TryParseArguments(ICommand command, List<PropertyInfo> properties, List<string> argumentValues, Dictionary<Type, ICommandTypeParser> typeParsers)
    {
        var argumentProperties = properties
            .Where(p => p.GetCustomAttribute<CommandArgumentAttribute>() != null)
            .OrderBy(p => p.GetCustomAttribute<CommandArgumentAttribute>()!.Position)
            .ToList();

        if (argumentValues.Count != argumentProperties.Count)
        {
            if (argumentValues.Count < argumentProperties.Count)
            {
                Console.WriteLine($"Missing required arguments. Expected {argumentProperties.Count}, got {argumentValues.Count}.");
            }
            else
            {
                Console.WriteLine($"Too many arguments. Expected {argumentProperties.Count}, got {argumentValues.Count}.");
            }
            return false;
        }

        for (int i = 0; i < argumentProperties.Count; i++)
        {
            var property = argumentProperties[i];
            var propertyType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;

            if (typeParsers.TryGetValue(propertyType, out var parser))
            {
                var parsedValue = parser.Parse([argumentValues[i]], out var error);
                if (error != null)
                {
                    var argAttr = property.GetCustomAttribute<CommandArgumentAttribute>()!;
                    var argName = argAttr.Name ?? property.Name;
                    Console.WriteLine($"Invalid value for argument {argName}: {error}");
                    return false;
                }
                property.SetValue(command, parsedValue);
            }
            else
            {
                Console.WriteLine($"No parser found for argument type {propertyType.Name}");
                return false;
            }
        }

        return true;
    }

    private static List<PropertyInfo> GetCommandProperties(ICommand command)
    {
        var properties = new List<PropertyInfo>();
        var currentType = command.GetType();

        while (currentType != null)
        {
            properties.AddRange(currentType.GetProperties(BindingFlags.DeclaredOnly | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance));
            currentType = currentType.BaseType;
        }

        return properties;
    }

    private static void ShowHelp(List<ICommand> commands)
    {
        Console.WriteLine("Available commands:");
        
        foreach (var command in commands)
        {
            var name = command.GetCommandName() ?? "<default>";
            var description = command.GetCommandDescription() ?? "";
            Console.WriteLine($"  {name,-20} {description}");
        }
    }

    private static async Task<int> ExecuteCommand(ICommand command, List<IApplicationDependency> dependencies, CancellationTokenSource cancellationTokenSource)
    {
        try
        {
            var applicationBuilder = await command.ApplicationBuilderInternal(cancellationTokenSource.Token);
            
            // Add command and dependencies
            applicationBuilder.ApplicationDependencies.Add(command);
            foreach (var dependency in dependencies)
            {
                applicationBuilder.ApplicationDependencies.Add(dependency);
            }

            // Setup required services
            var commandInvokerService = new CommandInvokerService();
            var lifetimeGlobalService = new LifetimeGlobalService();
            cancellationTokenSource.Token.Register(lifetimeGlobalService.CancellationTokenSource.Cancel);
            
            applicationBuilder.Services.AddSingleton(commandInvokerService);
            applicationBuilder.Services.AddSingleton(lifetimeGlobalService);
            applicationBuilder.Services.AddScoped<LifetimeService>();
            applicationBuilder.Services.AddHostedService<CommandInvokerWorker>();

            var applicationHost = applicationBuilder.BuildInternal();
            
            CommandException? commandException = null;
            commandInvokerService.SetCommand(async ct =>
            {
                try
                {
                    await command.RunInternal(applicationHost, cancellationTokenSource);
                }
                catch (CommandException ex)
                {
                    commandException = ex;
                }
            });

            await Task.WhenAll(
                Task.Run(async () => await applicationHost.Run(cancellationTokenSource.Token)),
                Task.Run(async () =>
                {
                    // Wait for cancellation
                    try
                    {
                        await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token);
                    }
                    catch (OperationCanceledException)
                    {
                        // Expected when cancellation is requested
                    }
                    await lifetimeGlobalService.InvokeApplicationExitingCallbacksAsync();
                }));

            if (commandException != null)
            {
                throw commandException;
            }

            return 0;
        }
        catch (CommandException)
        {
            throw; // Re-throw command exceptions
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error executing command: {ex.Message}");
            return 1;
        }
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
