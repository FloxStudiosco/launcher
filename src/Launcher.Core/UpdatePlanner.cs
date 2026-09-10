using System;
using System.Collections.Generic;

namespace FloxStudios.Launcher.Core;

public static class UpdatePlanner
{
    public static UpdatePlan Plan(Manifest manifest, IReadOnlyDictionary<string, InstalledFile> local)
    {
        var toFetch = new Dictionary<string, ManifestFile>(StringComparer.Ordinal);
        var toPlace = new List<ManifestFile>();
        var unchanged = new List<InstalledFile>();
        var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ManifestFile file in manifest.Game.Files)
        {
            wanted.Add(file.Path);
            if (local.TryGetValue(file.Path, out InstalledFile? present) && present.Sha256 == file.Sha256)
            {
                unchanged.Add(present);
                continue;
            }
            toPlace.Add(file);
            if (!toFetch.ContainsKey(file.Sha256))
            {
                toFetch.Add(file.Sha256, file);
            }
        }
        var toDelete = new List<string>();
        foreach (string path in local.Keys)
        {
            if (!wanted.Contains(path))
            {
                toDelete.Add(path);
            }
        }
        return new UpdatePlan(new List<ManifestFile>(toFetch.Values), toPlace, toDelete, unchanged);
    }
}
