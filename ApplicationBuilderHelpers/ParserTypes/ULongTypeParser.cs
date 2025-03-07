using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Text;

namespace ApplicationBuilderHelpers.ParserTypes;

public class ShortTypeParser : ICommandLineTypeParser
{
    public Type Type => throw new NotImplementedException();

    public string[] Choices()
    {
        throw new NotImplementedException();
    }

    public object? Parse(string? value)
    {
        throw new NotImplementedException();
    }

    public void Validate(string? value)
    {
        throw new NotImplementedException();
    }
}
