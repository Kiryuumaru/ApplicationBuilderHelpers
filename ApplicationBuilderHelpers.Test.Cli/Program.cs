using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Test.Cli;
using ApplicationBuilderHelpers.Test.Cli.Commands;
using ApplicationBuilderHelpers.Themes;

return await ApplicationBuilder.Create()
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
