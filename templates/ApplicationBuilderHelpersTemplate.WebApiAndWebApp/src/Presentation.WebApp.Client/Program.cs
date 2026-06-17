using Application;
using Application.Client;
using Application.Shared.Interfaces.Inbound;
using Application.Shared.Interfaces.Outbound;
using Domain.Client;
using Infrastructure.Browser.IndexedDB.LocalStore;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Presentation.WebApp.Client.Commands;

try
{
    return await ApplicationBuilderHelpers.ApplicationBuilder.Create()
        .AddApplication<Domain.Domain>()
        .AddApplication<ClientDomain>()
        .AddApplication<Application.Application>()
        .AddApplication<ClientApplication>()
        .AddApplication<IndexedDBLocalStoreInfrastructure>()
        .AddCommand<MainCommand>()
        .RunAsync(args);

}
catch (Exception ex)
{
    Console.Error.WriteLine($"An error occurred: {ex.Message}, StackTrace: {ex.StackTrace}");
    return -1;
}
