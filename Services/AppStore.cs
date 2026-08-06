using System.Text.Json;
using System.IO;
using TrayWebApps.Models;

namespace TrayWebApps.Services;

public sealed class AppStore
{
    private readonly string _path = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OrbitWebApps", "webapps.json");

    public List<WebAppDefinition> Load()
    {
        try
        {
            if (File.Exists(_path))
                return JsonSerializer.Deserialize<List<WebAppDefinition>>(File.ReadAllText(_path)) ?? [];
        }
        catch { /* A malformed config falls back to safe defaults. */ }

        return
        [
            new() { Name = "Microsoft 365", Url = "https://www.microsoft365.com" },
            new() { Name = "Outlook", Url = "https://outlook.office.com/mail" },
            new() { Name = "Teams", Url = "https://teams.microsoft.com" }
        ];
    }

    public void Save(IEnumerable<WebAppDefinition> apps)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        File.WriteAllText(_path, JsonSerializer.Serialize(apps, new JsonSerializerOptions { WriteIndented = true }));
    }
}
