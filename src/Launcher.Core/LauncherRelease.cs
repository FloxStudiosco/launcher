using System.Runtime.Serialization;

namespace FloxStudios.Launcher.Core;

[DataContract]
public sealed class LauncherRelease
{
    private string? _version;
    private string? _url;
    private string? _sha256;

    [DataMember(Name = "version", Order = 0)]
    public string Version
    {
        get => _version ?? "";
        set => _version = value;
    }

    [DataMember(Name = "url", Order = 1)]
    public string Url
    {
        get => _url ?? "";
        set => _url = value;
    }

    [DataMember(Name = "size", Order = 2)]
    public long Size { get; set; }

    [DataMember(Name = "sha256", Order = 3)]
    public string Sha256
    {
        get => _sha256 ?? "";
        set => _sha256 = value;
    }
}
