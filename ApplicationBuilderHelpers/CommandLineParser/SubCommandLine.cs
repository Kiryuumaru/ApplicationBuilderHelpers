using ApplicationBuilderHelpers.Interfaces;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

internal class SubCommandLine
{
    public string? Term { get; set; }

    public string? Description { get; set; }

    internal List<SubCommandLineArgument> PositionalArgs { get; } = [];

    internal Dictionary<string, SubCommandLineOption> Options { get; } = [];

    internal Dictionary<string, SubCommandLine> SubCommands { get; } = [];

    internal Dictionary<Type, ICommandTypeParser> TypeParsers { get; } = [];

    internal Task<int> Parse(string[] args, CancellationToken cancellationToken = default)
    {
        // Implementation of the command execution logic goes here.
        // This is a placeholder for the actual command execution logic.
        return Task.FromResult(0);
    }
}
