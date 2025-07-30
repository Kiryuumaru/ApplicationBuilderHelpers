using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Test.Cli;
using ApplicationBuilderHelpers.Test.Cli.Commands;

return await ApplicationBuilder.Create()
    .SetExecutableName("test")
    .SetExecutableTitle("ApplicationBuilderHelpers Test CLI")
    .SetExecutableDescription("A test CLI application for ApplicationBuilderHelpers with advanced command processing capabilities.")
    .SetExecutableVersion("2.1.0")
    .AddCommand<MainCommand>()
    .AddCommand<TestCommand>()
    .AddCommand<BuildCommand>()
    .AddApplication<Presentation>()
    .RunAsync(args);
