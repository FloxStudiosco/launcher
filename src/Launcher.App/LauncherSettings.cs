using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using FloxStudios.Launcher.Core;

namespace FloxStudios.Launcher.App;

public sealed class LauncherSettings
{
    private const string GAME_ID = "KintsugiMaster";
    private const string RELEASE_URL_KEY = "LauncherReleaseUrl";
    private const string RELEASE_URL_OVERRIDE = "FLOX_LAUNCHER_URL";
    private const string INSTALL_ROOT_OVERRIDE = "FLOX_LAUNCHER_ROOT";

    private LauncherSettings(string releaseAddress, string installRoot, string launcherVersion, string launcherExe)
    {
        ReleaseAddress = releaseAddress;
        InstallRoot = installRoot;
        LauncherVersion = launcherVersion;
        LauncherExe = launcherExe;
    }

    public string GameId => GAME_ID;
    public string ReleaseAddress { get; }
    public string InstallRoot { get; }
    public string LauncherVersion { get; }
    public string LauncherExe { get; }

    public static LauncherSettings Load()
    {
        Assembly assembly = Assembly.GetExecutingAssembly();
        string address = Environment.GetEnvironmentVariable(RELEASE_URL_OVERRIDE) ?? Metadata(assembly, RELEASE_URL_KEY) ?? "";
        string localData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        string root = Environment.GetEnvironmentVariable(INSTALL_ROOT_OVERRIDE) ?? Path.Combine(localData, "FloxStudios", GAME_ID);
        string version = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "0.0.0";
        return new LauncherSettings(address, root, version.Split('+')[0], assembly.Location);
    }

    public IObjectSource CreateSource(HttpClient http)
    {
        bool isWeb = ReleaseAddress.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || ReleaseAddress.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        if (isWeb)
            return new HttpObjectSource(http, new Uri(ReleaseAddress));
        return new DirectoryObjectSource(ReleaseAddress);
    }

    private static string? Metadata(Assembly assembly, string key)
    {
        string? value = assembly
            .GetCustomAttributes<AssemblyMetadataAttribute>()
            .FirstOrDefault(attribute => attribute.Key == key)?
            .Value;
        return value == null || value.Length == 0 ? null : value;
    }
}
