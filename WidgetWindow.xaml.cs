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

        var hasText = !string.IsNullOrWhiteSpace(settings.WidgetText);
        WidgetLabel.Text = settings.WidgetText;
        WidgetLabel.Visibility = hasText ? Visibility.Visible : Visibility.Collapsed;

        var hasImage = false;
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
                hasImage = true;
            }
            catch { /* fall back to text-only widget */ }
        }

        if (hasImage)
        {
            ImageRow.Height = new GridLength(1, GridUnitType.Star);
            TextRow.Height = hasText ? GridLength.Auto : new GridLength(0);
            WidgetLabel.FontSize = 10;
            WidgetLabel.Margin = new Thickness(0, 2, 0, 0);
        }
        else
        {
            ImageRow.Height = new GridLength(0);
            TextRow.Height = new GridLength(1, GridUnitType.Star);
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
