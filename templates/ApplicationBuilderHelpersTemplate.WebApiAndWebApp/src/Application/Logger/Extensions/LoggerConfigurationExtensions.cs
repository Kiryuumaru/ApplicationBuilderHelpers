using Application.Shared.Extensions;
using ApplicationBuilderHelpers.Extensions;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace Application.Logger.Extensions;

public static class LoggerConfigurationExtensions
{
    private const string ApplyThemeWhenOutputIsRedirectedKey = "RUNTIME_APPLYTHEMEWHENOUTPUTISREDIRECTED";

    public static bool GetApplyThemeWhenOutputIsRedirected(this IConfiguration configuration)
    {
        return configuration.GetBooleanOrDefault(ApplyThemeWhenOutputIsRedirectedKey);
    }

    public static void SetApplyThemeWhenOutputIsRedirected(this IConfiguration configuration, bool applyThemeWhenOutputIsRedirected)
    {
        configuration.SetBoolean(ApplyThemeWhenOutputIsRedirectedKey, applyThemeWhenOutputIsRedirected);
    }
}
