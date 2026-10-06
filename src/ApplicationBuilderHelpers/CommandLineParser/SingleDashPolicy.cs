using System;

namespace ApplicationBuilderHelpers.CommandLineParser;

/// <summary>Single-dash reserved-word gate: <c>-help</c> and <c>-version</c> are full-token unknowns, never clusters; exact-only.</summary>
/// <remarks>Single definition shared by <c>ArgumentParser</c> cluster guards and <c>HelpVersionGateway</c> cluster exclusions, so <c>-helpful</c> still clusters normally.</remarks>
internal static class SingleDashPolicy
{
    internal static bool IsReservedWord(string token)
    {
        return string.Equals(token, "-help", StringComparison.Ordinal)
            || string.Equals(token, "-version", StringComparison.Ordinal);
    }
}
