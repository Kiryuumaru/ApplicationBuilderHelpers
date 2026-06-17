using Application.Shared.Interfaces;
using Application.Shared.Interfaces.Inbound;
using ApplicationBuilderHelpers;
using ApplicationBuilderHelpers.Attributes;
using Asp.Versioning;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi;
using Presentation.WebApi.Filters;
using Scalar.AspNetCore;
using System.Reflection;

namespace Presentation.WebApi.Commands;

[Command("Main subcommand.")]
internal sealed class MainCommand : Build.BaseCommand<WebApplicationBuilder>
{
    [CommandOption("urls", Description = "Server listening URLs (semicolon-separated)", EnvironmentVariable = "ASPNETCORE_URLS")]
    public string? Urls { get; set; }

    protected override ValueTask<WebApplicationBuilder> ApplicationBuilder(CancellationToken stoppingToken)
    {
        var builder = WebApplication.CreateBuilder();
        
        // Configure URLs if specified
        if (!string.IsNullOrEmpty(Urls))
        {
            builder.WebHost.UseUrls(Urls.Split(';', StringSplitOptions.RemoveEmptyEntries));
        }
        
        return new ValueTask<WebApplicationBuilder>(builder);
    }

    public override void AddServices(ApplicationHostBuilder applicationBuilder, IServiceCollection services)
    {
        base.AddServices(applicationBuilder, services);

        services.AddScoped<ApiExceptionFilter>();
        services.AddScoped<Controllers.V1.Auth.Shared.AuthResponseFactory>();

        services.AddHttpClient(Options.DefaultName, (sp, client) =>
        {
            var applicationConstants = sp.GetRequiredService<IApplicationConstants>();
            client.DefaultRequestHeaders.Add("Client-Agent", applicationConstants.AppName);
        });

        services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), ["live"]);

        services.AddServiceDiscovery();

        services.ConfigureHttpClientDefaults(http =>
        {
            // Turn on service discovery by default
            http.AddServiceDiscovery();
        });

        services.AddProblemDetails();
        services.AddOpenApi(options =>
        {
            options.ShouldInclude = _ => true;
        });

        // Add API Versioning
        services
            .AddApiVersioning(options =>
            {
                options.ApiVersionReader = ApiVersionReader.Combine(
                    new UrlSegmentApiVersionReader()
                );
                options.DefaultApiVersion = new ApiVersion(1, 0);
                options.AssumeDefaultVersionWhenUnspecified = true;
                options.ApiVersionSelector = new CurrentImplementationApiVersionSelector(options);
            })
            .AddApiExplorer(options =>
            {
                options.GroupNameFormat = "'v'VVV";
                options.SubstituteApiVersionInUrl = true;
            });

        services.AddControllers(options =>
        {
            options.ModelBinderProviders.Insert(0, new ModelBinders.FromJwtModelBinderProvider());
            options.Filters.AddService<ApiExceptionFilter>();
        });

        // Add Authentication
        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer();

        // Add Authorization
        services.AddAuthorization();

        // Add SignalR
        services.AddSignalR();

        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Version = "v1",
                Title = Build.Constants.AppTitle,
                Description = Build.Constants.AppDescription
            });

            // Add JWT Authentication to Swagger
            options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter your JWT token in the format: Bearer {your token}"
            });

            options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
            {
                [new OpenApiSecuritySchemeReference("Bearer", document)] = []
            });

            // Add generic filters that work with AllowedValuesAttribute
            options.SchemaFilter<AllowedValuesSchemaFilter>();

            options.EnableAnnotations();

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            options.IncludeXmlComments(xmlPath, true);
        });
    }

    public override void AddMappings(ApplicationHost applicationHost, IHost host)
    {
        base.AddMappings(applicationHost, host);

        var app = (WebApplication)host;

        app.UseExceptionHandler();

        if (app.Environment.IsDevelopment())
        {
            app.MapHealthChecks("/health");
            app.MapHealthChecks("/alive", new HealthCheckOptions
            {
                Predicate = r => r.Tags.Contains("live")
            });
        }

        app.MapControllers();

        app.MapOpenApi();

        app.MapScalarApiReference(options =>
        {
            options.EnablePersistentAuthentication();
            options.AddPreferredSecuritySchemes("Bearer");
            options.WithTitle(Build.Constants.AppTitle);
            options.WithTheme(ScalarTheme.DeepSpace);
            options.HideModels();

            options.WithOpenApiRoutePattern("swagger/{documentName}/swagger.json");
            options.WithDefaultHttpClient(ScalarTarget.Python, ScalarClient.Curl);

            options.WithCustomCss("""
                .show-api-client-button span {
                    display: none;
                }
                .show-api-client-button::after {
                    content: 'Run Request';
                }
                """);
        });

        app.MapGet("/", () => Results.Redirect("/scalar/v1"))
            .ExcludeFromDescription();
    }

    public override void AddMiddlewares(ApplicationHost applicationHost, IHost host)
    {
        base.AddMiddlewares(applicationHost, host);

        var app = (WebApplication)host;

        app.UseSwagger(options =>
        {
            options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1;
        });

        //app.UseHttpsRedirection();

        // Add Authentication and Authorization middleware
        app.UseAuthentication();
        app.UseAuthorization();
    }
}
