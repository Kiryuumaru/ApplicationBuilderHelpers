using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Represents a command line option that corresponds to CommandOptionAttribute.
/// Supports option inheritance in subcommand hierarchies.
/// </summary>
internal class SubCommandOptionInfo
{
    /// <summary>
    /// The property this option is bound to
    /// </summary>
    public PropertyInfo Property { get; set; } = null!;

    /// <summary>
    /// The type of the property
    /// </summary>
    public Type PropertyType { get; set; } = null!;

    /// <summary>
    /// Short option name (e.g., 'l' for -l)
    /// </summary>
    public char? ShortName { get; set; }

    /// <summary>
    /// Long option name (e.g., "log-level" for --log-level)
    /// </summary>
    public string? LongName { get; set; }

    /// <summary>
    /// Description of the option
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Whether this option is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Environment variable to fall back to if option is not provided
    /// </summary>
    public string? EnvironmentVariable { get; set; }

    /// <summary>
    /// Valid values for this option (for validation)
    /// </summary>
    public object[]? ValidValues { get; set; }

    /// <summary>
    /// Whether validation should be case sensitive
    /// </summary>
    public bool IsCaseSensitive { get; set; }

    /// <summary>
    /// Default value for the option
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Whether this option is global (available to all subcommands)
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Whether this option is inherited by child commands
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// Whether this is a boolean flag option
    /// </summary>
    public bool IsFlag => PropertyType == typeof(bool) || PropertyType == typeof(bool?);

    /// <summary>
    /// Whether this option accepts multiple values (array type)
    /// </summary>
    public bool IsArray => PropertyType.IsArray;

    /// <summary>
    /// The element type if this is an array option
    /// </summary>
    public Type? ElementType => IsArray ? PropertyType.GetElementType() : null;

    /// <summary>
    /// The command this option belongs to
    /// </summary>
    public SubCommandInfo? OwnerCommand { get; set; }

    /// <summary>
    /// Creates a SubCommandOptionInfo from a property and its CommandOptionAttribute
    /// </summary>
    public static SubCommandOptionInfo FromProperty(PropertyInfo property, CommandOptionAttribute attribute, SubCommandInfo? ownerCommand = null)
    {
        var optionInfo = new SubCommandOptionInfo
        {
            Property = property,
            PropertyType = property.PropertyType,
            ShortName = attribute.ShortTerm,
            LongName = attribute.Term ?? property.Name.ToLowerInvariant(),
            Description = attribute.Description,
            IsRequired = attribute.Required,
            EnvironmentVariable = attribute.EnvironmentVariable,
            ValidValues = attribute.FromAmong?.Length > 0 ? attribute.FromAmong : null,
            IsCaseSensitive = attribute.CaseSensitive,
            OwnerCommand = ownerCommand
        };

        // Determine if this option should be inherited by checking if it comes from a base class
        // This works for both concrete command instances and abstract command types
        var declaringType = property.DeclaringType;
        var targetType = ownerCommand?.Command?.GetType() ?? ownerCommand?.CommandParts.LastOrDefault() switch
        {
            string lastPart => FindTypeByCommandName(lastPart, declaringType),
            _ => null
        };

        if (targetType != null && declaringType != targetType && declaringType != null && declaringType.IsAssignableFrom(targetType))
        {
            optionInfo.IsInherited = true;
            optionInfo.DetermineInheritanceScope();
        }

        return optionInfo;
    }

    /// <summary>
    /// Helper method to find a type by command name (used for abstract command processing)
    /// </summary>
    private static Type? FindTypeByCommandName(string commandName, Type? declaringType)
    {
        // For abstract command processing, we can use the declaring type as a reference
        return declaringType;
    }

    /// <summary>
    /// Creates a list of SubCommandOptionInfo objects from a command type
    /// </summary>
    public static List<SubCommandOptionInfo> FromCommandType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type commandType, SubCommandInfo? ownerCommand = null)
    {
        var options = new List<SubCommandOptionInfo>();
        var properties = GetAllProperties(commandType);

        foreach (var property in properties)
        {
            var optionAttr = property.GetCustomAttribute<CommandOptionAttribute>();
            if (optionAttr != null)
            {
                var optionInfo = FromProperty(property, optionAttr, ownerCommand);
                options.Add(optionInfo);
            }
        }

        return options;
    }

    /// <summary>
    /// Creates a list of SubCommandOptionInfo objects from properties declared directly in the specified type
    /// (excludes inherited properties to avoid conflicts)
    /// </summary>
    public static List<SubCommandOptionInfo> FromDeclaredType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type commandType, SubCommandInfo? ownerCommand = null)
    {
        var options = new List<SubCommandOptionInfo>();
        var properties = commandType.GetProperties(
            BindingFlags.DeclaredOnly | 
            BindingFlags.Public | 
            BindingFlags.NonPublic | 
            BindingFlags.Instance);

        foreach (var property in properties)
        {
            var optionAttr = property.GetCustomAttribute<CommandOptionAttribute>();
            if (optionAttr != null)
            {
                var optionInfo = FromProperty(property, optionAttr, ownerCommand);
                options.Add(optionInfo);
            }
        }

        return options;
    }

    /// <summary>
    /// Gets all properties including inherited ones from base classes
    /// </summary>
    private static List<PropertyInfo> GetAllProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties)] Type type)
    {
        var properties = new List<PropertyInfo>();
        var currentType = type;

        while (currentType != null)
        {
            var declaredProperties = currentType.GetProperties(
                BindingFlags.DeclaredOnly | 
                BindingFlags.Public | 
                BindingFlags.NonPublic | 
                BindingFlags.Instance);
            
            properties.AddRange(declaredProperties.Reverse());
            currentType = currentType.BaseType;
        }

        properties.Reverse();
        return properties;
    }

    /// <summary>
    /// Determines whether this option should be inherited by child commands
    /// </summary>
    private void DetermineInheritanceScope()
    {
        // For options that come from base classes, they should be inherited but not automatically global
        // Let the global option detection logic determine what's truly global based on actual usage patterns
        IsGlobal = false;  // Don't automatically promote to global based on hardcoded names
        IsInherited = true; // But do mark as inherited since this method is only called for base class options
    }

    /// <summary>
    /// Validates the option value against constraints
    /// </summary>
    public void ValidateValue(object? value)
    {
        if (IsRequired && value == null)
        {
            throw new ArgumentException($"Required option '--{LongName ?? ShortName?.ToString()}' is missing");
        }

        if (value != null && ValidValues?.Length > 0)
        {
            var stringValue = value.ToString();
            var comparisonType = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
            var isValid = ValidValues.Any(validValue => 
                string.Equals(validValue?.ToString(), stringValue, comparisonType));

            if (!isValid)
            {
                var validValuesString = string.Join(", ", ValidValues.Select(v => v?.ToString()));
                throw new ArgumentException(
                    $"Value '{stringValue}' is not valid for option '--{LongName ?? ShortName?.ToString()}'. " +
                    $"Must be one of: {validValuesString}");
            }
        }
    }

    /// <summary>
    /// Gets the option name for display purposes
    /// </summary>
    public string GetDisplayName()
    {
        if (ShortName.HasValue && !string.IsNullOrEmpty(LongName))
            return $"-{ShortName}, --{LongName}";
        else if (ShortName.HasValue)
            return $"-{ShortName}";
        else if (!string.IsNullOrEmpty(LongName))
            return $"--{LongName}";
        else
            return Property.Name;
    }

    /// <summary>
    /// Gets the option signature for help text
    /// </summary>
    public string GetSignature()
    {
        var name = GetDisplayName();
        
        if (IsFlag)
            return name;
            
        var typeName = GetTypeName();
        return $"{name} <{typeName}>";
    }

    /// <summary>
    /// Gets the type name for display
    /// </summary>
    public string GetTypeName()
    {
        var targetType = IsArray ? ElementType! : PropertyType;
        
        return targetType.Name.ToLowerInvariant() switch
        {
            "string" => "TEXT",
            "int32" => "NUMBER",
            "double" => "NUMBER",
            "boolean" => "BOOL",
            "datetime" => "DATE",
            "directoryinfo" => "DIR",
            "fileinfo" => "FILE",
            _ => targetType.Name.ToUpperInvariant()
        };
    }

    /// <summary>
    /// Checks if this option matches the given argument
    /// </summary>
    public bool MatchesArgument(string argument)
    {
        // Long option format: --option or --option=value
        if (LongName != null && (argument == $"--{LongName}" || argument.StartsWith($"--{LongName}=")))
            return true;

        // Short option format: -o or -o=value or -ovalue (compact)
        if (ShortName.HasValue)
        {
            if (argument == $"-{ShortName}" || argument.StartsWith($"-{ShortName}="))
                return true;

            // Compact format for non-boolean options: -ovalue
            if (!IsFlag && argument.StartsWith($"-{ShortName}") && argument.Length > 2)
                return true;
        }

        return false;
    }

    /// <summary>
    /// Extracts the value from a command line argument
    /// </summary>
    public string? ExtractValue(string argument, string? nextArgument = null)
    {
        // Handle --option=value format
        if (LongName != null && argument.StartsWith($"--{LongName}="))
        {
            return argument.Substring($"--{LongName}=".Length);
        }

        // Handle -o=value format
        if (ShortName.HasValue && argument.StartsWith($"-{ShortName}="))
        {
            return argument.Substring($"-{ShortName}=".Length);
        }

        // Handle compact format -ovalue
        if (ShortName.HasValue && !IsFlag && argument.StartsWith($"-{ShortName}") && argument.Length > 2)
        {
            return argument.Substring(2);
        }

        // Handle --option value or -o value format
        if ((LongName != null && argument == $"--{LongName}") ||
            (ShortName.HasValue && argument == $"-{ShortName}"))
        {
            if (IsFlag)
            {
                // For boolean flags, check if next argument is a boolean value
                if (nextArgument != null && IsBooleanValue(nextArgument))
                    return nextArgument;
                else
                    return "true"; // Flag without value means true
            }
            else
            {
                return nextArgument; // Use next argument as value
            }
        }

        return null;
    }

    /// <summary>
    /// Checks if a string represents a boolean value
    /// </summary>
    private static bool IsBooleanValue(string value)
    {
        return value.Equals("true", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("false", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("yes", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("no", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("on", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("off", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("1", StringComparison.OrdinalIgnoreCase) ||
               value.Equals("0", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a string representation of the option
    /// </summary>
    public override string ToString()
    {
        return GetDisplayName();
    }
}
