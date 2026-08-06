namespace TrayWebApps.Models;

public sealed class WebAppDefinition
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = "New app";
    public string Url { get; set; } = "https://example.com";
    public string? IconPath { get; set; }
}
