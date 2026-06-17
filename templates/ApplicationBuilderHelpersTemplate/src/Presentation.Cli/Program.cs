using Infrastructure.InMemory;
using Presentation.Cli.Commands;

return await ApplicationBuilderHelpers.ApplicationBuilder.Create()
    .AddApplication<Domain.Domain>()
    .AddApplication<Application.Application>()
    .AddApplication<InMemoryInfrastructure>()
    .AddCommand<MainCommand>()
    .RunAsync(args);
