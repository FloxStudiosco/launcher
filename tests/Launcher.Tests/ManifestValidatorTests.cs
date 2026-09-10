using System.IO;
using FloxStudios.Launcher.Core;
using Xunit;

namespace FloxStudios.Launcher.Tests;

public sealed class ManifestValidatorTests
{
    private static readonly string SHA = new string('a', 64);

    [Theory]
    [InlineData("Game.exe")]
    [InlineData("Game_Data/Managed/Assembly.dll")]
    public void SafePaths_AreAccepted(string path)
    {
        Assert.True(ManifestValidator.IsSafeRelativePath(path));
    }

    [Theory]
    [InlineData("")]
    [InlineData("../evil.dll")]
    [InlineData("Game_Data/../../evil.dll")]
    [InlineData("./Game.exe")]
    [InlineData("/etc/passwd")]
    [InlineData("C:/Windows/evil.dll")]
    [InlineData("Game_Data\\evil.dll")]
    [InlineData("Game_Data//evil.dll")]
    public void UnsafePaths_AreRejected(string path)
    {
        Assert.False(ManifestValidator.IsSafeRelativePath(path));
    }

    [Fact]
    public void ValidManifest_Passes()
    {
        ManifestValidator.Validate(Manifest("Game.exe", "Game.exe"));
    }

    [Fact]
    public void TraversalPath_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ManifestValidator.Validate(Manifest("Game.exe", "Game.exe", "../x.dll")));
    }

    [Fact]
    public void DuplicatePath_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ManifestValidator.Validate(Manifest("Game.exe", "Game.exe", "game.exe")));
    }

    [Fact]
    public void MissingExe_Throws()
    {
        Assert.Throws<InvalidDataException>(() => ManifestValidator.Validate(Manifest("Other.exe", "Game.exe")));
    }

    [Fact]
    public void UppercaseSha_Throws()
    {
        Manifest manifest = Manifest("Game.exe", "Game.exe");
        manifest.Game.Files[0].Sha256 = SHA.ToUpperInvariant();

        Assert.Throws<InvalidDataException>(() => ManifestValidator.Validate(manifest));
    }

    private static Manifest Manifest(string exe, params string[] paths)
    {
        var manifest = new Manifest { Game = new GameRelease { Version = "0.0.1", Exe = exe } };
        foreach (string path in paths)
        {
            manifest.Game.Files.Add(new ManifestFile { Path = path, Size = 1, PackedSize = 1, Sha256 = SHA });
        }
        return manifest;
    }
}
