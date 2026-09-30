using System.Linq;
using System.Reflection;

namespace ApplicationBuilderHelpers.Extensions;

/// <summary>Assembly-metadata fallback for unset builder values.</summary>
internal static class AssemblyHelpers
{
    /// <summary>Informational version without trailing commit hash.</summary>
    internal static string GetAutoDetectedVersion()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
        var assemblyInformationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? "0.0.0";
        return RemoveVersionHash(assemblyInformationalVersion);
    }

    /// <summary>Assembly name, or "app" when blank.</summary>
    internal static string GetAutoDetectedExecutableName()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

        var assemblyName = assembly.GetName().Name;
        if (!string.IsNullOrEmpty(assemblyName))
        {
            return assemblyName;
        }

        return "app";
    }

    /// <summary>Assembly title, falling back to the detected executable name.</summary>
    internal static string GetAutoDetectedExecutableTitle()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

        var assemblyTitle = assembly.GetCustomAttribute<AssemblyTitleAttribute>()?.Title;
        if (!string.IsNullOrEmpty(assemblyTitle))
        {
            return assemblyTitle;
        }

        return GetAutoDetectedExecutableName();
    }

    /// <summary>Assembly description, falling back to a default naming the executable.</summary>
    internal static string GetAutoDetectedExecutableDescription()
    {
        var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();

        var assemblyDescription = assembly.GetCustomAttribute<AssemblyDescriptionAttribute>()?.Description;
        if (!string.IsNullOrEmpty(assemblyDescription))
        {
            return assemblyDescription;
        }

        return $"Command line application {GetAutoDetectedExecutableName()}";
    }

    /// <summary>Strips a trailing commit-hash metadata segment, keeping other suffixes.</summary>
    private static string RemoveVersionHash(string version)
    {
        int plusIndex = version.IndexOf('+');
        if (plusIndex == -1)
            return version;

        string baseVersion = version[..plusIndex];
        string metadata = version[(plusIndex + 1)..];

        string[] parts = metadata.Split('.');

        string last = parts.Last();
        if (IsHex(last) && (last.Length == 40 || last.Length == 64))
        {
            parts = [.. parts.Take(parts.Length - 1)];
        }

        return parts.Length > 0 ? $"{baseVersion}+{string.Join(".", parts)}" : baseVersion;
    }

    /// <summary>True when every character is a hex digit.</summary>
    private static bool IsHex(string s)
    {
        foreach (char c in s)
        {
            if (!((c >= '0' && c <= '9') ||
                  (c >= 'a' && c <= 'f') ||
                  (c >= 'A' && c <= 'F')))
                return false;
        }
        return true;
    }
}
