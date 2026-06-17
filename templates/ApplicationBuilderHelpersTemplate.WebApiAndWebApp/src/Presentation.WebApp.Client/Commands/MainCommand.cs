using Application.Client.Shared.Extensions;
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Presentation.WebApp.Client.Components.Extensions;
using Presentation.WebApp.Client.Extensions;
using Presentation.WebApp.Client.Services;

namespace Presentation.WebApp.Client.Commands;

[Command("Main subcommand.")]
internal class MainCommand : Build.BaseCommand<WebAssemblyHostBuilderWrapper>
{
    protected override ValueTask<WebAssemblyHostBuilderWrapper> ApplicationBuilder(CancellationToken stoppingToken)
    {
        var builder = new WebAssemblyHostBuilderWrapper(WebAssemblyHostBuilder.CreateDefault());

        return new ValueTask<WebAssemblyHostBuilderWrapper>(builder);
    }

    public override void AddConfigurations(ApplicationHostBuilder applicationBuilder, IConfiguration configuration)
    {
        base.AddConfigurations(applicationBuilder, configuration);

        configuration.SetApiEndpoint(new Uri(((WebAssemblyHostBuilderWrapper)applicationBuilder.Builder).BaseAddress));
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddClientRenderStateServices();

        services.AddScoped<AuthenticationStateProvider, BlazorAuthStateProvider>();

        services.AddAuthorizationCore();

        services.AddClientComponents();
    }
}
