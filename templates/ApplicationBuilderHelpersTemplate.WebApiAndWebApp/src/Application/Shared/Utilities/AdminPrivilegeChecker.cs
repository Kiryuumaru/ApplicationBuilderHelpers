using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using System.Security.Principal;

namespace Application.Shared.Utilities;

public static class AdminPrivilegeChecker
{
    public static void RequireAdminPrivileges()
    {
        if (!IsRunningAsAdmin())
        {
            string osType = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "Windows" : "Linux/macOS";
            string requiredPrivilege = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? "administrator" : "sudo";

            throw new UnauthorizedAccessException(
                $"This application must be run as {requiredPrivilege} on {osType}. " +
                $"Please restart the application with elevated privileges.");
        }
    }

    public static bool IsRunningAsAdmin()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return IsWindowsAdmin();
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux) ||
                 RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return IsUnixSudo();
        }

        // Unknown platform - assume no admin privileges
        return false;
    }

    [SupportedOSPlatform("Windows")]
    private static bool IsWindowsAdmin()
    {
        try
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }
        catch
        {
            return false;
        }
    }

    private static bool IsUnixSudo()
    {
        try
        {
            // Check if EUID (Effective User ID) is 0 (root)
            return Environment.GetEnvironmentVariable("USER") == "root" ||
                   Environment.GetEnvironmentVariable("SUDO_USER") != null ||
                   GetEffectiveUserId() == 0;
        }
        catch
        {
            return false;
        }
    }

    [DllImport("libc", SetLastError = true)]
    private static extern uint geteuid();

    private static uint GetEffectiveUserId()
    {
        try
        {
            return geteuid();
        }
        catch
        {
            return uint.MaxValue; // Return non-zero value if call fails
        }
    }
}
