using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows;
using System.Windows.Interop;
using TrayWebApps.Services;
using Forms = System.Windows.Forms;
using MediaColor = System.Windows.Media.Color;
using MediaColorConverter = System.Windows.Media.ColorConverter;
using SolidColorBrush = System.Windows.Media.SolidColorBrush;

namespace TrayWebApps;

public partial class App : System.Windows.Application
{
    private Forms.NotifyIcon? _tray;
    private MainWindow? _window;
    private WidgetWindow? _widgetWindow;
    private Mutex? _singleInstanceMutex;
    private readonly SettingsStore _settingsStore = new();

    [DllImport("user32.dll")] private static extern IntPtr FindWindow(string? lpClassName, string lpWindowName);
    [DllImport("user32.dll")] private static extern bool PostMessage(IntPtr hWnd, int msg, IntPtr wParam, IntPtr lParam);
    [DllImport("user32.dll")] private static extern int RegisterWindowMessage(string lpString);
    private static readonly int ShowRequestMessage = RegisterWindowMessage("OrbitWebApps.ShowRequest");

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _singleInstanceMutex = new Mutex(true, "OrbitWebApps.SingleInstance", out var isNewInstance);
        if (!isNewInstance)
        {
            var existing = FindWindow(null, "Orbit");
            if (existing != IntPtr.Zero) PostMessage(existing, ShowRequestMessage, IntPtr.Zero, IntPtr.Zero);
            Shutdown();
            return;
        }

        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(args.Exception.Message, "Orbit", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        ApplyAccentColor(_settingsStore.Load().AccentColor);
        _window = new MainWindow();
        MainWindow = _window;
        var windowHandle = new WindowInteropHelper(_window).EnsureHandle();
        HwndSource.FromHwnd(windowHandle)?.AddHook(WndProc);

        _tray = new Forms.NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "Orbit — web app launcher",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
        _tray.DoubleClick += (_, _) => ShowWindow();
        _tray.MouseClick += (_, args) => { if (args.Button == Forms.MouseButtons.Left) ShowWindow(); };
        _window.AppsChanged += (_, _) => BuildTrayMenu();
        _window.MinimizeToWidgetRequested += (_, _) => ShowWidget();
        BuildTrayMenu();

        if (_window.Settings.StartAsWidget)
        {
            ShowWidget();
        }
        else
        {
            ShowWindow();
            if (_window.Settings.StartMaximised && !_window.IsFullScreen) _window.ToggleFullScreen();
        }
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == ShowRequestMessage)
        {
            ShowWindow();
            handled = true;
        }
        return IntPtr.Zero;
    }

    private void BuildTrayMenu()
    {
        if (_tray?.ContextMenuStrip is not { } menu || _window is null) return;
        menu.Items.Clear();

        foreach (var app in _window.ConfiguredApps)
        {
            var item = new Forms.ToolStripMenuItem(app.Name) { Tag = app.Id };
            item.Click += (_, _) => { ShowWindow(); _window.OpenApp((Guid)item.Tag!); };
            menu.Items.Add(item);
        }

        menu.Items.Add(new Forms.ToolStripSeparator());
        var show = menu.Items.Add("Open Orbit");
        show.Click += (_, _) => ShowWindow();
        var display = menu.Items.Add(_window.IsFullScreen ? "Windowed mode" : "Full screen");
        display.Click += (_, _) => { ShowWindow(); _window.ToggleFullScreen(); };
        menu.Items.Add(new Forms.ToolStripSeparator());
        var exit = menu.Items.Add("Exit");
        exit.Click += (_, _) => ExitApplication();
    }

    private void ShowWindow()
    {
        if (_window is null) return;
        _widgetWindow?.Hide();
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
        var alwaysOnTop = _window.Settings.AlwaysOnTop;
        _window.Topmost = true;
        if (!alwaysOnTop) _window.Topmost = false;
        _window.Focus();
    }

    private void ShowWidget()
    {
        if (_window is null) return;
        _window.Hide();
        _widgetWindow?.Close();
        _widgetWindow = new WidgetWindow(_window.Settings, _settingsStore);
        _widgetWindow.OpenMenuRequested += (_, _) =>
        {
            ShowWindow();
            _window?.OpenDrawer();
        };
        _widgetWindow.Show();
    }

    public void ExitApplication()
    {
        if (_window is not null && !_window.ConfirmClose()) return;
        _widgetWindow?.Close();
        _window?.ReallyClose();
        if (_tray is not null) { _tray.Visible = false; _tray.Dispose(); }
        Shutdown();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _tray?.Dispose();
        base.OnExit(e);
    }

    public static void ApplyAccentColor(string hex)
    {
        try
        {
            var color = (MediaColor)MediaColorConverter.ConvertFromString(hex)!;
            Current.Resources["Accent"] = new SolidColorBrush(color);
        }
        catch { /* invalid hex falls back to the current accent */ }
    }

    private static Icon CreateTrayIcon()
    {
        using var bitmap = new Bitmap(32, 32);
        using var graphics = Graphics.FromImage(bitmap);
        graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
        graphics.Clear(Color.Transparent);
        using var dark = new SolidBrush(Color.FromArgb(20, 25, 34));
        using var mint = new SolidBrush(Color.FromArgb(120, 227, 197));
        graphics.FillEllipse(dark, 1, 1, 30, 30);
        graphics.FillEllipse(mint, 7, 7, 18, 18);
        graphics.FillEllipse(dark, 12, 12, 8, 8);
        return Icon.FromHandle(bitmap.GetHicon());
    }
}
