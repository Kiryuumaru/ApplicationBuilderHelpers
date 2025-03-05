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

namespace ApplicationBuilderHelpers;

public class ApplicationBuilder
{
    private class ApplicationCommandHierarchy(string? name, Command command)
    {
        public string? Name { get; set; } = name;

        public Command Command { get; set; } = command;

        public ApplicationCommand? AppCommand { get; set; } = null;

        public Dictionary<string, ApplicationCommandHierarchy> SubCommands { get; set; } = [];
    }

    public static ApplicationBuilder Create()
    {
        return new ApplicationBuilder();
    }

    private readonly List<ApplicationDependency> _applicationDependencies = [];
    private readonly List<ApplicationCommand> _commands = [];

    private string? _executableName = null;

    private ApplicationBuilder() { }

    public ApplicationBuilder SetExecutableName(string name)
    {
        _executableName = name;
        return this;
    }

    public ApplicationBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TApplicationCommand>(TApplicationCommand applicationCommand)
        where TApplicationCommand : ApplicationCommand
    {
        _commands.Add(applicationCommand);
        return this;
    }

    public ApplicationBuilder AddCommand<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] TApplicationCommand>()
        where TApplicationCommand : ApplicationCommand
    {
        return AddCommand(applicationCommand: Activator.CreateInstance<TApplicationCommand>());
    }

    public ApplicationBuilder AddApplication(ApplicationDependency applicationDependency)
    {
        _applicationDependencies.Add(applicationDependency);
        return this;
    }

    public ApplicationBuilder AddApplication<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] TApplicationDependency>()
        where TApplicationDependency : ApplicationDependency
    {
        _applicationDependencies.Add(Activator.CreateInstance<TApplicationDependency>());
        return this;
    }

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
            var defaultValueType = GetDefaultValue(property.PropertyType);
            var currentValue = property.GetValue(applicationCommand);
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
            Option option = CreateOption(propertyUnderlyingType, [.. aliases]);
            option.Description = attribute.Description;
            option.IsRequired = attribute.Required
#if NET7_0_OR_GREATER
                || property.GetCustomAttribute<RequiredMemberAttribute>() != null;
#else
                ;
#endif
            if (defaultValueType != currentValue)
            {
                option.SetDefaultValue(currentValue);
            }
            valueResolver.Add(context =>
            {
                property.SetValue(applicationCommand, context.ParseResult.GetValueForOption(option));
            });
            hier.Command.AddOption(option);
        }

        List <(int Position, Argument Argument)> arguments = [];
        foreach (var property in properties.Where(prop => Attribute.IsDefined(prop, typeof(CommandArgumentAttribute))))
        {
            var attribute = property.GetCustomAttribute<CommandArgumentAttribute>()!;
            var defaultValueType = GetDefaultValue(property.PropertyType);
            var currentValue = property.GetValue(applicationCommand);
            Type propertyUnderlyingType = Nullable.GetUnderlyingType(property.PropertyType) ?? property.PropertyType;
            Argument argument = CreateArgument(propertyUnderlyingType);
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
                argument.FromAmong([.. attribute.FromAmong.Select(i => i?.ToString() ?? "")]);
            }
            valueResolver.Add(context =>
            {
                property.SetValue(applicationCommand, context.ParseResult.GetValueForArgument(argument));
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

    private static Option CreateOption(Type type, string[] aliases)
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
        if (type == typeof(DateTime)) return new Option<DateTime>(aliases, parseArgument: new ParseArgument<DateTime>(a => DateTime.Parse(a.GetValueOrDefault()?.ToString()!)));
        if (type == typeof(DateTimeOffset)) return new Option<DateTimeOffset>(aliases, parseArgument: new ParseArgument<DateTimeOffset>(a => DateTimeOffset.Parse(a.GetValueOrDefault()?.ToString()!)));
        throw new Exception("Unsupported type.");
    }

    private static Argument CreateArgument(Type type)
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
        throw new Exception("Unsupported type.");
    }
}
