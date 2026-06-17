namespace Application.Server.Identity.Models;

/// <summary>
/// Device information extracted from HTTP context for session creation.
/// </summary>
public sealed record SessionDeviceInfo(
    string? DeviceName,
    string? UserAgent,
    string? IpAddress);
