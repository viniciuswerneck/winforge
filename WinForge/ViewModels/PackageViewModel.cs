using CommunityToolkit.Mvvm.ComponentModel;
using WinForge.Models;

namespace WinForge.ViewModels;

public partial class PackageViewModel : ObservableObject
{
    private static readonly string[] Colors = ["#FF60B0FF", "#FFF0A030", "#FF4CCB70", "#FFBE70FF", "#FFE04343", "#FF70D0E0", "#FFD070A0", "#FFA0D060"];

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _id = string.Empty;

    [ObservableProperty]
    private string _version = string.Empty;

    [ObservableProperty]
    private string? _availableVersion;

    [ObservableProperty]
    private string _source = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public bool HasUpdate => !string.IsNullOrEmpty(AvailableVersion);

    public string Initial => Name.Length > 0 ? Name[..1].ToUpper() : "?";

    public string InitialColor
    {
        get
        {
            uint hash = 2166136261;
            foreach (char c in Name)
                hash = (hash ^ c) * 16777619;
            return Colors[hash % (uint)Colors.Length];
        }
    }

    public static PackageViewModel FromModel(PackageInfo info) => new()
    {
        Name = info.Name,
        Id = info.Id,
        Version = info.Version,
        AvailableVersion = info.AvailableVersion,
        Source = info.Source
    };
}
