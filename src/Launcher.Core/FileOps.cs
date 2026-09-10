using System;
using System.IO;
using System.Linq;

namespace FloxStudios.Launcher.Core;

internal static class FileOps
{
    public static void Replace(string source, string destination)
    {
        if (File.Exists(destination))
        {
            File.Delete(destination);
        }
        File.Move(source, destination);
    }

    public static string RelativePath(string root, string fullPath)
    {
        char separator = Path.DirectorySeparatorChar;
        string prefix = Path.GetFullPath(root).TrimEnd(separator, Path.AltDirectorySeparatorChar) + separator;
        return Path.GetFullPath(fullPath).Substring(prefix.Length).Replace(separator, '/');
    }

    public static string ToLocal(string root, string relativePath)
    {
        return Path.Combine(root, relativePath.Replace('/', Path.DirectorySeparatorChar));
    }

    public static void RemoveEmptyDirectories(string root)
    {
        if (!Directory.Exists(root))
            return;
        foreach (string directory in Directory.GetDirectories(root))
        {
            RemoveEmptyDirectories(directory);
            if (!Directory.EnumerateFileSystemEntries(directory).Any())
            {
                Directory.Delete(directory);
            }
        }
    }

    public static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, true);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }

    public static void TryDeleteFile(string path)
    {
        try
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
