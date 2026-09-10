using System.Collections.Generic;
using System.Linq;

namespace FloxStudios.Launcher.Core;

public sealed class UpdatePlan
{
    public UpdatePlan(
        IReadOnlyList<ManifestFile> objectsToFetch,
        IReadOnlyList<ManifestFile> filesToPlace,
        IReadOnlyList<string> filesToDelete,
        IReadOnlyList<InstalledFile> unchangedFiles)
    {
        ObjectsToFetch = objectsToFetch;
        FilesToPlace = filesToPlace;
        FilesToDelete = filesToDelete;
        UnchangedFiles = unchangedFiles;
    }

    public IReadOnlyList<ManifestFile> ObjectsToFetch { get; }
    public IReadOnlyList<ManifestFile> FilesToPlace { get; }
    public IReadOnlyList<string> FilesToDelete { get; }
    public IReadOnlyList<InstalledFile> UnchangedFiles { get; }
    public long DownloadBytes => ObjectsToFetch.Sum(file => file.PackedSize);
    public bool IsEmpty => FilesToPlace.Count == 0 && FilesToDelete.Count == 0;
}
