using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TrayWebApps.Models;
using TrayWebApps.Services;
using Brush = System.Windows.Media.Brush;
using Brushes = System.Windows.Media.Brushes;
using Button = System.Windows.Controls.Button;
using Color = System.Windows.Media.Color;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Image = System.Windows.Controls.Image;
using MessageBox = System.Windows.MessageBox;
using Orientation = System.Windows.Controls.Orientation;

namespace TrayWebApps;

public partial class MainWindow : Window
{
    // The composition control participates in WPF rendering, allowing drawers and
    // other app chrome to appear above the browser surface.
    private sealed record OpenTab(WebAppDefinition Definition, WebView2 Browser)
    {
        public BitmapImage? Preview { get; set; }
    }
    private readonly AppStore _store = new();
    private readonly SettingsStore _settingsStore = new();
    private List<WebAppDefinition> _apps;
    private AppSettings _settings;
    private readonly List<OpenTab> _tabs = [];
    private OpenTab? _activeTab;
    private bool _reallyClose;

    public event EventHandler? AppsChanged;
    public event EventHandler? MinimizeToWidgetRequested;
    public IReadOnlyList<WebAppDefinition> ConfiguredApps => _apps;
    public AppSettings Settings => _settings;
    public bool IsFullScreen => WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized && ResizeMode == ResizeMode.NoResize;

    public MainWindow()
    {
        InitializeComponent();
        _apps = _store.Load();
        _settings = _settingsStore.Load();
        RenderAppMenu();
        RefreshTitleStrip();
        ApplyWindowControlVisibility();
    }

    public async void OpenApp(Guid id)
    {
        var existing = _tabs.FirstOrDefault(t => t.Definition.Id == id);
        if (existing is not null) { ActivateTab(existing); return; }
        var definition = _apps.FirstOrDefault(a => a.Id == id);
        if (definition is null) return;
        if (!string.IsNullOrEmpty(definition.PinHash))
        {
            var pinPrompt = new PinPromptWindow(definition.PinHash, definition.Name) { Owner = this };
            if (pinPrompt.ShowDialog() != true) return;
        }

        WebView2 browser;
        try
        {
            browser = new WebView2
            {
                Visibility = Visibility.Collapsed,
                CreationProperties = new CoreWebView2CreationProperties()
            };
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not start the embedded browser.\n\n{ex.Message}\n\nInstall the Microsoft Edge WebView2 Runtime if it is not already installed.", "Orbit", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        var tab = new OpenTab(definition, browser);
        _tabs.Add(tab);
        BrowserHost.Children.Add(browser);
        browser.NavigationStarting += (_, _) => { if (_activeTab == tab) StatusText.Text = $"Loading {definition.Name}…"; };
        browser.NavigationCompleted += (_, _) => UpdateNavigation();
        ActivateTab(tab);
        StatusText.Text = $"Loading {definition.Name}…";
        try
        {
            await browser.EnsureCoreWebView2Async();
            browser.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            browser.CoreWebView2.Settings.IsStatusBarEnabled = false;
            browser.CoreWebView2.NewWindowRequested += (_, args) => { args.Handled = true; browser.CoreWebView2.Navigate(args.Uri); };
            browser.Source = new Uri(definition.Url);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Could not start the embedded browser.\n\n{ex.Message}\n\nInstall the Microsoft Edge WebView2 Runtime if it is not already installed.", "Orbit", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void ActivateTab(OpenTab tab)
    {
        var previous = _activeTab;
        _activeTab = tab;
        foreach (var item in _tabs) item.Browser.Visibility = item == tab ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Collapsed;
        RenderTabs();
        RenderAppMenu();
        UpdateNavigation();
        CloseDrawer();
        tab.Browser.Focus();
        if (previous is not null && previous != tab)
        {
            await CapturePreviewAsync(previous);
            RenderTabs();
        }
    }

    private static async Task CapturePreviewAsync(OpenTab tab)
    {
        if (tab.Browser.CoreWebView2 is not { } core) return;
        try
        {
            using var stream = new MemoryStream();
            await core.CapturePreviewAsync(CoreWebView2CapturePreviewImageFormat.Png, stream);
            stream.Position = 0;
            var image = new BitmapImage();
            image.BeginInit();
            image.CacheOption = BitmapCacheOption.OnLoad;
            image.StreamSource = stream;
            image.EndInit();
            image.Freeze();
            tab.Preview = image;
        }
        catch { /* preview capture is best-effort */ }
    }

    private void RenderTabs()
    {
        TabsPanel.Children.Clear();
        foreach (var tab in _tabs)
        {
            var content = new StackPanel { Orientation = Orientation.Horizontal };
            content.Children.Add(BuildTabThumbnail(tab));
            content.Children.Add(new TextBlock { Text = tab.Definition.Name, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) });

            var button = new Button
            {
                Content = content,
                Tag = tab,
                Padding = new Thickness(6, 5, 12, 5),
                Margin = new Thickness(0, 0, 6, 0),
                Background = tab == _activeTab ? new SolidColorBrush(Color.FromRgb(45, 68, 61)) : Brushes.Transparent,
                FontWeight = tab == _activeTab ? FontWeights.SemiBold : FontWeights.Normal
            };
            button.Click += (_, _) => ActivateTab((OpenTab)button.Tag);
            TabsPanel.Children.Add(button);
        }
    }

    private void UpdateNavigation()
    {
        var core = _activeTab?.Browser.CoreWebView2;
        BackButton.IsEnabled = core?.CanGoBack == true;
        ReloadButton.IsEnabled = core is not null;
        CloseTabButton.IsEnabled = _activeTab is not null;
        StatusText.Text = _activeTab?.Definition.Name ?? "No app open";
    }

    private void RenderAppMenu()
    {
        AppListPanel.Children.Clear();
        var availableApps = _apps.Where(app => _tabs.All(tab => tab.Definition.Id != app.Id)).ToList();
        foreach (var app in availableApps)
        {
            var row = new Grid { Margin = new Thickness(0, 0, 0, 8), Background = new SolidColorBrush(Color.FromRgb(30, 38, 51)) };
            row.ColumnDefinitions.Add(new ColumnDefinition());
            row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            var launch = new Button { Tag = app, HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(12), ToolTip = app.Url };

            var textStack = new StackPanel { Children = { new TextBlock { Text = app.Name, FontWeight = FontWeights.SemiBold }, new TextBlock { Text = ShortUrl(app.Url), Foreground = (Brush)FindResource("MutedInk"), FontSize = 11, Margin = new Thickness(0, 3, 0, 0) } } };
            var rowStack = new StackPanel { Orientation = Orientation.Horizontal };
            var icon = TryLoadIcon(app, 28);
            FrameworkElement iconElement = icon is not null ? icon : BuildFallbackIcon(app);
            rowStack.Children.Add(iconElement);
            rowStack.Children.Add(textStack);
            launch.Content = rowStack;

            launch.Click += (_, _) => OpenApp(((WebAppDefinition)launch.Tag).Id);
            var remove = new Button { Content = "×", Tag = app, ToolTip = "Remove from menu", Foreground = new SolidColorBrush(Color.FromRgb(255, 141, 152)) };
            remove.Click += RemoveApp_Click;
            Grid.SetColumn(remove, 1);
            row.Children.Add(launch); row.Children.Add(remove);
            AppListPanel.Children.Add(row);
        }

        if (availableApps.Count == 0)
        {
            AppListPanel.Children.Add(new TextBlock
            {
                Text = "All configured apps are already open.",
                Foreground = (Brush)FindResource("MutedInk"),
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(4, 10, 4, 18)
            });
        }
    }

    private static Border BuildTabThumbnail(OpenTab tab)
    {
        var border = new Border
        {
            Width = 44,
            Height = 28,
            CornerRadius = new CornerRadius(4),
            ClipToBounds = true,
            Background = new SolidColorBrush(Color.FromRgb(10, 14, 20))
        };
        if (tab.Preview is not null)
        {
            border.Child = new Image { Source = tab.Preview, Stretch = Stretch.UniformToFill };
        }
        else if (TryLoadIcon(tab.Definition, 16) is { } icon)
        {
            icon.Margin = new Thickness(0);
            border.Child = icon;
        }
        else
        {
            border.Child = new TextBlock { Text = "◎", Foreground = Brushes.Gray, FontSize = 11, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center };
        }
        return border;
    }

    private static Image? TryLoadIcon(WebAppDefinition app, double size)
    {
        if (string.IsNullOrWhiteSpace(app.IconPath) || !File.Exists(app.IconPath)) return null;
        try
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.UriSource = new Uri(app.IconPath, UriKind.Absolute);
            bitmap.EndInit();
            return new Image { Source = bitmap, Width = size, Height = size, Margin = new Thickness(0, 0, 8, 0) };
        }
        catch { return null; }
    }

    private Border BuildFallbackIcon(WebAppDefinition app)
    {
        return new Border
        {
            Width = 28,
            Height = 28,
            CornerRadius = new CornerRadius(6),
            Background = (Brush)FindResource("Accent"),
            Margin = new Thickness(0, 0, 8, 0),
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

    private static string ShortUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) return uri.Host.Replace("www.", "");
        return url;
    }

    private void RemoveApp_Click(object sender, RoutedEventArgs e)
    {
        var app = (WebAppDefinition)((Button)sender).Tag;
        if (MessageBox.Show(this, $"Remove {app.Name} from the launch menu?", "Orbit", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes) return;
        _apps.Remove(app); _store.Save(_apps); RenderAppMenu(); AppsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void Back_Click(object sender, RoutedEventArgs e) { if (_activeTab?.Browser.CoreWebView2?.CanGoBack == true) _activeTab.Browser.CoreWebView2.GoBack(); }
    private void Reload_Click(object sender, RoutedEventArgs e) => _activeTab?.Browser.CoreWebView2?.Reload();
    private void CloseTab_Click(object sender, RoutedEventArgs e)
    {
        if (_activeTab is null) return;
        var index = _tabs.IndexOf(_activeTab);
        BrowserHost.Children.Remove(_activeTab.Browser); _activeTab.Browser.Dispose(); _tabs.RemoveAt(index);
        RenderAppMenu();
        _activeTab = _tabs.Count == 0 ? null : _tabs[Math.Min(index, _tabs.Count - 1)];
        if (_activeTab is not null) ActivateTab(_activeTab);
        else { EmptyState.Visibility = Visibility.Visible; RenderTabs(); UpdateNavigation(); }
    }

    public void ToggleFullScreen()
    {
        if (IsFullScreen)
        {
            ResizeMode = ResizeMode.CanResizeWithGrip; WindowState = WindowState.Normal; WindowStyle = WindowStyle.None; FullScreenGlyph.Text = "□";
        }
        else
        {
            WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; WindowState = WindowState.Maximized; FullScreenGlyph.Text = "❐";
        }
        TitleStrip.Visibility = IsFullScreen ? Visibility.Collapsed : Visibility.Visible;
        AppsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void RefreshTitleStrip()
    {
        var info = Environment.MachineName;
        if (!string.IsNullOrWhiteSpace(_settings.AssetId)) info += $"   •   Asset: {_settings.AssetId}";
        SystemInfoText.Text = info;
        TitleStrip.Visibility = IsFullScreen ? Visibility.Collapsed : Visibility.Visible;
    }

    private void ApplyWindowControlVisibility()
    {
        var kiosk = _settings.HideWindowControls;
        var widgetMode = _settings.StartAsWidget && !kiosk;
        WindowControlsPanel.Visibility = !kiosk && !widgetMode ? Visibility.Visible : Visibility.Collapsed;
        WidgetControlsPanel.Visibility = widgetMode ? Visibility.Visible : Visibility.Collapsed;
        KioskCloseButton.Visibility = kiosk ? Visibility.Visible : Visibility.Collapsed;
        Topmost = _settings.AlwaysOnTop;
    }

    private void KioskClose_Click(object sender, RoutedEventArgs e) => (System.Windows.Application.Current as App)?.ExitApplication();
    private void MinimizeToWidget_Click(object sender, RoutedEventArgs e) => MinimizeToWidgetRequested?.Invoke(this, EventArgs.Empty);

    public void OpenDrawer()
    {
        if (_activeTab is not null) _activeTab.Browser.Visibility = Visibility.Collapsed;
        AppDrawer.Visibility = Visibility.Visible;
        Scrim.Visibility = Visibility.Visible;
    }

    public bool ConfirmClose()
    {
        if (string.IsNullOrEmpty(_settings.ClosePasswordHash)) return true;
        var prompt = new PasswordPromptWindow(_settings.ClosePasswordHash) { Owner = this };
        return prompt.ShowDialog() == true;
    }

    private void FullScreen_Click(object sender, RoutedEventArgs e) => ToggleFullScreen();
    private async void Minimize_Click(object sender, RoutedEventArgs e) { if (_activeTab is not null) await CapturePreviewAsync(_activeTab); WindowState = WindowState.Minimized; }
    private async void Hide_Click(object sender, RoutedEventArgs e) { if (_activeTab is not null) await CapturePreviewAsync(_activeTab); Hide(); }
    private void AppsButton_Click(object sender, RoutedEventArgs e) => OpenDrawer();
    private void CloseDrawer_Click(object sender, RoutedEventArgs e) => CloseDrawer();
    private void Scrim_Click(object sender, MouseButtonEventArgs e) => CloseDrawer();
    private void CloseDrawer()
    {
        AppDrawer.Visibility = Visibility.Collapsed;
        Scrim.Visibility = Visibility.Collapsed;
        if (_activeTab is not null) _activeTab.Browser.Visibility = Visibility.Visible;
    }
    private void TitleBar_Drag(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed && !IsFullScreen) DragMove(); }
    private async void Window_StateChanged(object? sender, EventArgs e) { if (WindowState == WindowState.Minimized) { if (_activeTab is not null) await CapturePreviewAsync(_activeTab); Hide(); } }
    private void Window_Closing(object? sender, CancelEventArgs e) { if (!_reallyClose) { e.Cancel = true; HideWithPreview(); } }
    private async void HideWithPreview() { if (_activeTab is not null) await CapturePreviewAsync(_activeTab); Hide(); }
    public void ReallyClose() { _reallyClose = true; Close(); }

    private void SettingsButton_Click(object sender, RoutedEventArgs e)
    {
        CloseDrawer();
        var settingsWindow = new SettingsWindow { Owner = this };
        settingsWindow.ShowDialog();
        _apps = _store.Load();
        _settings = _settingsStore.Load();
        App.ApplyAccentColor(_settings.AccentColor);
        RefreshTitleStrip();
        ApplyWindowControlVisibility();
        RenderAppMenu();
        RenderTabs();
        AppsChanged?.Invoke(this, EventArgs.Empty);
        if (_settings.StartAsWidget) MinimizeToWidgetRequested?.Invoke(this, EventArgs.Empty);
    }
}
