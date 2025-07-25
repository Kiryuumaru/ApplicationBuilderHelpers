﻿using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;

namespace ApplicationBuilderHelpers.ParserTypes;

public class FloatTypeParser : ICommandTypeParser
{
    public Type Type => typeof(float);

    public string[] Choices { get; } = [];

    public object? ParseToType(object? value)
    {
        var valueStr = value?.ToString();
        if (valueStr == null || string.IsNullOrEmpty(valueStr))
        {
            return default(float);
        }
        return float.Parse(valueStr);
    }

    public object? ParseFromType(object? value)
    {
        if (value == null || value is not float)
        {
            return default(float).ToString();
        }
        return value.ToString();
    }

    public bool Validate(object? value, [NotNullWhen(false)] out string? validateError)
    {
        validateError = null;
        if (value == null || value is not string valueStr || string.IsNullOrEmpty(valueStr))
        {
            return true;
        }
        if (!float.TryParse(valueStr, out float _))
        {
            validateError = "Value must be a float.";
            return false;
        }
        return true;
    }
}
