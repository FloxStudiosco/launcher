namespace FloxStudios.Launcher.Core;

public sealed class LauncherRelease
{
    public string Version { get; set; } = "";
    public string Url { get; set; } = "";
    public long Size { get; set; }
    public string Sha256 { get; set; } = "";
}
