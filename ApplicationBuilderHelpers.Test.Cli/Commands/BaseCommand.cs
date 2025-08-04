using ApplicationBuilderHelpers.Attributes;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Test.Cli.Commands;

internal abstract class BaseCommand : Command
{
    [CommandOption('l', "log-level", Description = "Set the logging level", FromAmong = ["trace", "debug", "information", "warning", "error", "critical", "none"])]
    public string LogLevel { get; set; } = "information";

    [CommandOption('q', "quiet", Description = "Suppress output except errors")]
    public bool Quiet { get; set; }

    [CommandOption("env", Description = "Environment variables to set")]
    public string[] EnvironmentVariables { get; set; } = [];

    [CommandOption("debug-parser", Description = "Enable debug output for parser diagnostics")]
    public bool DebugParser { get; set; }

    protected void PrintDebugInfo()
    {
        if (!DebugParser) return;

        Console.WriteLine();
        Console.WriteLine("===============================================");
        Console.WriteLine("[DEBUG] COMMAND LINE PARSER DEBUG INFORMATION");
        Console.WriteLine("===============================================");

        // Print command info
        var commandAttr = GetType().GetCustomAttribute<CommandAttribute>();
        Console.WriteLine($"[CMD] Command: {commandAttr?.Name ?? GetType().Name}");
        Console.WriteLine($"[DSC] Description: {commandAttr?.Description ?? "No description"}");
        Console.WriteLine();

        // Print all properties with their attributes and values
        var properties = GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite)
            .OrderBy(p => p.Name);

        Console.WriteLine("[OPT] ALL OPTIONS AND ARGUMENTS:");
        Console.WriteLine("-----------------------------------------------");

        foreach (var prop in properties)
        {
            var optionAttr = prop.GetCustomAttribute<CommandOptionAttribute>();
            var argAttr = prop.GetCustomAttribute<CommandArgumentAttribute>();
            
            if (optionAttr != null || argAttr != null)
            {
                var value = prop.GetValue(this);
                var valueStr = FormatValue(value, prop.PropertyType);
                var isDefault = IsDefaultValue(value, prop.PropertyType);
                var sourceIndicator = isDefault ? "[DEF]" : "[SET]";
                
                if (optionAttr != null)
                {
                    Console.WriteLine($"{sourceIndicator} OPTION: {prop.Name}");
                    Console.WriteLine($"   Short: {(optionAttr.ShortTerm.HasValue ? $"-{optionAttr.ShortTerm}" : "N/A")}");
                    Console.WriteLine($"   Long: --{optionAttr.Term ?? prop.Name.ToLowerInvariant()}");
                    Console.WriteLine($"   Type: {GetFriendlyTypeName(prop.PropertyType)}");
                    Console.WriteLine($"   Value: {valueStr}");
                    Console.WriteLine($"   Default: {isDefault}");
                    
                    if (!string.IsNullOrEmpty(optionAttr.EnvironmentVariable))
                    {
                        var envValue = Environment.GetEnvironmentVariable(optionAttr.EnvironmentVariable);
                        Console.WriteLine($"   Env Var: {optionAttr.EnvironmentVariable} = {envValue ?? "NOT_SET"}");
                    }
                    
                    if (optionAttr.FromAmong?.Length > 0)
                    {
                        Console.WriteLine($"   Allowed: [{string.Join(", ", optionAttr.FromAmong)}]");
                    }
                }
                else if (argAttr != null)
                {
                    Console.WriteLine($"{sourceIndicator} ARGUMENT: {prop.Name}");
                    Console.WriteLine($"   Position: {argAttr.Position}");
                    Console.WriteLine($"   Required: {argAttr.Required}");
                    Console.WriteLine($"   Type: {GetFriendlyTypeName(prop.PropertyType)}");
                    Console.WriteLine($"   Value: {valueStr}");
                    Console.WriteLine($"   Default: {isDefault}");
                }
                
                if (!string.IsNullOrEmpty(optionAttr?.Description) || !string.IsNullOrEmpty(argAttr?.Description))
                {
                    Console.WriteLine($"   Description: {optionAttr?.Description ?? argAttr?.Description}");
                }
                
                Console.WriteLine();
            }
        }

        // Print raw command line arguments if available
        var args = Environment.GetCommandLineArgs();
        Console.WriteLine("[RAW] RAW COMMAND LINE:");
        Console.WriteLine("-----------------------------------------------");
        for (int i = 0; i < args.Length; i++)
        {
            Console.WriteLine($"   [{i}]: {args[i]}");
        }
        Console.WriteLine();

        // Print environment variables related to this command
        Console.WriteLine("[ENV] RELEVANT ENVIRONMENT VARIABLES:");
        Console.WriteLine("-----------------------------------------------");
        foreach (var prop in properties)
        {
            var optionAttr = prop.GetCustomAttribute<CommandOptionAttribute>();
            if (optionAttr?.EnvironmentVariable != null)
            {
                var envValue = Environment.GetEnvironmentVariable(optionAttr.EnvironmentVariable);
                var status = envValue != null ? "SET" : "NOT_SET";
                Console.WriteLine($"   {optionAttr.EnvironmentVariable}: {envValue ?? "null"} ({status})");
            }
        }

        Console.WriteLine();
        Console.WriteLine("===============================================");
        Console.WriteLine();
    }

    private static string FormatValue(object? value, Type type)
    {
        if (value == null) return "null";
        
        if (type.IsArray)
        {
            var array = (Array)value;
            if (array.Length == 0) return "[]";
            
            var elements = new List<string>();
            foreach (var item in array)
            {
                elements.Add(item?.ToString() ?? "null");
            }
            return $"[{string.Join(", ", elements)}]";
        }
        
        if (type == typeof(bool))
        {
            return value.ToString()!;
        }
        
        if (type == typeof(string))
        {
            var str = (string)value;
            return string.IsNullOrEmpty(str) ? "\"\"" : $"\"{str}\"";
        }
        
        return value.ToString() ?? "null";
    }

    private static bool IsDefaultValue(object? value, Type type)
    {
        if (value == null) return true;
        
        if (type.IsArray)
        {
            var array = (Array)value;
            return array.Length == 0;
        }
        
        if (type == typeof(bool))
        {
            return (bool)value == false;
        }
        
        if (type == typeof(string))
        {
            return string.IsNullOrEmpty((string)value);
        }
        
        if (type == typeof(int))
        {
            return (int)value == 0;
        }
        
        if (type == typeof(double))
        {
            return Math.Abs((double)value) < 0.0001;
        }
        
        return Equals(value, GetDefaultValue(type));
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067", Justification = "We only create default values for simple types")]
    private static object? GetDefaultValue(Type type)
    {
        if (type.IsValueType)
        {
            return Activator.CreateInstance(type);
        }
        return null;
    }

    private static string GetFriendlyTypeName(Type type)
    {
        if (type == typeof(bool)) return "Boolean";
        if (type == typeof(int)) return "Integer";
        if (type == typeof(double)) return "Double";
        if (type == typeof(string)) return "String";
        if (type == typeof(int?)) return "Integer?";
        if (type == typeof(double?)) return "Double?";
        if (type == typeof(string[])) return "String[]";
        if (type == typeof(int[])) return "Integer[]";
        
        if (type.IsArray)
        {
            return $"{GetFriendlyTypeName(type.GetElementType()!)}[]";
        }
        
        if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            return $"{GetFriendlyTypeName(type.GetGenericArguments()[0])}?";
        }
        
        return type.Name;
    }
}
