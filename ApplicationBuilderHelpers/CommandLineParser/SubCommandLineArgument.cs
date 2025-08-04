using System;

internal class SubCommandLineArgument(Type type)
{
    public string? Name { get; set; }

    public string? Description { get; set; }

    public int Position { get; set; }

    public bool Required { get; set; } = false;

    public object[] FromAmong { get; set; } = [];

    public bool CaseSensitive { get; set; } = false;

    public Type Type { get; set; } = type;
}
