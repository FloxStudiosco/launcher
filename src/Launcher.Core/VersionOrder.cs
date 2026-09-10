using System;

namespace FloxStudios.Launcher.Core;

public static class VersionOrder
{
    public static int Compare(string? left, string? right)
    {
        Version? a = Parse(left);
        Version? b = Parse(right);
        if (a == null && b == null)
            return 0;
        if (a == null)
            return -1;
        if (b == null)
            return 1;
        return a.CompareTo(b);
    }

    public static bool IsOlder(string? candidate, string? reference)
    {
        return Compare(candidate, reference) < 0;
    }

    private static Version? Parse(string? text)
    {
        if (text == null || text.Trim().Length == 0)
            return null;
        string core = text.Trim().Split('-', '+')[0];
        if (core.IndexOf('.') < 0)
        {
            core += ".0";
        }
        return Version.TryParse(core, out Version? version) ? version : null;
    }
}
