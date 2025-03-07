using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

public class IntTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(int);

    public string[] Choices()
    {
        return [];
    }

    public object? Parse(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return 0;
        }
        return int.Parse(value!);
    }

    public void Validate(string? value)
    {
        if (value == null || string.IsNullOrEmpty(value))
        {
            return;
        }
        else if (!int.TryParse(value, out int _))
        {
            throw new ArgumentException("Invalid integer value.");
        }
    }
}
