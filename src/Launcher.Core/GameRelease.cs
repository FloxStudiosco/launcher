using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FloxStudios.Launcher.Core;

[DataContract]
public sealed class GameRelease
{
    private string? _version;
    private string? _minVersion;
    private string? _exe;
    private List<ManifestFile>? _files;

    [DataMember(Name = "version", Order = 0)]
    public string Version
    {
        get => _version ?? "";
        set => _version = value;
    }

    [DataMember(Name = "minVersion", Order = 1)]
    public string MinVersion
    {
        get => _minVersion ?? "";
        set => _minVersion = value;
    }

    [DataMember(Name = "exe", Order = 2)]
    public string Exe
    {
        get => _exe ?? "";
        set => _exe = value;
    }

    [DataMember(Name = "files", Order = 3)]
    public List<ManifestFile> Files
    {
        get => _files ??= new List<ManifestFile>();
        set => _files = value;
    }
}
