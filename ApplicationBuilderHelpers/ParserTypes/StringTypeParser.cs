﻿using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class StringTypeParser : ICommandTypeParser
{
    public Type Type => typeof(string);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        return value;
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not string)
        {
            return null;
        }
        return value.ToString();
    }

    public bool Validate(object? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        return true;
    }
}
