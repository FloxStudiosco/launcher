using FloxStudios.Launcher.Core;
using Xunit;

namespace FloxStudios.Launcher.Tests;

public sealed class LaunchPolicyTests
{
    [Theory]
    [InlineData(null, "0.0.2", "0.0.2", LaunchState.NotInstalled)]
    [InlineData("0.0.2", "0.0.2", "0.0.2", LaunchState.UpToDate)]
    [InlineData("0.0.3", "0.0.2", "0.0.2", LaunchState.UpToDate)]
    [InlineData("0.0.1", "0.0.2", "0.0.1", LaunchState.UpdateAvailable)]
    [InlineData("0.0.1", "0.0.2", "0.0.2", LaunchState.UpdateRequired)]
    [InlineData("0.0.1", "0.0.3", "", LaunchState.UpdateAvailable)]
    public void Online(string? installed, string version, string minVersion, LaunchState expected)
    {
        var manifest = new Manifest { Game = new GameRelease { Version = version, MinVersion = minVersion } };

        Assert.Equal(expected, LaunchPolicy.Decide(installed, manifest));
    }

    [Fact]
    public void Offline_WithInstall_AllowsPlay()
    {
        LaunchState state = LaunchPolicy.Decide("0.0.1", null);

        Assert.Equal(LaunchState.OfflineInstalled, state);
        Assert.True(LaunchPolicy.CanPlay(state));
        Assert.False(LaunchPolicy.CanUpdate(state));
    }

    [Fact]
    public void Offline_WithoutInstall_AllowsNothing()
    {
        LaunchState state = LaunchPolicy.Decide(null, null);

        Assert.False(LaunchPolicy.CanPlay(state));
        Assert.False(LaunchPolicy.CanUpdate(state));
    }

    [Fact]
    public void UpdateRequired_BlocksPlay()
    {
        Assert.False(LaunchPolicy.CanPlay(LaunchState.UpdateRequired));
        Assert.True(LaunchPolicy.CanUpdate(LaunchState.UpdateRequired));
    }
}
