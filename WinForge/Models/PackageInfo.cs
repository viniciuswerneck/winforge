namespace WinForge.Models;

public record PackageInfo
{
    public string Name { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string? AvailableVersion { get; init; }
    public string Source { get; init; } = string.Empty;
    public bool HasUpdate => !string.IsNullOrEmpty(AvailableVersion);
}
