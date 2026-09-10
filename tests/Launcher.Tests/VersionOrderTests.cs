using FloxStudios.Launcher.Core;
using Xunit;

namespace FloxStudios.Launcher.Tests;

public sealed class VersionOrderTests
{
    [Theory]
    [InlineData("0.0.9", "0.0.10")]
    [InlineData("0.9.0", "1.0.0")]
    [InlineData("1", "1.0.1")]
    [InlineData(null, "0.0.1")]
    [InlineData("", "0.0.1")]
    [InlineData("0.1.0-beta", "0.1.1")]
    public void LeftIsOlder(string? left, string right)
    {
        Assert.True(VersionOrder.IsOlder(left, right));
        Assert.False(VersionOrder.IsOlder(right, left));
    }

    [Theory]
    [InlineData("1", "1.0")]
    [InlineData("0.0.5", "0.0.5")]
    [InlineData("0.0.5+abc", "0.0.5")]
    public void EqualVersions(string left, string right)
    {
        Assert.Equal(0, VersionOrder.Compare(left, right));
    }
}
