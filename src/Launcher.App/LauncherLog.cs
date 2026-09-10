using System;
using System.Globalization;
using System.IO;

namespace FloxStudios.Launcher.App;

public sealed class LauncherLog
{
    private const long MAX_BYTES = 1024 * 1024;

    private readonly string _path;

    public LauncherLog(string path)
    {
        _path = path;
    }

    public void Write(string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
            if (File.Exists(_path) && new FileInfo(_path).Length > MAX_BYTES)
            {
                File.Delete(_path);
            }
            string stamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
            File.AppendAllText(_path, stamp + " " + message + Environment.NewLine);
        }
        catch (IOException)
        {
        }
        catch (UnauthorizedAccessException)
        {
        }
    }
}
