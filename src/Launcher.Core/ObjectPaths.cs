namespace FloxStudios.Launcher.Core;

public static class ObjectPaths
{
    public static string For(string sha256)
    {
        return "objects/" + sha256.Substring(0, 2) + "/" + sha256 + ".gz";
    }
}
