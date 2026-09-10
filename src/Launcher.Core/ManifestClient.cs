using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FloxStudios.Launcher.Core;

public static class ManifestClient
{
    public const string FILE_NAME = "manifest.json";

    public static async Task<Manifest> FetchAsync(IObjectSource source, CancellationToken cancellation)
    {
        string cacheBuster = DateTime.UtcNow.Ticks.ToString(CultureInfo.InvariantCulture);
        string json;
        using (Stream stream = await source.OpenAsync(FILE_NAME + "?t=" + cacheBuster, cancellation).ConfigureAwait(false))
        using (var reader = new StreamReader(stream))
        {
            json = await reader.ReadToEndAsync().ConfigureAwait(false);
        }
        Manifest manifest = LauncherJson.Deserialize<Manifest>(json);
        ManifestValidator.Validate(manifest);
        return manifest;
    }
}
