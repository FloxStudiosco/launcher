using System;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace FloxStudios.Launcher.App;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public event Action? PlayRequested;
    public event Action? SecondaryRequested;
    public event Action? OpenFolderRequested;
    public event Action? VerifyRequested;

    public void ShowActions(bool canPlay, string? secondaryLabel, bool secondaryEnabled)
    {
        PlayButton.IsEnabled = canPlay;
        SecondaryButton.Visibility = secondaryLabel == null ? Visibility.Collapsed : Visibility.Visible;
        SecondaryButton.IsEnabled = secondaryEnabled;
        SecondaryButton.Tag = 0.0;
        SecondaryIcon.Visibility = Visibility.Visible;
        SecondaryLabel.Text = secondaryLabel ?? "";
    }

    public void ShowProgress(double fraction, string label)
    {
        SecondaryButton.Visibility = Visibility.Visible;
        SecondaryButton.IsEnabled = true;
        SecondaryButton.Tag = Math.Max(0.0, Math.Min(1.0, fraction));
        SecondaryIcon.Visibility = Visibility.Collapsed;
        SecondaryLabel.Text = label;
    }

    public void ShowStatus(string text)
    {
        StatusText.Text = text.ToUpperInvariant();
    }

    public void ShowVersion(string text)
    {
        VersionText.Text = text;
    }

    public void ShowLauncherVersion(string text)
    {
        LauncherVersionText.Text = "ЛАУНЧЕР " + text;
    }

    public void ShowInstallPath(string path)
    {
        InstallPathText.Text = path;
    }

    public void ShowChangelog(string text)
    {
        ChangelogText.Text = text;
    }

    public void ShowCover(string path)
    {
        var image = new BitmapImage();
        image.BeginInit();
        image.CacheOption = BitmapCacheOption.OnLoad;
        image.UriSource = new Uri(path, UriKind.Absolute);
        image.EndInit();
        image.Freeze();
        CoverBrush.ImageSource = image;
    }

    public void HideSheets()
    {
        SettingsSheet.Visibility = Visibility.Collapsed;
        AboutSheet.Visibility = Visibility.Collapsed;
    }

    private void ShowSheet(UIElement sheet)
    {
        HideSheets();
        sheet.Visibility = Visibility.Visible;
    }

    private void OnDragArea(object sender, MouseButtonEventArgs e)
    {
        if (e.ButtonState == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState.Minimized;
    }

    private void OnCloseClick(object sender, RoutedEventArgs e)
    {
        Close();
    }

    private void OnPlayClick(object sender, RoutedEventArgs e)
    {
        PlayRequested?.Invoke();
    }

    private void OnSecondaryClick(object sender, RoutedEventArgs e)
    {
        SecondaryRequested?.Invoke();
    }

    private void OnSettingsClick(object sender, RoutedEventArgs e)
    {
        ShowSheet(SettingsSheet);
    }

    private void OnAboutClick(object sender, RoutedEventArgs e)
    {
        ShowSheet(AboutSheet);
    }

    private void OnSheetCloseClick(object sender, RoutedEventArgs e)
    {
        HideSheets();
    }

    private void OnOpenFolderClick(object sender, RoutedEventArgs e)
    {
        OpenFolderRequested?.Invoke();
    }

    private void OnVerifyClick(object sender, RoutedEventArgs e)
    {
        VerifyRequested?.Invoke();
    }
}
