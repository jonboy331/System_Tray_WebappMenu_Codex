using System.Drawing;
using System.Windows;
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

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        DispatcherUnhandledException += (_, args) =>
        {
            System.Windows.MessageBox.Show(args.Exception.Message, "Orbit", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };
        ApplyAccentColor(new SettingsStore().Load().AccentColor);
        _window = new MainWindow();
        MainWindow = _window;

        _tray = new Forms.NotifyIcon
        {
            Icon = CreateTrayIcon(),
            Text = "Orbit — web app launcher",
            Visible = true,
            ContextMenuStrip = new Forms.ContextMenuStrip()
        };
        _tray.DoubleClick += (_, _) => ShowWindow();
        _tray.MouseClick += (_, args) =>
        {
            if (args.Button != Forms.MouseButtons.Left) return;
            if (_window is not null && _window.Settings.SingleClickTrayMenu)
                _tray.ContextMenuStrip?.Show(Forms.Cursor.Position);
            else
                ShowWindow();
        };
        _window.AppsChanged += (_, _) => BuildTrayMenu();
        BuildTrayMenu();

        ShowWindow();
        if (_window.Settings.StartMaximised && !_window.IsFullScreen) _window.ToggleFullScreen();
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
        _window.Show();
        if (_window.WindowState == WindowState.Minimized) _window.WindowState = WindowState.Normal;
        _window.Activate();
        _window.Topmost = true;
        _window.Topmost = false;
        _window.Focus();
    }

    private void ExitApplication()
    {
        if (_window is not null && !_window.ConfirmClose()) return;
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
