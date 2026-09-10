using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace FloxStudios.Launcher.Core;

public static class SelfUpdater
{
    public const string NEW_SUFFIX = ".new";
    public const string OLD_SUFFIX = ".old";

    private const int BUFFER_SIZE = 81920;

    public static bool IsUpdateAvailable(string currentVersion, LauncherRelease? release)
    {
        return release != null && VersionOrder.IsOlder(currentVersion, release.Version);
    }

    public static async Task<string> DownloadAsync(IObjectSource source, LauncherRelease release, string currentExe, CancellationToken cancellation)
    {
        string downloaded = currentExe + NEW_SUFFIX;
        using (Stream remote = await source.OpenAsync(release.Url, cancellation).ConfigureAwait(false))
        using (FileStream output = File.Create(downloaded))
        {
            await remote.CopyToAsync(output, BUFFER_SIZE, cancellation).ConfigureAwait(false);
        }
        string actual = FileHash.OfFile(downloaded);
        if (actual != release.Sha256)
        {
            File.Delete(downloaded);
            throw new InvalidDataException($"Launcher {release.Version} failed verification: got {actual}.");
        }
        return downloaded;
    }

    public static void Swap(string currentExe, string downloadedExe)
    {
        string previous = currentExe + OLD_SUFFIX;
        FileOps.TryDeleteFile(previous);
        File.Move(currentExe, previous);
        try
        {
            File.Move(downloadedExe, currentExe);
        }
        catch
        {
            File.Move(previous, currentExe);
            throw;
        }
    }

    public static void CleanupPrevious(string currentExe)
    {
        FileOps.TryDeleteFile(currentExe + OLD_SUFFIX);
        FileOps.TryDeleteFile(currentExe + NEW_SUFFIX);
    }
}
