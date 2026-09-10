using System.Collections.Generic;

namespace FloxStudios.Launcher.Core;

public sealed class InstalledState
{
    public string Version { get; set; } = "";
    public string Exe { get; set; } = "";
    public List<InstalledFile> Files { get; set; } = new List<InstalledFile>();
}
