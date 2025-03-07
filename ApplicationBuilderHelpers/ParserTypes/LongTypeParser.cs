using ApplicationBuilderHelpers.Interfaces;
using System;

namespace ApplicationBuilderHelpers.ParserTypes;

public class LongTypeParser : ICommandLineTypeParser
{
    public Type Type => typeof(long);

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
        else if (!long.TryParse(value, out long _))
        {
            throw new ArgumentException("Invalid long value.");
        }
    }
}
