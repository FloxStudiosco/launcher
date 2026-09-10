namespace FloxStudios.Launcher.Core;

public static class LaunchPolicy
{
    public static LaunchState Decide(string? installedVersion, Manifest? remote)
    {
        bool installed = installedVersion != null && installedVersion.Length > 0;
        if (remote == null)
            return installed ? LaunchState.OfflineInstalled : LaunchState.OfflineNotInstalled;
        if (!installed)
            return LaunchState.NotInstalled;
        if (VersionOrder.IsOlder(installedVersion, remote.Game.MinVersion))
            return LaunchState.UpdateRequired;
        if (VersionOrder.IsOlder(installedVersion, remote.Game.Version))
            return LaunchState.UpdateAvailable;
        return LaunchState.UpToDate;
    }

    public static bool CanPlay(LaunchState state)
    {
        return state == LaunchState.UpToDate
            || state == LaunchState.UpdateAvailable
            || state == LaunchState.OfflineInstalled;
    }

    public static bool CanUpdate(LaunchState state)
    {
        return state == LaunchState.NotInstalled
            || state == LaunchState.UpdateAvailable
            || state == LaunchState.UpdateRequired;
    }
}
