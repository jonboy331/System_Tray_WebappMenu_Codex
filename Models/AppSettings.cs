namespace TrayWebApps.Models;

public sealed class AppSettings
{
    public string AssetId { get; set; } = "";
    public string AccentColor { get; set; } = "#78E3C5";
    public bool StartMaximised { get; set; }
    public bool HideWindowControls { get; set; }
    public string? ClosePasswordHash { get; set; }
    public bool AlwaysOnTop { get; set; }
    public bool StartAsWidget { get; set; }
    public string WidgetText { get; set; } = "Apps";
    public double WidgetWidth { get; set; } = 140;
    public double WidgetHeight { get; set; } = 48;
    public double? WidgetX { get; set; }
    public double? WidgetY { get; set; }
    public bool WidgetLocked { get; set; }
}
