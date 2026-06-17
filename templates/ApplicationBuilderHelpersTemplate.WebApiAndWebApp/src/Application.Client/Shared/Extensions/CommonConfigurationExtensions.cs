using AbsolutePathHelpers;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;

namespace Application.Client.Shared.Extensions;

public static class CommonConfigurationExtensions
{
    private const string ApiEndpointKey = "RUNTIME_API_ENDPOINT";
    public static Uri GetApiEndpoint(this IConfiguration configuration)
    {
        return new Uri(configuration.GetRefValue(ApiEndpointKey));
    }
    public static void SetApiEndpoint(this IConfiguration configuration, Uri apiEndpoint)
    {
        configuration[ApiEndpointKey] = apiEndpoint.ToString();
    }
}
