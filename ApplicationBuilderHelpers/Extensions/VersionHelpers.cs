using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationBuilderHelpers.Extensions;     

internal static class VersionHelpers
{
    internal static string RemoveHash(string version)
    {
        int plusIndex = version.IndexOf('+');
        if (plusIndex == -1)
            return version; // No build metadata

        string baseVersion = version[..plusIndex];
        string metadata = version[(plusIndex + 1)..];

        string[] parts = metadata.Split('.');

        // Check if the last part is a hex hash (length 40 or 64, all hex chars)
        string last = parts.Last();
        if (IsHex(last) && (last.Length == 40 || last.Length == 64))
        {
            parts = [.. parts.Take(parts.Length - 1)];
        }

        return parts.Length > 0 ? $"{baseVersion}+{string.Join(".", parts)}" : baseVersion;
    }

    static bool IsHex(string s)
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
