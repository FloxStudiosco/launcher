using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FloxStudios.Launcher.Core;
using Xunit;

namespace FloxStudios.Launcher.Tests;

public sealed class SelfUpdaterTests : IDisposable
{
    private readonly TempDirectory _site = new TempDirectory();
    private readonly TempDirectory _app = new TempDirectory();

    private string CurrentExe => _app.Combine("FloxLauncher.exe");

    [Fact]
    public async Task DownloadAndSwap_ReplacesExeAndKeepsOld()
    {
        File.WriteAllText(CurrentExe, "v1");
        LauncherRelease release = PublishLauncher("v2", "1.0.1");

        string downloaded = await SelfUpdater.DownloadAsync(new DirectoryObjectSource(_site.Path), release, CurrentExe, CancellationToken.None);
        SelfUpdater.Swap(CurrentExe, downloaded);

        Assert.Equal("v2", File.ReadAllText(CurrentExe));
        Assert.Equal("v1", File.ReadAllText(CurrentExe + SelfUpdater.OLD_SUFFIX));
        SelfUpdater.CleanupPrevious(CurrentExe);
        Assert.False(File.Exists(CurrentExe + SelfUpdater.OLD_SUFFIX));
    }

    [Fact]
    public async Task Download_WithWrongHash_Throws()
    {
        File.WriteAllText(CurrentExe, "v1");
        LauncherRelease release = PublishLauncher("v2", "1.0.1");
        release.Sha256 = new string('0', 64);

        await Assert.ThrowsAsync<InvalidDataException>(
            () => SelfUpdater.DownloadAsync(new DirectoryObjectSource(_site.Path), release, CurrentExe, CancellationToken.None));

        Assert.False(File.Exists(CurrentExe + SelfUpdater.NEW_SUFFIX));
    }

    [Theory]
    [InlineData("1.0.0", "1.0.1", true)]
    [InlineData("1.0.1", "1.0.1", false)]
    [InlineData("1.0.2", "1.0.1", false)]
    public void IsUpdateAvailable_ComparesVersions(string current, string remote, bool expected)
    {
        var release = new LauncherRelease { Version = remote };

        Assert.Equal(expected, SelfUpdater.IsUpdateAvailable(current, release));
    }

    public void Dispose()
    {
        _site.Dispose();
        _app.Dispose();
    }

    private LauncherRelease PublishLauncher(string content, string version)
    {
        string url = "launcher/FloxLauncher-" + version + ".exe";
        _site.Write(url, content);
        return new LauncherRelease
        {
            Version = version,
            Url = url,
            Sha256 = FileHash.OfFile(_site.Combine(url)),
        };
    }
}
