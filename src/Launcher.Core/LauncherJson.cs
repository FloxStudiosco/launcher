using System.IO;
using System.Text.Json;

namespace FloxStudios.Launcher.Core;

public static class LauncherJson
{
    private static readonly JsonSerializerOptions OPTIONS = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, OPTIONS);
    }

    public static T Deserialize<T>(string json) where T : class
    {
        T? value = JsonSerializer.Deserialize<T>(json, OPTIONS);
        if (value == null)
        {
            throw new InvalidDataException("JSON document is empty.");
        }
        return value;
    }
}
