using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Test.Cli;
using ApplicationBuilderHelpers.Test.Cli.Commands;
using ApplicationBuilderHelpers.Themes;

return await ApplicationBuilder.Create()
    .SetExecutableName("test")
    .SetExecutableTitle("ApplicationBuilderHelpers Test CLI")
    .SetExecutableDescription("A comprehensive test CLI application for ApplicationBuilderHelpers with advanced command processing capabilities, subcommands, options, and arguments testing.")
    .SetHelpWidth(120)
    .SetTheme(VSCodeDarkTheme.Instance)
    .AddCommand<BuildCommand>()
    .AddCommand<ConfigGetCommand>()
    .AddCommand<ConfigSetCommand>()
    .AddCommand<DatabaseMigrateCommand>()
    .AddCommand<DeployCommand>()
    .AddCommand<MainCommand>()
    .AddCommand<PluginCommand>()
    .AddCommand<RemoteAddCommand>()
    .AddCommand<ServeCommand>()
    .AddCommand<TestCommand>()
    .AddApplication<Presentation>()
    .RunAsync(args);
