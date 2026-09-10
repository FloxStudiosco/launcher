namespace FloxStudios.Launcher.Core;

public sealed class Manifest
{
    public GameRelease Game { get; set; } = new GameRelease();
    public LauncherRelease? Launcher { get; set; }
    public string? Screenshot { get; set; }
    public string? Changelog { get; set; }
}
