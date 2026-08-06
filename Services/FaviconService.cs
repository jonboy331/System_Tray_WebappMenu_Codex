using System.IO;
using System.Net.Http;

namespace TrayWebApps.Services;

public static class FaviconService
{
    private static readonly HttpClient Client = new();
    private static readonly string IconDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OrbitWebApps", "icons");

    public static async Task<string?> FetchAsync(string url, Guid appId)
    {
        try
        {
            Directory.CreateDirectory(IconDir);
            var faviconUrl = $"https://www.google.com/s2/favicons?sz=64&domain_url={Uri.EscapeDataString(url)}";
            var bytes = await Client.GetByteArrayAsync(faviconUrl);
            if (bytes.Length == 0) return null;
            var path = Path.Combine(IconDir, $"{appId}.png");
            await File.WriteAllBytesAsync(path, bytes);
            return path;
        }
        catch
        {
            return null;
        }
    }

    public static string CopyManualIcon(string sourceFilePath, Guid appId)
    {
        Directory.CreateDirectory(IconDir);
        var extension = Path.GetExtension(sourceFilePath) is { Length: > 0 } ext ? ext : ".png";
        var path = Path.Combine(IconDir, $"{appId}{extension}");
        File.Copy(sourceFilePath, path, overwrite: true);
        return path;
    }
}
