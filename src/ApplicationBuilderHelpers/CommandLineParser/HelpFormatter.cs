using ApplicationBuilderHelpers.Interfaces;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Formats help output.
/// </summary>
internal class HelpFormatter(
    ICommandBuilder commandBuilder,
    SubCommandInfo? rootCommand,
    Dictionary<string, SubCommandInfo> allCommands,
    ConsoleOutput? consoleOutput = null)
{
    private readonly ICommandBuilder _commandBuilder = commandBuilder;
    private readonly HelpContentProvider _contentProvider = new(commandBuilder, rootCommand, allCommands);

    /// <summary>Console sink help renders through; defaults to the real console.</summary>
    internal ConsoleOutput ConsoleOutput { get; } = consoleOutput ?? new ConsoleOutput();

    private HelpLayoutRenderer? _layoutRenderer;
    private HelpLayoutRenderer LayoutRenderer => _layoutRenderer ??= new(ConsoleOutput);

    /// <summary>Renders global help at the default 120 width (floored at 60).</summary>
    public void ShowGlobalHelp()
    {
        var theme = _commandBuilder.Theme;
        var helpWidth = _commandBuilder.HelpWidth ?? 120;

        var model = _contentProvider.BuildGlobalModel();
        LayoutRenderer.Render(model, theme, helpWidth);
    }

    /// <summary>Renders help for the given command (root falls back to global).</summary>
    public void ShowCommandHelp(SubCommandInfo commandInfo)
    {
        var theme = _commandBuilder.Theme;
        var helpWidth = _commandBuilder.HelpWidth ?? 120;

        var model = commandInfo.IsRoot
            ? _contentProvider.BuildGlobalModel()
            : _contentProvider.BuildCommandModel(commandInfo);
        LayoutRenderer.Render(model, theme, helpWidth);
    }
}
