﻿using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using ApplicationBuilderHelpers.Interfaces;
using ApplicationBuilderHelpers.ParserTypes;
using ApplicationBuilderHelpers.ParserTypes.Enumerables;
using ApplicationBuilderHelpers.Services;
using ApplicationBuilderHelpers.Workers;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.CommandLine.Builder;
using System.CommandLine.Help;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Represents a builder for managing application dependencies and running the configured application.
/// </summary>
public class ApplicationBuilder
{
    private class ApplicationCommandHierarchy(string? name, CommandLineBuilder commandLineBuilder)
    {
        public string? Name { get; set; } = name;

        public CommandLineBuilder CommandLineBuilder { get; set; } = commandLineBuilder;

        public ApplicationCommand? AppCommand { get; set; } = null;

        public Dictionary<string, ApplicationCommandHierarchy> SubCommands { get; set; } = [];
    }

    /// <summary>
    /// Creates a new instance of the <see cref="ApplicationBuilder"/> class.
    /// </summary>
    /// <returns>A new instance of the <see cref="ApplicationBuilder"/> class.</returns>
    public static ApplicationBuilder Create()
    {
        return new ApplicationBuilder();
    }

    private readonly List<ApplicationDependency> _applicationDependencies = [];
    private readonly List<ApplicationCommand> _commands = [];
    private readonly Dictionary<Type, ICommandLineTypeParser> _typeParsers = new()
    {
        [typeof(AbsolutePath)] = new AbsolutePathTypeParser(),
        [typeof(bool)] = new BoolTypeParser(),
        [typeof(byte)] = new ByteTypeParser(),
        [typeof(char)] = new CharTypeParser(),
        [typeof(DateTimeOffset)] = new DateTimeOffsetTypeParser(),
        [typeof(DateTime)] = new DateTimeTypeParser(),
        [typeof(decimal)] = new DecimalTypeParser(),
        [typeof(double)] = new DoubleTypeParser(),
        [typeof(float)] = new FloatTypeParser(),
        [typeof(int)] = new IntTypeParser(),
        [typeof(long)] = new LongTypeParser(),
        [typeof(sbyte)] = new SByteTypeParser(),
        [typeof(short)] = new ShortTypeParser(),
        [typeof(string)] = new StringTypeParser(),
        [typeof(uint)] = new UIntTypeParser(),
        [typeof(ulong)] = new ULongTypeParser(),
        [typeof(ushort)] = new UShortTypeParser(),
    };

    private string? _executableName = null;

    private ApplicationBuilder() { }

    /// <summary>
    /// Sets the executable name for the application.
    /// </summary>
    /// <param name="name">The name of the executable.</param>
    /// <returns>The current instance of the <see cref="ApplicationBuilder"/> class.</returns>
    public ApplicationBuilder SetExecutableName(string name)
    {
        _executableName = name;
        return this;
    }

    /// <summary>
    /// Adds a command to the application.
    /// </summary>
    /// <typeparam name="TApplicationCommand">The type of the application command.</typeparam>
    /// <param name="applicationCommand">The application command instance.</param>
    /// <returns>The current instance of the <see cref="ApplicationBuilder"/> class.</returns>
    public ApplicationBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TApplicationCommand>(TApplicationCommand applicationCommand)
        where TApplicationCommand : ApplicationCommand
    {
        _commands.Add(applicationCommand);
        return this;
    }

    /// <summary>
    /// Adds a command to the application.
    /// </summary>
    /// <typeparam name="TApplicationCommand">The type of the application command.</typeparam>
    /// <returns>The current instance of the <see cref="ApplicationBuilder"/> class.</returns>
    public ApplicationBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TApplicationCommand>()
        where TApplicationCommand : ApplicationCommand
    {
        return AddCommand(applicationCommand: Activator.CreateInstance<TApplicationCommand>());
    }

    /// <summary>
    /// Adds an application dependency to the application.
    /// </summary>
    /// <param name="applicationDependency">The application dependency instance.</param>
    /// <returns>The current instance of the <see cref="ApplicationBuilder"/> class.</returns>
    public ApplicationBuilder AddApplication(ApplicationDependency applicationDependency)
    {
        _applicationDependencies.Add(applicationDependency);
        return this;
    }

    /// <summary>
    /// Adds an application dependency to the application.
    /// </summary>
    /// <typeparam name="TApplicationDependency">The type of the application dependency.</typeparam>
    /// <returns>The current instance of the <see cref="ApplicationBuilder"/> class.</returns>
    public ApplicationBuilder AddApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency>()
        where TApplicationDependency : ApplicationDependency
    {
        _applicationDependencies.Add(Activator.CreateInstance<TApplicationDependency>());
        return this;
    }

    /// <summary>
    /// Adds a command line type parser to the application.
    /// </summary>
    /// <typeparam name="TCommandLineTypeParser">The type of the command line type parser.</typeparam>
    /// <returns>The current instance of the <see cref="ApplicationBuilder"/> class.</returns>
    public ApplicationBuilder AddCommandLineType<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TCommandLineTypeParser>()
        where TCommandLineTypeParser : ICommandLineTypeParser
    {
        TCommandLineTypeParser commandLineTypeParser = Activator.CreateInstance<TCommandLineTypeParser>();
        _typeParsers[commandLineTypeParser.Type] = commandLineTypeParser;
        return this;
    }

    /// <summary>
    /// Runs the application asynchronously.
    /// </summary>
    /// <param name="args">The command-line arguments.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the exit code of the application.</returns>
    public async Task<int> RunAsync(string[] args)
    {
        CancellationTokenSource cancellationTokenSource = new();
        Console.CancelKeyPress += (sender, e) =>
        {
            cancellationTokenSource.Cancel();
        };
        bool hasRootCommand = false;
        var commandLineBuilder = new CommandLineBuilder(new RootCommand())
            .UseDefaults();
        ApplicationCommandHierarchy rootHierarchy = new(null, commandLineBuilder);
        if (_executableName != null && !string.IsNullOrEmpty(_executableName))
        {
            rootHierarchy.CommandLineBuilder.Command.Name = _executableName;
        }
        foreach (var command in _commands)
        {
            ApplicationCommandHierarchy? currentHier = rootHierarchy;
            if (command.Name == null || string.IsNullOrEmpty(command.Name))
            {
                if (hasRootCommand)
                {
                    throw new Exception("Cannot have more than one root command.");
                }
                hasRootCommand = true;
            }
            else
            {
                foreach (var splitCommand in command.Name.Split(' '))
                {
                    if (!currentHier.SubCommands.TryGetValue(splitCommand, out var subHier))
                    {
                        Command childCommand = new(splitCommand, command.Description);
                        subHier = new(splitCommand, new CommandLineBuilder(childCommand).UseDefaults());
                        currentHier.CommandLineBuilder.Command.AddCommand(childCommand);
                        currentHier.SubCommands.Add(splitCommand, subHier);
                    }
                    currentHier = subHier;
                }
            }
            currentHier.AppCommand = command;
            WireHandler(currentHier, currentHier.AppCommand, cancellationTokenSource.Token);
        }

        return await rootHierarchy.CommandLineBuilder.Build().InvokeAsync(args);
    }

    private void WireHandler(ApplicationCommandHierarchy hier, ApplicationCommand applicationCommand, CancellationToken stoppingToken)
    {
        PropertyInfo[] properties = applicationCommand.GetType().GetProperties();

        List<Action<InvocationContext>> valueResolver = [];
        List<Action<HelpContext>> helpResolver = [];

        foreach (var property in properties.Where(prop => Attribute.IsDefined(prop, typeof(CommandOptionAttribute))))
        {
            var attribute = property.GetCustomAttribute<CommandOptionAttribute>()!;
            var isRequired = attribute.Required
#if NET7_0_OR_GREATER
                        || property.GetCustomAttribute<RequiredMemberAttribute>() != null;
#else
                ;
#endif
            Type propertyUnderlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var typeParser = GetParser(propertyUnderlyingType, attribute.CaseSensitive);
            var defaultValue = typeParser.ParseToType(null);
            var currentValue = property.GetValue(applicationCommand);
            var currentValueObj = typeParser.ParseFromType(currentValue);
            List<string> aliases = [];
            if (attribute.ShortTerm != null)
            {
                aliases.Add($"-{attribute.ShortTerm}");
            }
            if (!string.IsNullOrEmpty(attribute.Term))
            {
                aliases.Add($"--{attribute.Term}");
            }
            Option option = new Option<string>([.. aliases])
            {
                IsRequired = isRequired
            };
            option.AddValidator(GetValidation<OptionResult>(typeParser, attribute.EnvironmentVariable, attribute.CaseSensitive, isRequired));
            if (typeParser.Choices.Length > 0)
            {
                option.AddCompletions(typeParser.Choices);
            }
            if (attribute.Description != null && !string.IsNullOrEmpty(attribute.Description))
            {
                option.Description = attribute.Description;
            }
            if (defaultValue != currentValue)
            {
                option.SetDefaultValue(currentValueObj);
            }
            valueResolver.Add(context =>
            {
                string? value = null;
                if (context.ParseResult.HasOption(option))
                {
                    value = context.ParseResult.GetValueForOption(option)?.ToString();
                }
                else if (!string.IsNullOrEmpty(attribute.EnvironmentVariable) && Environment.GetEnvironmentVariable(attribute.EnvironmentVariable) is string valueEnv)
                {
                    value = valueEnv;
                }
                property.SetValue(applicationCommand, typeParser.ParseToType(value));
            });
            if (!string.IsNullOrEmpty(attribute.EnvironmentVariable) && Environment.GetEnvironmentVariable(attribute.EnvironmentVariable) is string valueEnv)
            {
                option.SetDefaultValue(valueEnv);
            }
            helpResolver.Add(ctx =>
            {
                ctx.HelpBuilder.CustomizeSymbol(option,
                    firstColumnText: _ =>
                    {
                        List<string> text = [];
                        text.Add(string.Join(", ", aliases));
                        if (propertyUnderlyingType != typeof(bool))
                        {
                            text.Add($"<{option.Name}>");
                        }
                        if (isRequired)
                        {
                            text.Add("(REQUIRED)");
                        }
                        return string.Join(" ", text);
                    },
                    secondColumnText: _ =>
                    {
                        List<string> text = [];
                        if (option.Description != null && !string.IsNullOrEmpty(option.Description))
                        {
                            text.Add(option.Description);
                        }
                        if (attribute.EnvironmentVariable != null && !string.IsNullOrEmpty(attribute.EnvironmentVariable))
                        {
                            text.Add($"Environment variable: {attribute.EnvironmentVariable}");
                        }
                        if (typeParser.Choices.Length > 0)
                        {
                            text.Add($"One of: {string.Join(", ", typeParser.Choices)}");
                        }
                        if (defaultValue != currentValue)
                        {
                            string readableCurrentValueObj;
                            if (currentValueObj is string[] currentValueArr)
                            {
                                readableCurrentValueObj = string.Join(", ", currentValueArr);
                            }
                            else if (currentValueObj is string currentValueStr)
                            {
                                readableCurrentValueObj = currentValueStr;
                            }
                            else
                            {
                                throw new Exception($"Unknown default value {currentValueObj?.GetType()?.Name}");
                            }
                            text.Add($"Default value: {currentValueObj}");
                        }
                        return string.Join("\n", text);
                    });
            });
            hier.CommandLineBuilder.Command.AddOption(option);
        }

        List<(int Position, Argument Argument)> arguments = [];
        foreach (var property in properties.Where(prop => Attribute.IsDefined(prop, typeof(CommandArgumentAttribute))))
        {
            var attribute = property.GetCustomAttribute<CommandArgumentAttribute>()!;
            Type propertyUnderlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            var typeParser = GetParser(propertyUnderlyingType, attribute.CaseSensitive);
            var defaultValue = typeParser.ParseToType(null);
            var currentValue = property.GetValue(applicationCommand);
            var currentValueObj = typeParser.ParseFromType(currentValue);
            Argument argument = new Argument<string>();
            argument.AddValidator(GetValidation<ArgumentResult>(typeParser, null, attribute.CaseSensitive, true));
            if (typeParser.Choices.Length > 0)
            {
                argument.AddCompletions(typeParser.Choices);
            }
            if (attribute.Name != null && !string.IsNullOrEmpty(attribute.Name))
            {
                argument.Name = attribute.Name;
            }
            if (attribute.Description != null && !string.IsNullOrEmpty(attribute.Description))
            {
                argument.Description = attribute.Description;
            }
            if (defaultValue != currentValue)
            {
                argument.SetDefaultValue(currentValueObj);
            }
            if (attribute.FromAmong.Length != 0)
            {
                argument.FromAmong([.. attribute.FromAmong.Select(i => i?.ToString() ?? "").Where(i => !string.IsNullOrEmpty(i))]);
            }
            valueResolver.Add(context =>
            {
                property.SetValue(applicationCommand, typeParser.ParseToType(context.ParseResult.GetValueForArgument(argument)?.ToString()));
            });
            arguments.Add((attribute.Position, argument));
        }

        foreach (var argument in arguments.OrderBy(a => a.Position))
        {
            hier.CommandLineBuilder.Command.AddArgument(argument.Argument);
        }

        hier.CommandLineBuilder.UseHelp(context =>
        {
            foreach (var resolver in helpResolver)
            {
                resolver(context);
            }
        });
        hier.CommandLineBuilder.Command.SetHandler(async context =>
        {
            try
            {
                CancellationTokenSource cts = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
                foreach (var resolver in valueResolver)
                {
                    resolver(context);
                }
                var applicationBuilder = await applicationCommand.ApplicationBuilderInternal(cts.Token);
                foreach (var dependency in _applicationDependencies)
                {
                    applicationBuilder.AddApplication(dependency);
                }
                applicationBuilder.AddApplication(applicationCommand);
                CommandInvokerService commandInvokerService = new();
                applicationBuilder.Services.AddSingleton(commandInvokerService);
                applicationBuilder.Services.AddHostedService<CommandInvokerWorker>();
                var applicationHost = applicationBuilder.Build();
                commandInvokerService.SetCommand(async ct =>
                {
                    await applicationCommand.RunInternal(applicationHost, ct);
                    if (applicationCommand.ExitOnRunComplete)
                    {
                        cts.Cancel();
                    }
                });
                await applicationHost.Run(cts.Token);
                context.ExitCode = 0;
            }
            catch (CommandException ex)
            {
                Console.WriteLine(ex.Message);
                context.ExitCode = ex.ExitCode;
            }
        });
    }

    private ICommandLineTypeParser GetParser(Type type, bool caseSensitive)
    {
        if (_typeParsers.TryGetValue(type, out ICommandLineTypeParser? typeParser))
        {
            return typeParser;
        }
        if (type.IsArray)
        {
            return new ArrayTypeParser(type);
        }
        if (type.IsEnum)
        {
            return new EnumTypeParser(type, caseSensitive);
        }

        throw new Exception($"Unsupported type \"{type.Name}\"");
    }

    private static ValidateSymbolResult<TSymbolResult> GetValidation<TSymbolResult>(ICommandLineTypeParser typeParser, string? environmentVariable, bool caseSensitive, bool required)
        where TSymbolResult : SymbolResult
    {
        return a =>
        {
            bool isOption = a.Symbol is Option;
            string alias = (a.Symbol is Option o) ? o.Aliases.FirstOrDefault() ?? "" : (a.Symbol as Argument)?.Name ?? "";
            string symbol = a.Symbol is Option ? "Option" : "Argument";
            string? value = a.Tokens.SingleOrDefault()?.Value;
            if (required && string.IsNullOrEmpty(value) && (string.IsNullOrEmpty(environmentVariable) || Environment.GetEnvironmentVariable(environmentVariable) is null))
            {
                a.ErrorMessage = $"{symbol} '{alias}' is required.";
                return;
            }
            if (typeParser.Choices.Length > 0)
            {
                bool valid = false;
                foreach (var choice in typeParser.Choices)
                {
                    if (choice.Equals(value, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase))
                    {
                        valid = true;
                        break;
                    }
                }
                if (!valid)
                {
                    a.ErrorMessage = $"{symbol} '{value}' for '{alias}' is not valid. Must be one of:\n\t{string.Join("\n\t", typeParser.Choices)}";
                    return;
                }
            }
            if (!typeParser.Validate(value, out var validateError))
            {
                a.ErrorMessage = $"{symbol} '{value}' for '{alias}' is not valid: {validateError}";
                return;
            }
        };
    }
}
