using System.Collections.Generic;

namespace FloxStudios.Launcher.Core;

public sealed class GameRelease
{
    public string Version { get; set; } = "";
    public string MinVersion { get; set; } = "";
    public string Exe { get; set; } = "";
    public List<ManifestFile> Files { get; set; } = new List<ManifestFile>();
}
