namespace FloxStudios.Launcher.Core;

public sealed class InstalledFile
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public long LastWriteTicks { get; set; }
    public string Sha256 { get; set; } = "";
}
