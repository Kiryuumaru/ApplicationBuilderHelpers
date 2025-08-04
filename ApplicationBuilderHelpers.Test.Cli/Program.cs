using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Test.Cli;
using ApplicationBuilderHelpers.Test.Cli.Commands;
using ApplicationBuilderHelpers.Themes;

return await ApplicationBuilder.Create()
    .SetExecutableName("test")
    .SetExecutableTitle("ApplicationBuilderHelpers Test CLI")
    .SetExecutableDescription("A comprehensive test CLI application for ApplicationBuilderHelpers with advanced command processing capabilities, subcommands, options, and arguments testing.")
    .AddCommand<MainCommand>()
    .AddCommand<BuildCommand>()
    .AddCommand<ConfigSetCommand>()
    .AddCommand<ConfigGetCommand>()
    .AddCommand<DeployCommand>()
    .AddCommand<DatabaseMigrateCommand>()
    .AddCommand<ServeCommand>()
    .AddCommand<RemoteAddCommand>()
    .AddCommand<PluginCommand>()
    .AddCommand<ServeCommand>()
    .AddCommand<TestCommand>()
    .AddApplication<Presentation>()
    .RunAsync(args);
