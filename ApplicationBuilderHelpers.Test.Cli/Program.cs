using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Test.Cli;
using ApplicationBuilderHelpers.Test.Cli.Commands;

return await ApplicationBuilder.Create()
    .SetExecutableName("test")
    .SetExecutableTitle("ApplicationBuilderHelpers Test CLI")
    .SetExecutableDescription("A test CLI application for ApplicationBuilderHelpers")
    .AddCommand<MainCommand>()
    .AddApplication<Presentation>()
    .RunAsync(args);
