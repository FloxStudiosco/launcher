using System.Runtime.Serialization;

namespace FloxStudios.Launcher.Core;

[DataContract]
public sealed class ManifestFile
{
    private string? _path;
    private string? _sha256;

    [DataMember(Name = "path", Order = 0)]
    public string Path
    {
        get => _path ?? "";
        set => _path = value;
    }

    [DataMember(Name = "size", Order = 1)]
    public long Size { get; set; }

    [DataMember(Name = "packedSize", Order = 2)]
    public long PackedSize { get; set; }

    [DataMember(Name = "sha256", Order = 3)]
    public string Sha256
    {
        get => _sha256 ?? "";
        set => _sha256 = value;
    }
}
