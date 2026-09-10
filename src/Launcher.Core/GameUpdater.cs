using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;

namespace FloxStudios.Launcher.Core;

public sealed class GameUpdater
{
    private const int PARALLEL_DOWNLOADS = 4;
    private const int BUFFER_SIZE = 81920;

    private readonly IObjectSource _source;
    private readonly InstallLayout _layout;

    public GameUpdater(IObjectSource source, InstallLayout layout)
    {
        _source = source;
        _layout = layout;
    }

    public UpdatePlan Plan(Manifest manifest, bool verifyAll = false)
    {
        InstalledState? state = verifyAll ? null : InstalledStateStore.Load(_layout);
        Dictionary<string, InstalledFile> local = LocalScanner.Scan(_layout, state);
        return UpdatePlanner.Plan(manifest, local);
    }

    public async Task ApplyAsync(Manifest manifest, UpdatePlan plan, IProgress<UpdateProgress>? progress, CancellationToken cancellation)
    {
        long total = plan.DownloadBytes;
        await FetchObjectsAsync(plan, progress, cancellation).ConfigureAwait(false);
        progress?.Report(new UpdateProgress(UpdatePhase.Installing, total, total));
        InstalledStateStore.Delete(_layout);
        List<InstalledFile> placed = PlaceFiles(plan.FilesToPlace);
        DeleteFiles(plan.FilesToDelete);
        SaveState(manifest.Game, plan.UnchangedFiles, placed);
        FileOps.TryDeleteDirectory(_layout.StagingDirectory);
        progress?.Report(new UpdateProgress(UpdatePhase.Done, total, total));
    }

    private async Task FetchObjectsAsync(UpdatePlan plan, IProgress<UpdateProgress>? progress, CancellationToken cancellation)
    {
        Directory.CreateDirectory(_layout.StagingDirectory);
        long total = plan.DownloadBytes;
        long done = 0;
        Action<long> advance = bytes =>
        {
            long now = Interlocked.Add(ref done, bytes);
            progress?.Report(new UpdateProgress(UpdatePhase.Downloading, now, total));
        };
        using var gate = new SemaphoreSlim(PARALLEL_DOWNLOADS);
        var downloads = new List<Task>();
        foreach (ManifestFile file in plan.ObjectsToFetch)
        {
            downloads.Add(FetchGatedAsync(file, gate, advance, cancellation));
        }
        await Task.WhenAll(downloads).ConfigureAwait(false);
    }

    private async Task FetchGatedAsync(ManifestFile file, SemaphoreSlim gate, Action<long> advance, CancellationToken cancellation)
    {
        await gate.WaitAsync(cancellation).ConfigureAwait(false);
        try
        {
            await FetchObjectAsync(file, advance, cancellation).ConfigureAwait(false);
        }
        finally
        {
            gate.Release();
        }
    }

    private async Task FetchObjectAsync(ManifestFile file, Action<long> advance, CancellationToken cancellation)
    {
        string staged = StagedPath(file.Sha256);
        if (File.Exists(staged) && FileHash.OfFile(staged) == file.Sha256)
        {
            advance(file.PackedSize);
            return;
        }
        string partial = staged + ".part";
        using (Stream remote = await _source.OpenAsync(ObjectPaths.For(file.Sha256), cancellation).ConfigureAwait(false))
        using (var counted = new CountingStream(remote, advance))
        using (var unpacked = new GZipStream(counted, CompressionMode.Decompress))
        using (FileStream output = File.Create(partial))
        {
            await unpacked.CopyToAsync(output, BUFFER_SIZE, cancellation).ConfigureAwait(false);
        }
        Verify(partial, file.Sha256);
        FileOps.Replace(partial, staged);
    }

    private static void Verify(string path, string expectedSha256)
    {
        string actual = FileHash.OfFile(path);
        if (actual == expectedSha256)
            return;
        File.Delete(path);
        throw new InvalidDataException($"Object {expectedSha256} failed verification: got {actual}.");
    }

    private List<InstalledFile> PlaceFiles(IReadOnlyList<ManifestFile> files)
    {
        var placed = new List<InstalledFile>();
        foreach (ManifestFile file in files)
        {
            string target = _layout.GamePath(file.Path);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            string incoming = target + ".incoming";
            File.Copy(StagedPath(file.Sha256), incoming, true);
            FileOps.Replace(incoming, target);
            var info = new FileInfo(target);
            placed.Add(new InstalledFile
            {
                Path = file.Path,
                Size = info.Length,
                LastWriteTicks = info.LastWriteTimeUtc.Ticks,
                Sha256 = file.Sha256,
            });
        }
        return placed;
    }

    private void DeleteFiles(IReadOnlyList<string> paths)
    {
        foreach (string path in paths)
        {
            string target = _layout.GamePath(path);
            if (File.Exists(target))
            {
                File.Delete(target);
            }
        }
        FileOps.RemoveEmptyDirectories(_layout.GameDirectory);
    }

    private void SaveState(GameRelease release, IReadOnlyList<InstalledFile> unchanged, List<InstalledFile> placed)
    {
        var files = new List<InstalledFile>(unchanged);
        files.AddRange(placed);
        var state = new InstalledState
        {
            Version = release.Version,
            Exe = release.Exe,
            Files = files,
        };
        InstalledStateStore.Save(_layout, state);
    }

    private string StagedPath(string sha256)
    {
        return Path.Combine(_layout.StagingDirectory, sha256);
    }
}
