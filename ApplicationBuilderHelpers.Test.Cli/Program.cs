using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Test.Cli;
using ApplicationBuilderHelpers.Test.Cli.Commands;

return await ApplicationBuilder.Create()
    .AddCommand<BuildCommand>()
    .AddCommand<ConfigGetCommand>()
    .AddCommand<ConfigSetCommand>()
    .AddCommand<DatabaseMigrateCommand>()
    .AddCommand<DeployCommand>()
    .AddCommand<EnumLimitedCommand>()
    .AddCommand<EnumTestCommand>()
    .AddCommand<MainCommand>()
    .AddCommand<PluginCommand>()
    .AddCommand<RemoteAddCommand>()
    .AddCommand<ServeCommand>()
    .AddCommand<TestCommand>()
    .AddApplication<Presentation>()
    .RunAsync(args);
