using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.ParserTypes;

internal class DoubleTypeParser : ICommandTypeParser
{
    public Type Type => typeof(double);

    public object? Parse(string? value, out string? validateError)
    {
        if (double.TryParse(value, out var result))
        {
            validateError = null;
            return result;
        }

        validateError = $"Invalid {Type.Name} value: '{value}'. Expected a valid {Type.Name}.";
        return null;
    }

    public string? GetString(object? value)
    {
        return value?.ToString();
    }
}
