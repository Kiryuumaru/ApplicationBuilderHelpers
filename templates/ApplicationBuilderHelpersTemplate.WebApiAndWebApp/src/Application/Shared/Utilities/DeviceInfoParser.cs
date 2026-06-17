namespace Application.Shared.Utilities;

public static class DeviceInfoParser
{
    public static string? ParseDeviceName(string? userAgent)
    {
        if (string.IsNullOrWhiteSpace(userAgent))
        {
            return null;
        }

        if (userAgent.Contains("Windows", StringComparison.OrdinalIgnoreCase))
        {
            if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                return "Chrome on Windows";
            if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                return "Firefox on Windows";
            if (userAgent.Contains("Edge", StringComparison.OrdinalIgnoreCase))
                return "Edge on Windows";
            return "Windows";
        }

        if (userAgent.Contains("Mac", StringComparison.OrdinalIgnoreCase))
        {
            if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                return "Chrome on Mac";
            if (userAgent.Contains("Safari", StringComparison.OrdinalIgnoreCase))
                return "Safari on Mac";
            if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                return "Firefox on Mac";
            return "Mac";
        }

        if (userAgent.Contains("Linux", StringComparison.OrdinalIgnoreCase))
        {
            if (userAgent.Contains("Chrome", StringComparison.OrdinalIgnoreCase))
                return "Chrome on Linux";
            if (userAgent.Contains("Firefox", StringComparison.OrdinalIgnoreCase))
                return "Firefox on Linux";
            return "Linux";
        }

        if (userAgent.Contains("iPhone", StringComparison.OrdinalIgnoreCase))
            return "iPhone";
        if (userAgent.Contains("iPad", StringComparison.OrdinalIgnoreCase))
            return "iPad";
        if (userAgent.Contains("Android", StringComparison.OrdinalIgnoreCase))
            return "Android";

        return "Unknown Device";
    }
}
