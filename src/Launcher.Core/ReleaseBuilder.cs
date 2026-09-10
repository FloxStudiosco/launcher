using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace FloxStudios.Launcher.Core;

public static class ReleaseBuilder
{
    private static readonly string[] NOT_SHIPPED_MARKERS = { "DoNotShip", "DontShip" };

    public static Manifest Build(ReleaseRequest request)
    {
        if (!Directory.Exists(request.BuildDirectory))
        {
            throw new DirectoryNotFoundException($"Build directory not found: '{request.BuildDirectory}'.");
        }
        Directory.CreateDirectory(request.SiteDirectory);
        var manifest = new Manifest
        {
            Game = new GameRelease
            {
                Version = request.Version,
                MinVersion = IsBlank(request.MinVersion) ? request.Version : request.MinVersion!,
                Exe = request.Exe,
                Files = PackBuild(request.BuildDirectory, request.SiteDirectory),
            },
            Screenshot = CopyMedia(request.Screenshot, request.SiteDirectory, request.Version),
            Changelog = CopyMedia(request.Changelog, request.SiteDirectory, request.Version),
            Launcher = PackLauncher(request),
        };
        ManifestValidator.Validate(manifest);
        File.WriteAllText(Path.Combine(request.SiteDirectory, ManifestClient.FILE_NAME), LauncherJson.Serialize(manifest));
        return manifest;
    }

    private static List<ManifestFile> PackBuild(string buildDirectory, string siteDirectory)
    {
        var files = new List<ManifestFile>();
        IEnumerable<string> paths = Directory
            .EnumerateFiles(buildDirectory, "*", SearchOption.AllDirectories)
            .OrderBy(path => path, StringComparer.Ordinal);
        foreach (string fullPath in paths)
        {
            string relative = FileOps.RelativePath(buildDirectory, fullPath);
            if (!IsShipped(relative))
                continue;
            files.Add(PackFile(fullPath, relative, siteDirectory));
        }
        return files;
    }

    private static bool IsShipped(string relativePath)
    {
        foreach (string segment in relativePath.Split('/'))
        {
            foreach (string marker in NOT_SHIPPED_MARKERS)
            {
                if (segment.IndexOf(marker, StringComparison.OrdinalIgnoreCase) >= 0)
                    return false;
            }
        }
        return true;
    }

    private static ManifestFile PackFile(string fullPath, string relative, string siteDirectory)
    {
        string sha = FileHash.OfFile(fullPath);
        string objectPath = FileOps.ToLocal(siteDirectory, ObjectPaths.For(sha));
        if (!File.Exists(objectPath))
        {
            WriteCompressed(fullPath, objectPath);
        }
        return new ManifestFile
        {
            Path = relative,
            Size = new FileInfo(fullPath).Length,
            PackedSize = new FileInfo(objectPath).Length,
            Sha256 = sha,
        };
    }

    private static void WriteCompressed(string source, string destination)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        string partial = destination + ".part";
        using (FileStream input = File.OpenRead(source))
        using (FileStream output = File.Create(partial))
        using (var packed = new GZipStream(output, CompressionLevel.Optimal))
        {
            input.CopyTo(packed);
        }
        FileOps.Replace(partial, destination);
    }

    private static string? CopyMedia(string? source, string siteDirectory, string version)
    {
        if (IsBlank(source))
            return null;
        string name = version + "-" + Path.GetFileName(source!).Replace(' ', '-');
        string destination = Path.Combine(siteDirectory, "media", name);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(source!, destination, true);
        return "media/" + name;
    }

    private static LauncherRelease? PackLauncher(ReleaseRequest request)
    {
        if (IsBlank(request.LauncherExe))
            return null;
        if (IsBlank(request.LauncherVersion))
        {
            throw new ArgumentException("Launcher version is required when a launcher exe is published.");
        }
        string exe = request.LauncherExe!;
        string name = Path.GetFileNameWithoutExtension(exe) + "-" + request.LauncherVersion + ".exe";
        string destination = Path.Combine(request.SiteDirectory, "launcher", name);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        File.Copy(exe, destination, true);
        return new LauncherRelease
        {
            Version = request.LauncherVersion!,
            Url = "launcher/" + name,
            Size = new FileInfo(destination).Length,
            Sha256 = FileHash.OfFile(destination),
        };
    }

    private static bool IsBlank(string? value)
    {
        return value == null || value.Trim().Length == 0;
    }
}
