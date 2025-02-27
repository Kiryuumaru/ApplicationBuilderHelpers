using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Test.Cli.Commands;

return await ApplicationBuilder.Create()
    .AddCommand<MainCommand>()
    .AddCommand<SubCommand>()
    .AddCommand<SubCommand2>()
    .AddCommand<SubCommand3>()
    .RunAsync(args);
