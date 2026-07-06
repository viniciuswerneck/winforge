using CommunityToolkit.Mvvm.ComponentModel;

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

public enum UpgradeStatus { Pending, InProgress, Success, Failed }

public partial class UpgradeResult : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private UpgradeStatus _status = UpgradeStatus.Pending;

    [ObservableProperty]
    private string? _errorMessage;
}
