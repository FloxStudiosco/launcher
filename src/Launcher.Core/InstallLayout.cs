using System.IO;

namespace FloxStudios.Launcher.Core;

public sealed class InstallLayout
{
    public InstallLayout(string root)
    {
        Root = root;
    }

    public string Root { get; }
    public string GameDirectory => Path.Combine(Root, "game");
    public string StagingDirectory => Path.Combine(Root, "staging");
    public string StateFile => Path.Combine(Root, "installed.json");

    public string GamePath(string relativePath)
    {
        return FileOps.ToLocal(GameDirectory, relativePath);
    }
}
