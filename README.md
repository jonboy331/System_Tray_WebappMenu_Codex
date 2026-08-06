# Orbit — tray web app workspace

Orbit is a Windows system-tray application that opens configured web applications inside one tabbed, contained window.

## Run

Requirements: Windows 10/11, .NET 8 SDK, and the Microsoft Edge WebView2 Runtime (included with current Windows/Edge installations).

```powershell
dotnet restore
dotnet run -- --show
```

Without `--show`, Orbit starts quietly in the notification area. Left-click the tray icon to show the main window, or right-click it to launch an app directly.

## Use

- Open **Apps** to launch, add, or remove web apps.
- Open apps appear as buttons across the top.
- **Back**, **Reload**, and **Close current** control the selected web app.
- Use the square title-bar control to switch between windowed and full-screen modes.
- Closing or minimizing the main window returns Orbit to the tray. Choose **Exit** from the tray menu to quit.

Menu configuration is stored in `%APPDATA%\OrbitWebApps\webapps.json`.
