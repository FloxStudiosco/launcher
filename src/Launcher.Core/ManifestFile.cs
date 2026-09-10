namespace FloxStudios.Launcher.Core;

public sealed class ManifestFile
{
    public string Path { get; set; } = "";
    public long Size { get; set; }
    public long PackedSize { get; set; }
    public string Sha256 { get; set; } = "";
}
