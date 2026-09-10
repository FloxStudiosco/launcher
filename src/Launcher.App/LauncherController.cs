using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using FloxStudios.Launcher.Core;

namespace FloxStudios.Launcher.App;

public sealed class LauncherController : IDisposable
{
    private const double BYTES_PER_MEGABYTE = 1024 * 1024;

    private static readonly TimeSpan HTTP_TIMEOUT = TimeSpan.FromSeconds(30);
    private static readonly string[] IMAGE_EXTENSIONS = { ".png", ".jpg", ".jpeg" };
    private static readonly string[] TEXT_EXTENSIONS = { ".md", ".txt" };

    private readonly MainWindow _view;
    private readonly LauncherSettings _settings;
    private readonly LauncherLog _log;
    private readonly InstallLayout _layout;
    private readonly HttpClient _http;
    private readonly IObjectSource _source;
    private readonly GameUpdater _updater;

    private Manifest? _manifest;
    private CancellationTokenSource? _updateCancellation;

    public LauncherController(MainWindow view, LauncherSettings settings, LauncherLog log)
    {
        _view = view;
        _settings = settings;
        _log = log;
        _layout = new InstallLayout(settings.InstallRoot);
        _http = new HttpClient { Timeout = HTTP_TIMEOUT };
        _source = settings.CreateSource(_http);
        _updater = new GameUpdater(_source, _layout);
        _view.PlayRequested += OnPlayRequested;
        _view.SecondaryRequested += OnSecondaryRequested;
        _view.OpenFolderRequested += OnOpenFolderRequested;
        _view.VerifyRequested += OnVerifyRequested;
    }

    private string MediaCache => Path.Combine(_layout.Root, "cache");

    public void Start()
    {
        _ = StartAsync();
    }

    public void Dispose()
    {
        _view.PlayRequested -= OnPlayRequested;
        _view.SecondaryRequested -= OnSecondaryRequested;
        _view.OpenFolderRequested -= OnOpenFolderRequested;
        _view.VerifyRequested -= OnVerifyRequested;
        _updateCancellation?.Cancel();
        _http.Dispose();
    }

    private async Task StartAsync()
    {
        try
        {
            SelfUpdater.CleanupPrevious(_settings.LauncherExe);
            _view.ShowInstallPath(_layout.GameDirectory);
            _view.ShowLauncherVersion(_settings.LauncherVersion);
            ShowCachedMedia();
            await CheckAsync();
        }
        catch (Exception exception)
        {
            _log.Write("Startup failed: " + exception);
            Render("Ошибка запуска — подробности в журнале");
        }
    }

    private async Task CheckAsync()
    {
        _view.ShowActions(false, null, false);
        _view.ShowStatus("Проверяю обновления…");
        _manifest = await TryFetchManifestAsync();
        if (_manifest != null && await TrySelfUpdateAsync(_manifest))
            return;
        Render(null);
        if (_manifest != null)
        {
            await LoadMediaAsync(_manifest);
        }
    }

    private async Task<Manifest?> TryFetchManifestAsync()
    {
        try
        {
            return await ManifestClient.FetchAsync(_source, CancellationToken.None);
        }
        catch (Exception exception) when (ExpectedFailures.Is(exception))
        {
            _log.Write("Manifest unavailable: " + exception.Message);
            return null;
        }
    }

    private async Task<bool> TrySelfUpdateAsync(Manifest manifest)
    {
        LauncherRelease? release = manifest.Launcher;
        if (release == null || !SelfUpdater.IsUpdateAvailable(_settings.LauncherVersion, release))
            return false;
        try
        {
            _view.ShowStatus("Обновляю лаунчер…");
            string downloaded = await SelfUpdater.DownloadAsync(_source, release, _settings.LauncherExe, CancellationToken.None);
            SelfUpdater.Swap(_settings.LauncherExe, downloaded);
            Process.Start(_settings.LauncherExe, App.AFTER_UPDATE_ARGUMENT)?.Dispose();
            Application.Current.Shutdown();
            return true;
        }
        catch (Exception exception) when (ExpectedFailures.Is(exception))
        {
            _log.Write("Launcher self-update failed: " + exception);
            return false;
        }
    }

    private void Render(string? statusOverride)
    {
        InstalledState? installed = InstalledStateStore.Load(_layout);
        LaunchState state = LaunchPolicy.Decide(installed?.Version, _manifest);
        bool running = installed != null && IsRunning(installed.Exe);
        _view.ShowVersion(installed == null ? "" : "v" + installed.Version);
        _view.ShowActions(LaunchPolicy.CanPlay(state) && !running, SecondaryLabel(state), !running);
        _view.ShowStatus(statusOverride ?? (running ? "Игра запущена" : StatusFor(state)));
    }

    private static string? SecondaryLabel(LaunchState state)
    {
        switch (state)
        {
            case LaunchState.NotInstalled:
                return "Скачать";
            case LaunchState.UpdateAvailable:
            case LaunchState.UpdateRequired:
                return "Обновить";
            case LaunchState.OfflineInstalled:
            case LaunchState.OfflineNotInstalled:
                return "Повторить";
            default:
                return null;
        }
    }

    private string StatusFor(LaunchState state)
    {
        string remote = _manifest?.Game.Version ?? "";
        switch (state)
        {
            case LaunchState.NotInstalled:
                return "Доступна версия " + remote;
            case LaunchState.UpdateAvailable:
                return "Доступна версия " + remote;
            case LaunchState.UpdateRequired:
                return "Нужно обновиться до версии " + remote;
            case LaunchState.OfflineInstalled:
                return "Нет связи с сервером";
            case LaunchState.OfflineNotInstalled:
                return "Нет связи с сервером — попробуйте позже";
            default:
                return "Установлена последняя версия";
        }
    }

    private void OnSecondaryRequested()
    {
        if (_updateCancellation != null)
        {
            _updateCancellation.Cancel();
            return;
        }
        if (_manifest == null)
        {
            _ = CheckAsync();
            return;
        }
        _ = UpdateAsync(false);
    }

    private void OnVerifyRequested()
    {
        _view.HideSheets();
        if (_updateCancellation != null)
            return;
        if (_manifest == null)
        {
            Render("Проверка файлов недоступна без связи с сервером");
            return;
        }
        _ = UpdateAsync(true);
    }

    private async Task UpdateAsync(bool verifyAll)
    {
        InstalledState? installed = InstalledStateStore.Load(_layout);
        if (installed != null && IsRunning(installed.Exe))
        {
            Render("Закройте игру, чтобы обновить её");
            return;
        }
        Manifest manifest = _manifest!;
        _updateCancellation = new CancellationTokenSource();
        CancellationToken token = _updateCancellation.Token;
        _view.ShowActions(false, null, false);
        _view.ShowProgress(0, "Проверяю файлы…");
        _view.ShowStatus("Нажмите ещё раз, чтобы остановить");
        string outcome = await RunUpdateAsync(manifest, verifyAll, token);
        _updateCancellation.Dispose();
        _updateCancellation = null;
        Render(outcome);
    }

    private async Task<string> RunUpdateAsync(Manifest manifest, bool verifyAll, CancellationToken token)
    {
        try
        {
            UpdatePlan plan = await Task.Run(() => _updater.Plan(manifest, verifyAll), token);
            var progress = new Progress<UpdateProgress>(ReportProgress);
            await Task.Run(() => _updater.ApplyAsync(manifest, plan, progress, token), token);
            return "Готово — версия " + manifest.Game.Version;
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
            return "Загрузка остановлена";
        }
        catch (Exception exception) when (ExpectedFailures.Is(exception))
        {
            _log.Write("Update failed: " + exception);
            return "Не удалось обновить — подробности в журнале";
        }
    }

    private void ReportProgress(UpdateProgress progress)
    {
        if (_updateCancellation == null)
            return;
        if (progress.Phase != UpdatePhase.Downloading)
        {
            _view.ShowProgress(1, "Устанавливаю…");
            return;
        }
        double fraction = progress.TotalBytes > 0 ? (double)progress.DoneBytes / progress.TotalBytes : 1;
        _view.ShowProgress(fraction, Megabytes(progress.DoneBytes) + " / " + Megabytes(progress.TotalBytes) + " МБ");
    }

    private void OnPlayRequested()
    {
        InstalledState? installed = InstalledStateStore.Load(_layout);
        if (installed == null)
        {
            Render(null);
            return;
        }
        string exe = _layout.GamePath(installed.Exe);
        try
        {
            var start = new ProcessStartInfo(exe)
            {
                WorkingDirectory = Path.GetDirectoryName(exe),
                UseShellExecute = false,
            };
            Process.Start(start)?.Dispose();
            Application.Current.Shutdown();
        }
        catch (Exception exception) when (ExpectedFailures.Is(exception))
        {
            _log.Write("Launch failed: " + exception);
            Render("Не удалось запустить игру — попробуйте «Проверить файлы»");
        }
    }

    private void OnOpenFolderRequested()
    {
        Directory.CreateDirectory(_layout.GameDirectory);
        Process.Start("explorer.exe", "\"" + _layout.GameDirectory + "\"")?.Dispose();
    }

    private void ShowCachedMedia()
    {
        string? cover = NewestFile(MediaCache, IMAGE_EXTENSIONS);
        if (cover != null)
        {
            TryShowCover(cover);
        }
        string? changelog = NewestFile(MediaCache, TEXT_EXTENSIONS);
        if (changelog != null)
        {
            _view.ShowChangelog(File.ReadAllText(changelog));
        }
    }

    private async Task LoadMediaAsync(Manifest manifest)
    {
        string? cover = await TryCacheAsync(manifest.Screenshot);
        if (cover != null)
        {
            TryShowCover(cover);
        }
        string? changelog = await TryCacheAsync(manifest.Changelog);
        if (changelog != null)
        {
            _view.ShowChangelog(File.ReadAllText(changelog));
        }
    }

    private async Task<string?> TryCacheAsync(string? relativePath)
    {
        if (relativePath == null)
            return null;
        string target = Path.Combine(MediaCache, Path.GetFileName(relativePath));
        if (File.Exists(target))
            return target;
        try
        {
            Directory.CreateDirectory(MediaCache);
            string partial = target + ".part";
            using (Stream remote = await _source.OpenAsync(relativePath, CancellationToken.None))
            using (FileStream output = File.Create(partial))
            {
                await remote.CopyToAsync(output);
            }
            File.Move(partial, target);
            return target;
        }
        catch (Exception exception) when (ExpectedFailures.Is(exception))
        {
            _log.Write("Media unavailable: " + relativePath + ": " + exception.Message);
            return null;
        }
    }

    private void TryShowCover(string path)
    {
        try
        {
            _view.ShowCover(path);
        }
        catch (Exception exception) when (ExpectedFailures.Is(exception) || exception is FileFormatException)
        {
            _log.Write("Cover unreadable: " + path + ": " + exception.Message);
        }
    }

    private static string? NewestFile(string directory, string[] extensions)
    {
        if (!Directory.Exists(directory))
            return null;
        return new DirectoryInfo(directory)
            .EnumerateFiles()
            .Where(file => extensions.Contains(file.Extension.ToLowerInvariant()))
            .OrderByDescending(file => file.LastWriteTimeUtc)
            .Select(file => file.FullName)
            .FirstOrDefault();
    }

    private static bool IsRunning(string exeRelativePath)
    {
        if (exeRelativePath.Length == 0)
            return false;
        Process[] processes = Process.GetProcessesByName(Path.GetFileNameWithoutExtension(exeRelativePath));
        foreach (Process process in processes)
        {
            process.Dispose();
        }
        return processes.Length > 0;
    }

    private static string Megabytes(long bytes)
    {
        return (bytes / BYTES_PER_MEGABYTE).ToString("0", CultureInfo.CurrentCulture);
    }
}
