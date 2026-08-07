using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using TrayWebApps.Models;
using TrayWebApps.Services;

namespace TrayWebApps;

public partial class WidgetWindow : Window
{
    private readonly AppSettings _settings;
    private readonly SettingsStore _settingsStore;

    public event EventHandler? OpenMenuRequested;

    public WidgetWindow(AppSettings settings, SettingsStore settingsStore)
    {
        InitializeComponent();
        _settings = settings;
        _settingsStore = settingsStore;

        Width = Math.Max(60, settings.WidgetWidth);
        Height = Math.Max(32, settings.WidgetHeight);
        WidgetLabel.Text = settings.WidgetText;

        if (!string.IsNullOrWhiteSpace(settings.WidgetImagePath) && File.Exists(settings.WidgetImagePath))
        {
            try
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.UriSource = new Uri(settings.WidgetImagePath, UriKind.Absolute);
                bitmap.EndInit();
                WidgetImage.Source = bitmap;
                WidgetImage.Visibility = Visibility.Visible;
            }
            catch { /* fall back to text-only widget */ }
        }

        var workArea = SystemParameters.WorkArea;
        Left = settings.WidgetX ?? workArea.Right - Width - 40;
        Top = settings.WidgetY ?? workArea.Top + 80;

        LocationChanged += WidgetWindow_LocationChanged;
    }

    private void Widget_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_settings.WidgetLocked)
        {
            OpenMenuRequested?.Invoke(this, EventArgs.Empty);
        }
        else if (e.LeftButton == MouseButtonState.Pressed)
        {
            DragMove();
        }
    }

    private void WidgetWindow_LocationChanged(object? sender, EventArgs e)
    {
        if (_settings.WidgetLocked) return;
        _settings.WidgetX = Left;
        _settings.WidgetY = Top;
        _settingsStore.Save(_settings);
    }
}
