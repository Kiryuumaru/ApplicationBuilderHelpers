using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Exceptions;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;
using System.CommandLine.Invocation;
using System.CommandLine.Parsing;
using Microsoft.Extensions.DependencyInjection;
using ApplicationBuilderHelpers.Workers;
using ApplicationBuilderHelpers.Services;
using System.ComponentModel.DataAnnotations;

namespace ApplicationBuilderHelpers;

/// <summary>
/// Represents a builder for managing application dependencies and running the configured application.
/// </summary>
public class ApplicationBuilder
{
    private class ApplicationCommandHierarchy(string? name, Command command)
    {
        public string? Name { get; set; } = name;

        public Command Command { get; set; } = command;

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
        ApplicationCommandHierarchy rootHierarchy = new(null, new RootCommand());
        if (_executableName != null && !string.IsNullOrEmpty(_executableName))
        {
            rootHierarchy.Command.Name = _executableName;
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
                        subHier = new(splitCommand, childCommand);
                        currentHier.Command.AddCommand(childCommand);
                        currentHier.SubCommands.Add(splitCommand, subHier);
                    }
                    currentHier = subHier;
                }
            }
            currentHier.AppCommand = command;
            WireHandler(currentHier, currentHier.AppCommand, cancellationTokenSource.Token);
        }
        return await rootHierarchy.Command.InvokeAsync(args);
    }

    private void WireHandler(ApplicationCommandHierarchy hier, ApplicationCommand applicationCommand, CancellationToken stoppingToken)
    {
        PropertyInfo[] properties = applicationCommand.GetType().GetProperties();

        List<Action<InvocationContext>> valueResolver = [];

        foreach (var property in properties.Where(prop => Attribute.IsDefined(prop, typeof(CommandOptionAttribute))))
        {
            var attribute = property.GetCustomAttribute<CommandOptionAttribute>()!;
            var defaultValue = GetDefaultValue(property.PropertyType);
            var currentValue = property.GetValue(applicationCommand);
            var isRequired = attribute.Required
#if NET7_0_OR_GREATER
                        || property.GetCustomAttribute<RequiredMemberAttribute>() != null;
#else
                ;
#endif
            Type propertyUnderlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            List<string> aliases = [];
            if (!string.IsNullOrEmpty(attribute.Term))
            {
                aliases.Add($"--{attribute.Term}");
            }
            if (attribute.ShortTerm != null)
            {
                aliases.Add($"-{attribute.ShortTerm}");
            }
            Option option = CreateOption(propertyUnderlyingType, [.. aliases], attribute.CaseSensitive, isRequired);
            option.Description = attribute.Description;
            option.IsRequired = isRequired;
            if (defaultValue != currentValue)
            {
                option.SetDefaultValue(currentValue);
            }
            valueResolver.Add(context =>
            {
                property.SetValue(applicationCommand, ResolveValue(propertyUnderlyingType, context.ParseResult.GetValueForOption(option), attribute.CaseSensitive));
            });
            hier.Command.AddOption(option);
        }

        List<(int Position, Argument Argument)> arguments = [];
        foreach (var property in properties.Where(prop => Attribute.IsDefined(prop, typeof(CommandArgumentAttribute))))
        {
            var attribute = property.GetCustomAttribute<CommandArgumentAttribute>()!;
            var defaultValueType = GetDefaultValue(property.PropertyType);
            var currentValue = property.GetValue(applicationCommand);
            Type propertyUnderlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            Argument argument = CreateArgument(propertyUnderlyingType, attribute.CaseSensitive, true);
            argument.Description = attribute.Description;
            if (attribute.Name != null && !string.IsNullOrEmpty(attribute.Name))
            {
                argument.Name = attribute.Name;
            }
            if (defaultValueType != currentValue)
            {
                argument.SetDefaultValue(currentValue);
            }
            if (attribute.FromAmong.Length != 0)
            {
                argument.FromAmong([.. attribute.FromAmong.Select(i => i?.ToString() ?? "").Where(i => !string.IsNullOrEmpty(i))]);
            }
            valueResolver.Add(context =>
            {
                property.SetValue(applicationCommand, ResolveValue(propertyUnderlyingType, context.ParseResult.GetValueForArgument(argument), attribute.CaseSensitive));
            });
            arguments.Add((attribute.Position, argument));
        }

        foreach (var argument in arguments.OrderBy(a => a.Position))
        {
            hier.Command.AddArgument(argument.Argument);
        }

        hier.Command.SetHandler(async context =>
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

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static object? GetDefaultValue(Type type)
    {
        if (type == typeof(short)) return (short)0;
        if (type == typeof(int)) return 0;
        if (type == typeof(long)) return (long)0;
        if (type == typeof(ushort)) return (ushort)0;
        if (type == typeof(uint)) return (uint)0;
        if (type == typeof(ulong)) return (ulong)0;
        if (type == typeof(float)) return (float)0;
        if (type == typeof(double)) return (double)0;
        if (type == typeof(decimal)) return (decimal)0;
        if (type == typeof(bool)) return false;
        if (type == typeof(char)) return '\0';
        if (type == typeof(byte)) return (byte)0;
        if (type == typeof(sbyte)) return (sbyte)0;
        if (type == typeof(string)) return null;
        if (type == typeof(DateTime)) return new DateTime();
        if (type == typeof(DateTimeOffset)) return new DateTimeOffset();
        if (type.IsEnum) return Enum.GetValues(type).GetValue(0);
        return null;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static Option CreateOption(Type type, string[] aliases, bool caseSensitive, bool required)
    {
        if (type == typeof(short)) return new Option<short>(aliases);
        if (type == typeof(int)) return new Option<int>(aliases);
        if (type == typeof(long)) return new Option<long>(aliases);
        if (type == typeof(ushort)) return new Option<ushort>(aliases);
        if (type == typeof(uint)) return new Option<uint>(aliases);
        if (type == typeof(ulong)) return new Option<ulong>(aliases);
        if (type == typeof(float)) return new Option<float>(aliases);
        if (type == typeof(double)) return new Option<double>(aliases);
        if (type == typeof(decimal)) return new Option<decimal>(aliases);
        if (type == typeof(bool)) return new Option<bool>(aliases);
        if (type == typeof(char)) return new Option<char>(aliases);
        if (type == typeof(byte)) return new Option<byte>(aliases);
        if (type == typeof(sbyte)) return new Option<sbyte>(aliases);
        if (type == typeof(string)) return new Option<string>(aliases);
        if (type == typeof(DateTime)) return new Option<DateTime>(aliases, parseArgument: new ParseArgument<DateTime>(a => DateTime.Parse(a.Tokens.SingleOrDefault()?.Value!)));
        if (type == typeof(DateTimeOffset)) return new Option<DateTimeOffset>(aliases, parseArgument: new ParseArgument<DateTimeOffset>(a => DateTimeOffset.Parse(a.Tokens.SingleOrDefault()?.Value!)));
        if (type.IsEnum)
        {
            var option = new Option<string>(aliases, parseArgument: new ParseArgument<string>(a => GetCasedEnum(type, a.Tokens.SingleOrDefault()?.Value, caseSensitive)));
            option.AddCompletions(GetEnumStrings(type));
            option.AddValidator(GetEnumValidation<OptionResult>(type, caseSensitive, required));
            return option;
        }
        throw new Exception($"Unsupported type \"{type.Name}\" for option");
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static Argument CreateArgument(Type type, bool caseSensitive, bool required)
    {
        if (type == typeof(short)) return new Argument<short>();
        if (type == typeof(int)) return new Argument<int>();
        if (type == typeof(long)) return new Argument<long>();
        if (type == typeof(ushort)) return new Argument<ushort>();
        if (type == typeof(uint)) return new Argument<uint>();
        if (type == typeof(ulong)) return new Argument<ulong>();
        if (type == typeof(float)) return new Argument<float>();
        if (type == typeof(double)) return new Argument<double>();
        if (type == typeof(decimal)) return new Argument<decimal>();
        if (type == typeof(bool)) return new Argument<bool>();
        if (type == typeof(char)) return new Argument<char>();
        if (type == typeof(byte)) return new Argument<byte>();
        if (type == typeof(sbyte)) return new Argument<sbyte>();
        if (type == typeof(string)) return new Argument<string>();
        if (type == typeof(DateTime)) return new Argument<DateTime>(parse: new ParseArgument<DateTime>(a => DateTime.Parse(a.GetValueOrDefault()?.ToString()!)));
        if (type == typeof(DateTimeOffset)) return new Argument<DateTimeOffset>(parse: new ParseArgument<DateTimeOffset>(a => DateTimeOffset.Parse(a.GetValueOrDefault()?.ToString()!)));
        if (type.IsEnum)
        {
            var option = new Argument<string>(parse: new ParseArgument<string>(a => GetCasedEnum(type, a.Tokens.SingleOrDefault()?.Value, caseSensitive)));
            option.AddCompletions(GetEnumStrings(type));
            option.AddValidator(GetEnumValidation<ArgumentResult>(type, caseSensitive, required));
            return option;
        }
        throw new Exception($"Unsupported type \"{type.Name}\" for argument");
    }

    private static object? ResolveValue(Type type, object? value, bool caseSensitive)
    {
        if (type == typeof(short)) return value;
        if (type == typeof(int)) return value;
        if (type == typeof(long)) return value;
        if (type == typeof(ushort)) return value;
        if (type == typeof(uint)) return value;
        if (type == typeof(ulong)) return value;
        if (type == typeof(float)) return value;
        if (type == typeof(double)) return value;
        if (type == typeof(decimal)) return value;
        if (type == typeof(bool)) return value;
        if (type == typeof(char)) return value;
        if (type == typeof(byte)) return value;
        if (type == typeof(sbyte)) return value;
        if (type == typeof(string)) return value;
        if (type == typeof(DateTime)) return value;
        if (type == typeof(DateTimeOffset)) return value;
        if (type.IsEnum)
        {
            return Enum.Parse(type, value?.ToString() ?? "", !caseSensitive);
        }
        throw new Exception($"Unsupported type \"{type.Name}\", for resolve");
    }

    private static ValidateSymbolResult<TSymbolResult> GetEnumValidation<TSymbolResult>(Type type, bool caseSensitive, bool required)
        where TSymbolResult : SymbolResult
    {
        return a =>
        {
            var value = a.Tokens.SingleOrDefault()?.Value?.ToString();
            if (required && string.IsNullOrEmpty(value))
            {
                a.ErrorMessage = $"Argument '{a.Symbol.Name}' is required";
                return;
            }
            if (!string.IsNullOrEmpty(value) && !TryGetCasedEnum(type, value, caseSensitive, out _))
            {
                a.ErrorMessage = $"Argument '{value}' not recognized. Must be one of:\n\t{string.Join("\n\t", GetEnumStrings(type))}";
                return;
            }
        };
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static string GetCasedEnum(Type type, object? value, bool caseSensitive)
    {
        var valueStr = value?.ToString();
        if (TryGetCasedEnum(type, value, caseSensitive, out var casedEnum))
        {
            return casedEnum;
        }
        throw new Exception($"Value \"{valueStr}\" is not a valid value for enum \"{type.Name}\"");
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static bool TryGetCasedEnum(Type type, object? value, bool caseSensitive, [NotNullWhen(true)] out string? casedEnum)
    {
        var valueStr = value?.ToString();
        foreach (var enumValue in GetEnumStrings(type))
        {
            if (enumValue.Equals(valueStr, caseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase))
            {
                casedEnum = enumValue;
                return true;
            }
        }
        casedEnum = null;
        return false;
    }

    [UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
    private static string[] GetEnumStrings(Type type)
    {
        return [.. Enum.GetValues(type)
            .Cast<object>()
            .Select(i => i?.ToString()!)
            .Where(i => !string.IsNullOrEmpty(i))];
    }
}
