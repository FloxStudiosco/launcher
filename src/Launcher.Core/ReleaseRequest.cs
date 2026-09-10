namespace FloxStudios.Launcher.Core;

public sealed class ReleaseRequest
{
    public string BuildDirectory { get; set; } = "";
    public string SiteDirectory { get; set; } = "";
    public string Version { get; set; } = "";
    public string? MinVersion { get; set; }
    public string Exe { get; set; } = "";
    public string? Screenshot { get; set; }
    public string? Changelog { get; set; }
    public string? LauncherExe { get; set; }
    public string? LauncherVersion { get; set; }
}
