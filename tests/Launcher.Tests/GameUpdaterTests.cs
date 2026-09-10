using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using FloxStudios.Launcher.Core;
using Xunit;

namespace FloxStudios.Launcher.Tests;

public sealed class GameUpdaterTests : IDisposable
{
    private const string EXE = "Game.exe";

    private readonly TempDirectory _build = new TempDirectory();
    private readonly TempDirectory _site = new TempDirectory();
    private readonly TempDirectory _install = new TempDirectory();

    private InstallLayout Layout => new InstallLayout(_install.Path);

    [Fact]
    public async Task FreshInstall_MirrorsBuildAndRecordsVersion()
    {
        WriteBaseBuild();

        await InstallAsync(Release("0.0.1"));

        Assert.Equal("exe", File.ReadAllText(Layout.GamePath(EXE)));
        Assert.Equal("alpha", File.ReadAllText(Layout.GamePath("Game_Data/a.bin")));
        Assert.Equal("beta", File.ReadAllText(Layout.GamePath("Game_Data/Sub/b.bin")));
        InstalledState? state = InstalledStateStore.Load(Layout);
        Assert.NotNull(state);
        Assert.Equal("0.0.1", state!.Version);
        Assert.Equal(EXE, state.Exe);
        Assert.False(Directory.Exists(Layout.StagingDirectory));
    }

    [Fact]
    public async Task IdenticalFiles_AreFetchedOnce()
    {
        WriteBaseBuild();
        _build.Write("Game_Data/copy.bin", "alpha");

        RecordingSource source = await InstallAsync(Release("0.0.1"));

        Assert.Equal(3, source.Opened.Count);
        Assert.Equal("alpha", File.ReadAllText(Layout.GamePath("Game_Data/copy.bin")));
    }

    [Fact]
    public async Task PlanAfterInstall_IsEmpty()
    {
        WriteBaseBuild();
        Manifest manifest = Release("0.0.1");
        await InstallAsync(manifest);

        UpdatePlan plan = new GameUpdater(new DirectoryObjectSource(_site.Path), Layout).Plan(manifest);

        Assert.True(plan.IsEmpty);
        Assert.Empty(plan.ObjectsToFetch);
    }

    [Fact]
    public async Task SecondRelease_FetchesOnlyChangedObjects()
    {
        WriteBaseBuild();
        await InstallAsync(Release("0.0.1"));
        _build.Write("Game_Data/Sub/b.bin", "beta-2");

        RecordingSource source = await InstallAsync(Release("0.0.2"));

        string changed = ObjectPaths.For(Sha("beta-2"));
        Assert.Equal(new[] { changed }, source.Opened.ToArray());
        Assert.Equal("beta-2", File.ReadAllText(Layout.GamePath("Game_Data/Sub/b.bin")));
        Assert.Equal("0.0.2", InstalledStateStore.Load(Layout)!.Version);
    }

    [Fact]
    public async Task Update_RemovesDroppedFilesAndEmptyDirectories()
    {
        WriteBaseBuild();
        await InstallAsync(Release("0.0.1"));
        File.Delete(_build.Combine("Game_Data/Sub/b.bin"));
        File.WriteAllText(Layout.GamePath("stray.log"), "junk");

        await InstallAsync(Release("0.0.2"));

        Assert.False(File.Exists(Layout.GamePath("Game_Data/Sub/b.bin")));
        Assert.False(Directory.Exists(Layout.GamePath("Game_Data/Sub")));
        Assert.False(File.Exists(Layout.GamePath("stray.log")));
        Assert.True(File.Exists(Layout.GamePath("Game_Data/a.bin")));
    }

    [Fact]
    public async Task LocallyModifiedFile_IsRepaired()
    {
        WriteBaseBuild();
        Manifest manifest = Release("0.0.1");
        await InstallAsync(manifest);
        File.WriteAllText(Layout.GamePath("Game_Data/a.bin"), "tampered");

        await InstallAsync(manifest);

        Assert.Equal("alpha", File.ReadAllText(Layout.GamePath("Game_Data/a.bin")));
    }

    [Fact]
    public async Task CorruptObject_ThrowsAndKeepsPreviousInstall()
    {
        WriteBaseBuild();
        await InstallAsync(Release("0.0.1"));
        _build.Write("Game_Data/a.bin", "alpha-2");
        Manifest next = Release("0.0.2");
        WriteCompressedObject(Sha("alpha-2"), "evil");

        await Assert.ThrowsAsync<InvalidDataException>(() => InstallAsync(next));

        Assert.Equal("0.0.1", InstalledStateStore.Load(Layout)!.Version);
        Assert.Equal("alpha", File.ReadAllText(Layout.GamePath("Game_Data/a.bin")));
    }

    [Fact]
    public async Task ReportsDownloadProgressUpToTotal()
    {
        WriteBaseBuild();
        Manifest manifest = Release("0.0.1");
        var updater = new GameUpdater(new DirectoryObjectSource(_site.Path), Layout);
        UpdatePlan plan = updater.Plan(manifest);
        var recorder = new ProgressRecorder();

        await updater.ApplyAsync(manifest, plan, recorder, CancellationToken.None);

        UpdateProgress last = recorder.Last!;
        Assert.Equal(UpdatePhase.Done, last.Phase);
        Assert.Equal(plan.DownloadBytes, last.DoneBytes);
        Assert.True(plan.DownloadBytes > 0);
    }

    [Fact]
    public void ReleaseBuilder_SkipsDoNotShipFolders()
    {
        WriteBaseBuild();
        _build.Write("Game_BurstDebugInformation_DoNotShip/x.pdb", "debug");
        _build.Write("Game_BackUpThisFolder_ButDontShipItWithYourGame/y.cpp", "backup");

        Manifest manifest = Release("0.0.1");

        Assert.DoesNotContain(manifest.Game.Files, file => file.Path.Contains("Ship"));
        Assert.Equal(3, manifest.Game.Files.Count);
    }

    [Fact]
    public void ReleaseBuilder_DefaultsMinVersionToVersion()
    {
        WriteBaseBuild();

        Manifest manifest = Release("0.0.7");

        Assert.Equal("0.0.7", manifest.Game.MinVersion);
    }

    [Fact]
    public async Task ManifestClient_ReadsPublishedManifest()
    {
        WriteBaseBuild();
        Release("0.0.3");

        Manifest manifest = await ManifestClient.FetchAsync(new DirectoryObjectSource(_site.Path), CancellationToken.None);

        Assert.Equal("0.0.3", manifest.Game.Version);
        Assert.Equal(3, manifest.Game.Files.Count);
    }

    public void Dispose()
    {
        _build.Dispose();
        _site.Dispose();
        _install.Dispose();
    }

    private void WriteBaseBuild()
    {
        _build.Write(EXE, "exe");
        _build.Write("Game_Data/a.bin", "alpha");
        _build.Write("Game_Data/Sub/b.bin", "beta");
    }

    private Manifest Release(string version)
    {
        return ReleaseBuilder.Build(new ReleaseRequest
        {
            BuildDirectory = _build.Path,
            SiteDirectory = _site.Path,
            Version = version,
            Exe = EXE,
        });
    }

    private async Task<RecordingSource> InstallAsync(Manifest manifest)
    {
        var source = new RecordingSource(new DirectoryObjectSource(_site.Path));
        var updater = new GameUpdater(source, Layout);
        UpdatePlan plan = updater.Plan(manifest);
        await updater.ApplyAsync(manifest, plan, null, CancellationToken.None);
        return source;
    }

    private void WriteCompressedObject(string sha256, string content)
    {
        string path = _site.Combine(ObjectPaths.For(sha256));
        using FileStream output = File.Create(path);
        using var packed = new GZipStream(output, CompressionLevel.Optimal);
        byte[] bytes = Encoding.UTF8.GetBytes(content);
        packed.Write(bytes, 0, bytes.Length);
    }

    private static string Sha(string content)
    {
        return FileHash.OfBytes(Encoding.UTF8.GetBytes(content));
    }

    private sealed class ProgressRecorder : IProgress<UpdateProgress>
    {
        public UpdateProgress? Last { get; private set; }

        public void Report(UpdateProgress value)
        {
            Last = value;
        }
    }
}
