namespace TrayWebApps.Models;

public sealed class AppSettings
{
    public string AssetId { get; set; } = "";
    public string AccentColor { get; set; } = "#78E3C5";
    public bool StartMaximised { get; set; }
    public bool SingleClickTrayMenu { get; set; }
    public bool HideWindowControls { get; set; }
    public string? ClosePasswordHash { get; set; }
}
