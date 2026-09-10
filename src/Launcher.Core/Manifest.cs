using System.Runtime.Serialization;

namespace FloxStudios.Launcher.Core;

[DataContract]
public sealed class Manifest
{
    private GameRelease? _game;

    [DataMember(Name = "game", Order = 0)]
    public GameRelease Game
    {
        get => _game ??= new GameRelease();
        set => _game = value;
    }

    [DataMember(Name = "launcher", Order = 1, EmitDefaultValue = false)]
    public LauncherRelease? Launcher { get; set; }

    [DataMember(Name = "screenshot", Order = 2, EmitDefaultValue = false)]
    public string? Screenshot { get; set; }

    [DataMember(Name = "changelog", Order = 3, EmitDefaultValue = false)]
    public string? Changelog { get; set; }
}
