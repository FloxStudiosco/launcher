using System;
using System.Collections.Generic;
using System.IO;

namespace FloxStudios.Launcher.Core;

public static class ManifestValidator
{
    private const int SHA256_HEX_LENGTH = 64;

    public static void Validate(Manifest manifest)
    {
        GameRelease game = manifest.Game;
        if (game.Version.Length == 0)
        {
            throw new InvalidDataException("Manifest has no game version.");
        }
        ValidateFiles(game);
        ValidateOptionalPath(manifest.Screenshot, "screenshot");
        ValidateOptionalPath(manifest.Changelog, "changelog");
        if (manifest.Launcher != null)
        {
            ValidateLauncher(manifest.Launcher);
        }
    }

    public static bool IsSafeRelativePath(string? path)
    {
        if (path == null || path.Length == 0)
            return false;
        if (path.IndexOf('\\') >= 0 || path.IndexOf(':') >= 0 || path[0] == '/')
            return false;
        foreach (string segment in path.Split('/'))
        {
            if (segment.Length == 0 || segment == "." || segment == "..")
                return false;
        }
        return true;
    }

    public static bool IsSha256(string? value)
    {
        if (value == null || value.Length != SHA256_HEX_LENGTH)
            return false;
        foreach (char symbol in value)
        {
            bool isHex = (symbol >= '0' && symbol <= '9') || (symbol >= 'a' && symbol <= 'f');
            if (!isHex)
                return false;
        }
        return true;
    }

    private static void ValidateFiles(GameRelease game)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ManifestFile file in game.Files)
        {
            if (!IsSafeRelativePath(file.Path))
            {
                throw new InvalidDataException($"Unsafe path in manifest: '{file.Path}'.");
            }
            if (!IsSha256(file.Sha256))
            {
                throw new InvalidDataException($"Bad SHA-256 for '{file.Path}'.");
            }
            if (!seen.Add(file.Path))
            {
                throw new InvalidDataException($"Duplicate path in manifest: '{file.Path}'.");
            }
        }
        if (!seen.Contains(game.Exe))
        {
            throw new InvalidDataException($"Game exe '{game.Exe}' is not among the manifest files.");
        }
    }

    private static void ValidateOptionalPath(string? path, string field)
    {
        if (path != null && !IsSafeRelativePath(path))
        {
            throw new InvalidDataException($"Unsafe {field} path in manifest: '{path}'.");
        }
    }

    private static void ValidateLauncher(LauncherRelease launcher)
    {
        if (launcher.Version.Length == 0)
        {
            throw new InvalidDataException("Launcher release has no version.");
        }
        if (!IsSafeRelativePath(launcher.Url))
        {
            throw new InvalidDataException($"Unsafe launcher url: '{launcher.Url}'.");
        }
        if (!IsSha256(launcher.Sha256))
        {
            throw new InvalidDataException("Bad SHA-256 for the launcher release.");
        }
    }
}
