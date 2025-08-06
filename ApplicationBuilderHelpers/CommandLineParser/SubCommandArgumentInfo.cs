using ApplicationBuilderHelpers.Attributes;
using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Represents a command line argument that corresponds to CommandArgumentAttribute.
/// Supports argument inheritance in subcommand hierarchies.
/// </summary>
internal class SubCommandArgumentInfo
{
    /// <summary>
    /// The property this argument is bound to
    /// </summary>
    public PropertyInfo Property { get; set; } = null!;

    /// <summary>
    /// The type of the property
    /// </summary>
    public Type PropertyType { get; set; } = null!;

    /// <summary>
    /// The name of the argument (for display purposes)
    /// </summary>
    public string? Name { get; set; }

    /// <summary>
    /// Description of the argument
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Position of the argument (0-based index)
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Whether this argument is required
    /// </summary>
    public bool IsRequired { get; set; }

    /// <summary>
    /// Valid values for this argument (for validation)
    /// </summary>
    public object[]? ValidValues { get; set; }

    /// <summary>
    /// Whether validation should be case sensitive
    /// </summary>
    public bool IsCaseSensitive { get; set; }

    /// <summary>
    /// Default value for the argument
    /// </summary>
    public object? DefaultValue { get; set; }

    /// <summary>
    /// Whether this argument is global (available to all subcommands)
    /// </summary>
    public bool IsGlobal { get; set; }

    /// <summary>
    /// Whether this argument is inherited by child commands
    /// </summary>
    public bool IsInherited { get; set; }

    /// <summary>
    /// Whether this argument accepts multiple values (array type)
    /// </summary>
    public bool IsArray => PropertyType.IsArray;

    /// <summary>
    /// The element type if this is an array argument
    /// </summary>
    public Type? ElementType => IsArray ? PropertyType.GetElementType() : null;

    /// <summary>
    /// The command this argument belongs to
    /// </summary>
    public SubCommandInfo? OwnerCommand { get; set; }

    /// <summary>
    /// Display name for the argument (used in help text)
    /// </summary>
    public string DisplayName => Name ?? Property.Name.ToLowerInvariant();

    /// <summary>
    /// Creates a SubCommandArgumentInfo from a property and its CommandArgumentAttribute
    /// </summary>
    public static SubCommandArgumentInfo FromProperty(PropertyInfo property, CommandArgumentAttribute attribute, SubCommandInfo? ownerCommand = null)
    {
        var argumentInfo = new SubCommandArgumentInfo
        {
            Property = property,
            PropertyType = property.PropertyType,
            Name = attribute.Name ?? property.Name.ToLowerInvariant(),
            Description = attribute.Description,
            Position = attribute.Position,
            IsRequired = attribute.Required,
            ValidValues = attribute.FromAmong?.Length > 0 ? attribute.FromAmong : null,
            IsCaseSensitive = attribute.CaseSensitive,
            OwnerCommand = ownerCommand
        };

        // Determine if this argument should be inherited
        argumentInfo.DetermineInheritanceScope();

        return argumentInfo;
    }

    /// <summary>
    /// Creates a list of SubCommandArgumentInfo objects from a command type
    /// </summary>
    public static List<SubCommandArgumentInfo> FromCommandType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, SubCommandInfo? ownerCommand = null)
    {
        var arguments = new List<SubCommandArgumentInfo>();
        var properties = GetAllProperties(commandType);

        foreach (var property in properties)
        {
            var argumentAttr = property.GetCustomAttribute<CommandArgumentAttribute>();
            if (argumentAttr != null)
            {
                var argumentInfo = FromProperty(property, argumentAttr, ownerCommand);
                arguments.Add(argumentInfo);
            }
        }

        return [.. arguments.OrderBy(a => a.Position)];
    }

    /// <summary>
    /// Creates a list of SubCommandArgumentInfo objects from properties declared directly in the specified type
    /// (excludes inherited properties to avoid conflicts)
    /// </summary>
    public static List<SubCommandArgumentInfo> FromDeclaredType([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type commandType, SubCommandInfo? ownerCommand = null)
    {
        var arguments = new List<SubCommandArgumentInfo>();
        var properties = commandType.GetProperties(
            BindingFlags.DeclaredOnly | 
            BindingFlags.Public | 
            BindingFlags.NonPublic | 
            BindingFlags.Instance);

        foreach (var property in properties)
        {
            var argumentAttr = property.GetCustomAttribute<CommandArgumentAttribute>();
            if (argumentAttr != null)
            {
                var argumentInfo = FromProperty(property, argumentAttr, ownerCommand);
                arguments.Add(argumentInfo);
            }
        }

        return [.. arguments.OrderBy(a => a.Position)];
    }

    /// <summary>
    /// Gets all properties including inherited ones from base classes
    /// </summary>
    private static List<PropertyInfo> GetAllProperties([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] Type type)
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
    /// Determines whether this argument should be inherited by child commands
    /// </summary>
    private void DetermineInheritanceScope()
    {
        // Arguments are typically more specific to individual commands
        // But some arguments like input files might be common across command hierarchies
        var commonArgumentNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "input", "file", "path", "directory", "target", "source"
        };

        var isCommonArgument = Name != null && commonArgumentNames.Contains(Name);

        if (isCommonArgument)
        {
            IsGlobal = false; // Arguments are rarely truly global
            IsInherited = true;
        }
        else
        {
            IsGlobal = false;
            IsInherited = false;
        }
    }

    /// <summary>
    /// Gets the argument signature for help text
    /// </summary>
    public string GetSignature()
    {
        var name = DisplayName.ToUpperInvariant();
        
        if (IsArray)
            name += "...";
            
        if (IsRequired)
            return $"<{name}>";
        else
            return $"[{name}]";
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
    /// Checks if this argument can accept the value at the given position
    /// </summary>
    public bool CanAcceptValueAtPosition(int position)
    {
        if (IsArray)
        {
            // Array arguments can accept values at their position and beyond
            return position >= Position;
        }
        else
        {
            // Non-array arguments accept only at their exact position
            return position == Position;
        }
    }

    /// <summary>
    /// Converts a string value to the appropriate type for this argument
    /// </summary>
    public object? ConvertValue(string value, ICommandTypeParserCollection typeParserCollection)
    {
        if (string.IsNullOrEmpty(value))
            return null;

        var targetType = IsArray ? ElementType! : PropertyType;
        
        // First validate against valid values if specified
        if (ValidValues?.Length > 0)
        {
            var comparisonType = IsCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            var isValid = ValidValues.Any(validValue => 
                string.Equals(validValue?.ToString(), value, comparisonType));

            if (!isValid)
            {
                var validValuesString = string.Join(", ", ValidValues.Select(v => v?.ToString()));
                throw new ArgumentException(
                    $"Invalid value '{value}' for argument '{DisplayName}'. " +
                    $"Valid values are: {validValuesString}");
            }
        }

        // Use the type parser collection to convert the value
        if (typeParserCollection.TypeParsers.TryGetValue(targetType, out var parser))
        {
            var result = parser.Parse(value, out var error);
            if (error != null)
                throw new ArgumentException($"Invalid value '{value}' for argument '{DisplayName}': {error}");
            return result;
        }

        // Fallback to built-in conversion
        if (targetType == typeof(string))
            return value;

        if (targetType.IsEnum)
            return Enum.Parse(targetType, value, !IsCaseSensitive);

        try
        {
            return Convert.ChangeType(value, targetType);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Cannot convert '{value}' to {targetType.Name} for argument '{DisplayName}': {ex.Message}");
        }
    }

    /// <summary>
    /// Returns a string representation of the argument
    /// </summary>
    public override string ToString()
    {
        return $"{DisplayName} (position {Position})";
    }
}
