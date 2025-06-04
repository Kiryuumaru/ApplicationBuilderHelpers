using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace ApplicationBuilderHelpers.ParserTypes;

[method: UnconditionalSuppressMessage("AOT", "IL3050:Calling members annotated with 'RequiresDynamicCodeAttribute' may break functionality when AOT compiling.", Justification = "<Pending>")]
public class EnumTypeParser(Type enumType, bool caseSensitive) : ICommandTypeParser
{
    public Type Type => throw new NotImplementedException();

    public bool ChoicesCaseSensitive { get; } = caseSensitive;

    public string[] Choices { get; } = [.. Enum.GetValues(enumType)
        .Cast<object>()
        .Select(i => i?.ToString()!)
        .Where(i => !string.IsNullOrEmpty(i))];

    private readonly Dictionary<string, object> enumValues = Enum.GetValues(enumType)
        .Cast<object>()
        .ToDictionary(i => i?.ToString()!);

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return Enum.GetValues(enumType).GetValue(0);
        }
        foreach (var enumValue in enumValues)
        {
            if (enumValue.Key.Equals(valueStr, ChoicesCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase))
            {
                return enumValue.Value;
            }
        }
        throw new ArgumentException($"Enum unexpected value '{value}'");
    }

    public object? ParseFromType(object? value)
    {
        if (value == null)
        {
            return null;
        }
        if (value is Enum)
        {
            return value.ToString();
        }
        throw new ArgumentException("Value must be an enum.");
    }

    public bool Validate(object? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        var valueStr = value?.ToString();
        if (string.IsNullOrEmpty(valueStr))
        {
            return true;
        }
        foreach (var enumValue in enumValues)
        {
            if (enumValue.Key.Equals(valueStr, ChoicesCaseSensitive ? StringComparison.InvariantCulture : StringComparison.InvariantCultureIgnoreCase))
            {
                return true;
            }
        }
        validateError = $"Must be one of:\n\t{string.Join("\n\t", Choices)}.";
        return false;
    }
}
