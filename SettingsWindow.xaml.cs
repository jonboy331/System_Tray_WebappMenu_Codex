using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TrayWebApps.Models;
using TrayWebApps.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using ColorConverter = System.Windows.Media.ColorConverter;
using Cursors = System.Windows.Input.Cursors;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;

namespace TrayWebApps;

public partial class SettingsWindow : Window
{
    private readonly AppStore _store = new();
    private readonly SettingsStore _settingsStore = new();
    private readonly List<WebAppDefinition> _apps;
    private readonly AppSettings _settings;

    private static readonly (string Name, string Hex)[] AccentPresets =
    [
        ("Mint", "#78E3C5"),
        ("Sky", "#6CA0FF"),
        ("Violet", "#B78CFF"),
        ("Amber", "#FFC65C"),
        ("Coral", "#FF6B6B"),
    ];

    public SettingsWindow()
    {
        InitializeComponent();
        _apps = _store.Load();
        _settings = _settingsStore.Load();
        AssetIdBox.Text = _settings.AssetId;
        AccentHexBox.Text = _settings.AccentColor;
        StartupCheckBox.IsChecked = StartupManager.IsEnabled();
        StartMaximisedCheckBox.IsChecked = _settings.StartMaximised;
        AlwaysOnTopCheckBox.IsChecked = _settings.AlwaysOnTop;
        HideControlsCheckBox.IsChecked = _settings.HideWindowControls;
        StartAsWidgetCheckBox.IsChecked = _settings.StartAsWidget;
        WidgetTextBox.Text = _settings.WidgetText;
        WidgetWidthBox.Text = _settings.WidgetWidth.ToString("0");
        WidgetHeightBox.Text = _settings.WidgetHeight.ToString("0");
        WidgetLockCheckBox.IsChecked = _settings.WidgetLocked;
        RefreshWidgetImagePreview();
        RefreshPasswordStatus();
        BuildAccentSwatches();
        RenderAppsList();
    }

    private void StartMaximisedCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        _settings.StartMaximised = StartMaximisedCheckBox.IsChecked == true;
        _settingsStore.Save(_settings);
    }

    private void HideControlsCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        _settings.HideWindowControls = HideControlsCheckBox.IsChecked == true;
        _settingsStore.Save(_settings);
    }

    private void AlwaysOnTopCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        _settings.AlwaysOnTop = AlwaysOnTopCheckBox.IsChecked == true;
        _settingsStore.Save(_settings);
    }

    private void StartAsWidgetCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        _settings.StartAsWidget = StartAsWidgetCheckBox.IsChecked == true;
        _settingsStore.Save(_settings);
    }

    private void WidgetTextBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _settings.WidgetText = string.IsNullOrWhiteSpace(WidgetTextBox.Text) ? "Apps" : WidgetTextBox.Text.Trim();
        _settingsStore.Save(_settings);
    }

    private void WidgetSizeBox_LostFocus(object sender, RoutedEventArgs e)
    {
        if (double.TryParse(WidgetWidthBox.Text, out var width) && width >= 60) _settings.WidgetWidth = width;
        else WidgetWidthBox.Text = _settings.WidgetWidth.ToString("0");

        if (double.TryParse(WidgetHeightBox.Text, out var height) && height >= 32) _settings.WidgetHeight = height;
        else WidgetHeightBox.Text = _settings.WidgetHeight.ToString("0");

        _settingsStore.Save(_settings);
    }

    private void WidgetLockCheckBox_Toggled(object sender, RoutedEventArgs e)
    {
        _settings.WidgetLocked = WidgetLockCheckBox.IsChecked == true;
        _settingsStore.Save(_settings);
    }

    private void RefreshWidgetImagePreview()
    {
        WidgetImagePreview.Source = null;
        if (string.IsNullOrWhiteSpace(_settings.WidgetImagePath) || !File.Exists(_settings.WidgetImagePath)) return;
        try
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(_settings.WidgetImagePath, UriKind.Absolute);
            bitmap.EndInit();
            WidgetImagePreview.Source = bitmap;
        }
        catch { /* leave preview empty */ }
    }

    private void ChooseWidgetImage_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Filter = "Images|*.png;*.ico;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) != true) return;
        _settings.WidgetImagePath = FaviconService.CopyWidgetImage(dialog.FileName);
        _settingsStore.Save(_settings);
        RefreshWidgetImagePreview();
    }

    private void ClearWidgetImage_Click(object sender, RoutedEventArgs e)
    {
        _settings.WidgetImagePath = null;
        _settingsStore.Save(_settings);
        RefreshWidgetImagePreview();
    }

    private void RefreshPasswordStatus()
    {
        PasswordStatusText.Text = string.IsNullOrEmpty(_settings.ClosePasswordHash)
            ? "Not set — anyone can exit from the tray menu."
            : "A password is set and required to exit Orbit.";
    }

    private void SetPassword_Click(object sender, RoutedEventArgs e)
    {
        var password = ClosePasswordBox.Password;
        _settings.ClosePasswordHash = string.IsNullOrEmpty(password) ? null : PasswordService.Hash(password);
        _settingsStore.Save(_settings);
        ClosePasswordBox.Clear();
        RefreshPasswordStatus();
    }

    private void BuildAccentSwatches()
    {
        SwatchPanel.Children.Clear();
        foreach (var (name, hex) in AccentPresets)
        {
            var swatch = new Border
            {
                Width = 32,
                Height = 32,
                CornerRadius = new CornerRadius(8),
                Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex)!),
                Margin = new Thickness(0, 0, 8, 0),
                Cursor = Cursors.Hand,
                ToolTip = name,
                BorderBrush = hex.Equals(_settings.AccentColor, StringComparison.OrdinalIgnoreCase) ? Brushes.White : Brushes.Transparent,
                BorderThickness = new Thickness(2)
            };
            swatch.MouseLeftButtonUp += (_, _) => ApplyAccent(hex);
            SwatchPanel.Children.Add(swatch);
        }
    }

    private void ApplyAccent(string hex)
    {
        _settings.AccentColor = hex;
        AccentHexBox.Text = hex;
        _settingsStore.Save(_settings);
        App.ApplyAccentColor(hex);
        BuildAccentSwatches();
    }

    private void ApplyHex_Click(object sender, RoutedEventArgs e)
    {
        var hex = AccentHexBox.Text.Trim();
        try
        {
            ColorConverter.ConvertFromString(hex);
        }
        catch
        {
            MessageBox.Show(this, "Enter a valid colour, e.g. #78E3C5.", "Orbit", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }
        ApplyAccent(hex);
    }

    private void AssetIdBox_LostFocus(object sender, RoutedEventArgs e)
    {
        _settings.AssetId = AssetIdBox.Text.Trim();
        _settingsStore.Save(_settings);
    }

    private void StartupCheckBox_Toggled(object sender, RoutedEventArgs e) => StartupManager.SetEnabled(StartupCheckBox.IsChecked == true);

    private void AddApp_Click(object sender, RoutedEventArgs e)
    {
        var name = NewName.Text.Trim();
        var url = NewUrl.Text.Trim();
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || (uri.Scheme != "https" && uri.Scheme != "http") || string.IsNullOrWhiteSpace(name))
        {
            FormError.Text = "Enter a name and a full http:// or https:// address.";
            FormError.Visibility = Visibility.Visible;
            return;
        }

        var app = new WebAppDefinition { Name = name, Url = uri.ToString() };
        _apps.Add(app);
        _store.Save(_apps);
        NewName.Clear(); NewUrl.Text = "https://"; FormError.Visibility = Visibility.Collapsed;
        RenderAppsList();

        _ = FetchIconAsync(app);
    }

    private async Task FetchIconAsync(WebAppDefinition app)
    {
        var iconPath = await FaviconService.FetchAsync(app.Url, app.Id);
        if (iconPath is null) return;
        app.IconPath = iconPath;
        _store.Save(_apps);
        Dispatcher.Invoke(RenderAppsList);
    }

    private void RenderAppsList()
    {
        AppsListPanel.Children.Clear();
        foreach (var app in _apps)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var loadedIcon = TryLoadIcon(app);
            FrameworkElement icon = loadedIcon is not null ? (FrameworkElement)loadedIcon : BuildFallbackIcon(app);
            Grid.SetColumn(icon, 0);

            var textStack = new StackPanel { Margin = new Thickness(10, 0, 10, 0), VerticalAlignment = VerticalAlignment.Center };
            textStack.Children.Add(new TextBlock { Text = app.Name, FontWeight = FontWeights.SemiBold, Foreground = (Brush)FindResource("Ink") });
            textStack.Children.Add(new TextBlock { Text = app.Url, FontSize = 11, Foreground = (Brush)FindResource("MutedInk"), TextTrimming = TextTrimming.CharacterEllipsis });
            Grid.SetColumn(textStack, 1);

            var changeIcon = new Button { Content = "Change icon", Tag = app, FontSize = 11 };
            changeIcon.Click += ChangeIcon_Click;
            Grid.SetColumn(changeIcon, 2);

            var setPin = new Button { Content = string.IsNullOrEmpty(app.PinHash) ? "Set PIN" : "PIN set ✓", Tag = app, FontSize = 11, Margin = new Thickness(6, 0, 0, 0) };
            setPin.Click += SetPin_Click;
            Grid.SetColumn(setPin, 3);

            row.Children.Add(icon); row.Children.Add(textStack); row.Children.Add(changeIcon); row.Children.Add(setPin);
            AppsListPanel.Children.Add(row);
        }

        if (_apps.Count == 0)
            AppsListPanel.Children.Add(new TextBlock { Text = "No web apps yet.", Foreground = (Brush)FindResource("MutedInk"), Margin = new Thickness(0, 4, 0, 4) });
    }

    private void SetPin_Click(object sender, RoutedEventArgs e)
    {
        var app = (WebAppDefinition)((Button)sender).Tag;
        var dialog = new SetPinWindow(app.Name) { Owner = this };
        if (dialog.ShowDialog() != true) return;
        app.PinHash = dialog.RemovePin ? null : PasswordService.Hash(dialog.EnteredPin);
        _store.Save(_apps);
        RenderAppsList();
    }

    private void ChangeIcon_Click(object sender, RoutedEventArgs e)
    {
        var app = (WebAppDefinition)((Button)sender).Tag;
        var dialog = new OpenFileDialog { Filter = "Images|*.png;*.ico;*.jpg;*.jpeg;*.bmp" };
        if (dialog.ShowDialog(this) != true) return;
        app.IconPath = FaviconService.CopyManualIcon(dialog.FileName, app.Id);
        _store.Save(_apps);
        RenderAppsList();
    }

    private static Image? TryLoadIcon(WebAppDefinition app)
    {
        if (string.IsNullOrWhiteSpace(app.IconPath) || !File.Exists(app.IconPath)) return null;
        try
        {
            var bitmap = new System.Windows.Media.Imaging.BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = System.Windows.Media.Imaging.BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(app.IconPath, UriKind.Absolute);
            bitmap.EndInit();
            return new Image { Source = bitmap, Width = 32, Height = 32 };
        }
        catch { return null; }
    }

    private Border BuildFallbackIcon(WebAppDefinition app)
    {
        return new Border
        {
            Width = 32,
            Height = 32,
            CornerRadius = new CornerRadius(6),
            Background = (Brush)FindResource("Accent"),
            Child = new TextBlock
            {
                Text = app.Name.Length > 0 ? app.Name[0].ToString().ToUpperInvariant() : "?",
                Foreground = Brushes.Black,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                FontWeight = FontWeights.Bold
            }
        };
    }

    private void TitleBar_Drag(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed) DragMove(); }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();
}
