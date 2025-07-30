using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Extensions;
using ApplicationBuilderHelpers.Test.Cli;
using ApplicationBuilderHelpers.Test.Cli.Commands;
using ApplicationBuilderHelpers.Themes;

// Example 1: Default width (80 characters total)
Console.WriteLine("=== Default Width (80 chars) ===");
await ApplicationBuilder.Create()
    .SetExecutableName("test")
    .SetExecutableTitle("ApplicationBuilderHelpers Test CLI")
    .SetExecutableDescription("A test CLI application for ApplicationBuilderHelpers with advanced command processing capabilities.")
    .SetExecutableVersion("2.1.0")
    .SetTheme(new MonokaiDimmedTheme())
    .AddCommand<MainCommand>()
    .AddCommand<TestCommand>()
    .AddCommand<BuildCommand>()
    .AddApplication<Presentation>()
    .RunAsync(["--help"]);

Console.WriteLine("\n" + new string('=', 80) + "\n");

// Example 2: Narrow width (60 characters total)
Console.WriteLine("=== Narrow Width (60 chars) ===");
await ApplicationBuilder.Create()
    .SetExecutableName("test")
    .SetExecutableTitle("ApplicationBuilderHelpers Test CLI")
    .SetExecutableDescription("A test CLI application for ApplicationBuilderHelpers.")
    .SetExecutableVersion("2.1.0")
    .SetTheme(new DraculaTheme())
    .SetHelpWidth(60)  // Total width = 60 chars, left column ≈ 24 chars
    .AddCommand<MainCommand>()
    .AddCommand<TestCommand>()
    .AddCommand<BuildCommand>()
    .AddApplication<Presentation>()
    .RunAsync(["--help"]);

Console.WriteLine("\n" + new string('=', 60) + "\n");

// Example 3: Wide width (120 characters total)
Console.WriteLine("=== Wide Width (120 chars) ===");
await ApplicationBuilder.Create()
    .SetExecutableName("test")
    .SetExecutableTitle("ApplicationBuilderHelpers Test CLI")
    .SetExecutableDescription("A comprehensive test CLI application for ApplicationBuilderHelpers with advanced command processing capabilities and extensive feature demonstration.")
    .SetExecutableVersion("2.1.0")
    .SetTheme(new NordTheme())
    .SetHelpWidth(120)  // Total width = 120 chars, left column ≈ 48 chars
    .AddCommand<MainCommand>()
    .AddCommand<TestCommand>()
    .AddCommand<BuildCommand>()
    .AddApplication<Presentation>()
    .RunAsync(["--help"]);

return 0;
