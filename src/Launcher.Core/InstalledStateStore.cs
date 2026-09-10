using System.IO;
using System.Text.Json;

namespace FloxStudios.Launcher.Core;

public static class InstalledStateStore
{
    public static InstalledState? Load(InstallLayout layout)
    {
        if (!File.Exists(layout.StateFile))
            return null;
        try
        {
            return LauncherJson.Deserialize<InstalledState>(File.ReadAllText(layout.StateFile));
        }
        catch (JsonException)
        {
            return null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    public static void Save(InstallLayout layout, InstalledState state)
    {
        Directory.CreateDirectory(layout.Root);
        string temporary = layout.StateFile + ".tmp";
        File.WriteAllText(temporary, LauncherJson.Serialize(state));
        FileOps.Replace(temporary, layout.StateFile);
    }

    public static void Delete(InstallLayout layout)
    {
        if (File.Exists(layout.StateFile))
        {
            File.Delete(layout.StateFile);
        }
    }
}
