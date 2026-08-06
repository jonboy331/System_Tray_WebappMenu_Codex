using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using System.ComponentModel;
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
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using MessageBox = System.Windows.MessageBox;

namespace TrayWebApps;

public partial class MainWindow : Window
{
    // The composition control participates in WPF rendering, allowing drawers and
    // other app chrome to appear above the browser surface.
    private sealed record OpenTab(WebAppDefinition Definition, WebView2CompositionControl Browser);
    private readonly AppStore _store = new();
    private readonly List<WebAppDefinition> _apps;
    private readonly List<OpenTab> _tabs = [];
    private OpenTab? _activeTab;
    private bool _reallyClose;

    public event EventHandler? AppsChanged;
    public IReadOnlyList<WebAppDefinition> ConfiguredApps => _apps;
    public bool IsFullScreen => WindowStyle == WindowStyle.None && WindowState == WindowState.Maximized && ResizeMode == ResizeMode.NoResize;

    public MainWindow()
    {
        InitializeComponent();
        _apps = _store.Load();
        RenderAppMenu();
    }

    public async void OpenApp(Guid id)
    {
        var existing = _tabs.FirstOrDefault(t => t.Definition.Id == id);
        if (existing is not null) { ActivateTab(existing); return; }
        var definition = _apps.FirstOrDefault(a => a.Id == id);
        if (definition is null) return;

        var browser = new WebView2CompositionControl
        {
            Visibility = Visibility.Collapsed,
            CreationProperties = new CoreWebView2CreationProperties()
        };
        var tab = new OpenTab(definition, browser);
        _tabs.Add(tab);
        BrowserHost.Children.Add(browser);
        browser.NavigationStarting += (_, _) => { if (_activeTab == tab) StatusText.Text = $"Loading {definition.Name}…"; };
        browser.NavigationCompleted += (_, _) => UpdateNavigation();
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
        ActivateTab(tab);
    }

    private void ActivateTab(OpenTab tab)
    {
        _activeTab = tab;
        foreach (var item in _tabs) item.Browser.Visibility = item == tab ? Visibility.Visible : Visibility.Collapsed;
        EmptyState.Visibility = Visibility.Collapsed;
        RenderTabs();
        RenderAppMenu();
        UpdateNavigation();
        CloseDrawer();
        tab.Browser.Focus();
    }

    private void RenderTabs()
    {
        TabsPanel.Children.Clear();
        foreach (var tab in _tabs)
        {
            var button = new Button
            {
                Content = tab.Definition.Name,
                Tag = tab,
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
            launch.Content = new StackPanel { Children = { new TextBlock { Text = app.Name, FontWeight = FontWeights.SemiBold }, new TextBlock { Text = ShortUrl(app.Url), Foreground = (Brush)FindResource("MutedInk"), FontSize = 11, Margin = new Thickness(0, 3, 0, 0) } } };
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

    private static string ShortUrl(string url)
    {
        if (Uri.TryCreate(url, UriKind.Absolute, out var uri)) return uri.Host.Replace("www.", "");
        return url;
    }

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
        _apps.Add(new WebAppDefinition { Name = name, Url = uri.ToString() });
        _store.Save(_apps); NewName.Clear(); NewUrl.Text = "https://"; FormError.Visibility = Visibility.Collapsed;
        RenderAppMenu(); AppsChanged?.Invoke(this, EventArgs.Empty);
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
        AppsChanged?.Invoke(this, EventArgs.Empty);
    }

    private void FullScreen_Click(object sender, RoutedEventArgs e) => ToggleFullScreen();
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Hide_Click(object sender, RoutedEventArgs e) => Hide();
    private void AppsButton_Click(object sender, RoutedEventArgs e) { AppDrawer.Visibility = Visibility.Visible; Scrim.Visibility = Visibility.Visible; }
    private void CloseDrawer_Click(object sender, RoutedEventArgs e) => CloseDrawer();
    private void Scrim_Click(object sender, MouseButtonEventArgs e) => CloseDrawer();
    private void CloseDrawer() { AppDrawer.Visibility = Visibility.Collapsed; Scrim.Visibility = Visibility.Collapsed; }
    private void TitleBar_Drag(object sender, MouseButtonEventArgs e) { if (e.LeftButton == MouseButtonState.Pressed && !IsFullScreen) DragMove(); }
    private void Window_StateChanged(object? sender, EventArgs e) { if (WindowState == WindowState.Minimized) Hide(); }
    private void Window_Closing(object? sender, CancelEventArgs e) { if (!_reallyClose) { e.Cancel = true; Hide(); } }
    public void ReallyClose() { _reallyClose = true; Close(); }
}
