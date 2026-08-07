namespace TrayWebApps.Models;

public sealed class AppSettings
{
    public string AssetId { get; set; } = "";
    public string AccentColor { get; set; } = "#FF6B6B";
    public bool StartMaximised { get; set; } = true;
    public bool HideWindowControls { get; set; } = true;
    public string? ClosePasswordHash { get; set; }
    public bool AlwaysOnTop { get; set; } = true;
    public bool StartAsWidget { get; set; } = true;
    public string WidgetText { get; set; } = "WEB APP MENU";
    public double WidgetWidth { get; set; } = 140;
    public double WidgetHeight { get; set; } = 140;
    public double? WidgetX { get; set; }
    public double? WidgetY { get; set; }
    public bool WidgetLocked { get; set; } = true;
    public string? WidgetImagePath { get; set; }
}
