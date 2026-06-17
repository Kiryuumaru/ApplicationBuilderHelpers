using Application;
using Application.Server;
using Domain.Server;
using Infrastructure.EFCore;
using Infrastructure.EFCore.LocalStore;
using Infrastructure.EFCore.Server.Identity;
using Infrastructure.EFCore.Sqlite;
using Infrastructure.EFCore.Sqlite.Server;
using Infrastructure.Mock;
using Infrastructure.OpenTelemetry;
using Infrastructure.Server.Identity;
using Infrastructure.Server.Passkeys;
using Presentation.WebApp.Server.Commands;

return await ApplicationBuilderHelpers.ApplicationBuilder.Create()
    .AddApplication<Domain.Domain>()
    .AddApplication<ServerDomain>()
    .AddApplication<Application.Application>()
    .AddApplication<ServerApplication>()
    .AddApplication<OpenTelemetryInfrastructure>()
    .AddApplication<IdentityInfrastructure>()
    .AddApplication<PasskeysInfrastructure>()
    .AddApplication<MockInfrastructure>()
    .AddApplication<EFCoreInfrastructure>()
    .AddApplication<EFCoreLocalStoreInfrastructure>()
    .AddApplication<EFCoreServerIdentityInfrastructure>()
    .AddApplication<EFCoreSqliteInfrastructure>()
    .AddApplication<EFCoreSqliteServerInfrastructure>()
    .AddCommand<MainCommand>()
    .RunAsync(args);
