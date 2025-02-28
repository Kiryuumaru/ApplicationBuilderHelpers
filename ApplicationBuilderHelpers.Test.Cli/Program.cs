using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Test.Cli;
using ApplicationBuilderHelpers.Test.Cli.Commands;

return await ApplicationBuilder.Create()
    .SetExecutableName("test.exe")
    .AddCommand<MainCommand>()
    .AddCommand<SubCommand>()
    .AddCommand<SubCommand2>()
    .AddCommand<SubCommand3>()
    .AddApplication<Presentation>()
    .RunAsync(args);
