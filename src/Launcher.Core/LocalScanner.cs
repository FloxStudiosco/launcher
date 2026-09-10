using System;
using System.Collections.Generic;
using System.IO;

namespace FloxStudios.Launcher.Core;

public static class LocalScanner
{
    public static Dictionary<string, InstalledFile> Scan(InstallLayout layout, InstalledState? cache)
    {
        var result = new Dictionary<string, InstalledFile>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(layout.GameDirectory))
            return result;
        Dictionary<string, InstalledFile> known = Index(cache);
        foreach (string fullPath in Directory.EnumerateFiles(layout.GameDirectory, "*", SearchOption.AllDirectories))
        {
            string relative = FileOps.RelativePath(layout.GameDirectory, fullPath);
            result[relative] = Describe(fullPath, relative, known);
        }
        return result;
    }

    private static Dictionary<string, InstalledFile> Index(InstalledState? cache)
    {
        var index = new Dictionary<string, InstalledFile>(StringComparer.OrdinalIgnoreCase);
        if (cache == null)
            return index;
        foreach (InstalledFile file in cache.Files)
        {
            index[file.Path] = file;
        }
        return index;
    }

    private static InstalledFile Describe(string fullPath, string relative, Dictionary<string, InstalledFile> known)
    {
        var info = new FileInfo(fullPath);
        long ticks = info.LastWriteTimeUtc.Ticks;
        if (known.TryGetValue(relative, out InstalledFile? cached) && cached.Size == info.Length && cached.LastWriteTicks == ticks)
            return cached;
        return new InstalledFile
        {
            Path = relative,
            Size = info.Length,
            LastWriteTicks = ticks,
            Sha256 = FileHash.OfFile(fullPath),
        };
    }
}
