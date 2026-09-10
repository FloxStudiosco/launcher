using System.Collections.Generic;
using System.Runtime.Serialization;

namespace FloxStudios.Launcher.Core;

[DataContract]
public sealed class InstalledState
{
    private string? _version;
    private string? _exe;
    private List<InstalledFile>? _files;

    [DataMember(Name = "version", Order = 0)]
    public string Version
    {
        get => _version ?? "";
        set => _version = value;
    }

    [DataMember(Name = "exe", Order = 1)]
    public string Exe
    {
        get => _exe ?? "";
        set => _exe = value;
    }

    [DataMember(Name = "files", Order = 2)]
    public List<InstalledFile> Files
    {
        get => _files ??= new List<InstalledFile>();
        set => _files = value;
    }
}
