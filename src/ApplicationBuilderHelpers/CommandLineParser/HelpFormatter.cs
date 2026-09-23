using ApplicationBuilderHelpers.Interfaces;
using System.Collections.Generic;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>
/// Formats help output: <see cref="HelpContentProvider"/>
/// emits a typed <see cref="HelpModel"/>, <see cref="HelpLayoutRenderer"/>
/// lays it out. This class holds no content and no layout itself, only the
/// Theme/HelpWidth values passed from the builder to the renderer.
/// </summary>
internal class HelpFormatter(
    ICommandBuilder commandBuilder,
    SubCommandInfo? rootCommand,
    Dictionary<string, SubCommandInfo> allCommands,
    ConsoleOutput? consoleOutput = null)
{
    private readonly ICommandBuilder _commandBuilder = commandBuilder;
    private readonly HelpContentProvider _contentProvider = new(commandBuilder, rootCommand, allCommands);

    internal ConsoleOutput ConsoleOutput { get; } = consoleOutput ?? new ConsoleOutput();

    private HelpLayoutRenderer? _layoutRenderer;
    private HelpLayoutRenderer LayoutRenderer => _layoutRenderer ??= new(ConsoleOutput);

    public void ShowGlobalHelp()
    {
        var theme = _commandBuilder.Theme;
        var helpWidth = _commandBuilder.HelpWidth ?? 120;

        var model = _contentProvider.BuildGlobalModel();
        LayoutRenderer.Render(model, theme, helpWidth);
    }

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
