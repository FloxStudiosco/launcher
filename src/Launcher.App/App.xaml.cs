using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace FloxStudios.Launcher.App;

public partial class App : Application
{
    public const string AFTER_UPDATE_ARGUMENT = "--after-update";

    private static readonly TimeSpan AFTER_UPDATE_WAIT = TimeSpan.FromSeconds(15);

    private Mutex? _instanceLock;
    private LauncherLog? _log;
    private LauncherController? _controller;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        LauncherSettings settings = LauncherSettings.Load();
        _log = new LauncherLog(Path.Combine(settings.InstallRoot, "launcher.log"));
        DispatcherUnhandledException += OnUnhandledException;
        if (!AcquireInstanceLock(settings, e.Args))
        {
            Shutdown();
            return;
        }
        var window = new MainWindow();
        MainWindow = window;
        _controller = new LauncherController(window, settings, _log);
        window.Show();
        _controller.Start();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _controller?.Dispose();
        if (_instanceLock != null)
        {
            _instanceLock.ReleaseMutex();
            _instanceLock.Dispose();
        }
        base.OnExit(e);
    }

    private bool AcquireInstanceLock(LauncherSettings settings, string[] args)
    {
        var instanceLock = new Mutex(false, "FloxLauncher-" + settings.GameId);
        TimeSpan wait = args.Contains(AFTER_UPDATE_ARGUMENT) ? AFTER_UPDATE_WAIT : TimeSpan.Zero;
        bool acquired;
        try
        {
            acquired = instanceLock.WaitOne(wait);
        }
        catch (AbandonedMutexException)
        {
            acquired = true;
        }
        if (acquired)
        {
            _instanceLock = instanceLock;
            return true;
        }
        instanceLock.Dispose();
        return false;
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        _log?.Write("Unhandled: " + e.Exception);
        MessageBox.Show("Что-то пошло не так. Подробности — в launcher.log.\n\n" + e.Exception.Message, "Kintsugi",
            MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
