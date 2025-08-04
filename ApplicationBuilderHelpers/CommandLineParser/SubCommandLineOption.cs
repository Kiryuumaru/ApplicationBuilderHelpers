using System;

internal class SubCommandLineOption(Type type)
{
    public string? Term { get; set; }

    public char? ShortTerm { get; set; }

    public string? EnvironmentVariable { get; set; }

    public bool Required { get; set; }

    public string? Description { get; set; }

    public object[] FromAmong { get; set; } = [];

    public bool CaseSensitive { get; set; } = false;

    public Type Type { get; set; } = type;
}
