using System.Collections.ObjectModel;
using System.Reflection;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinForge.Models;
using WinForge.Services;

namespace WinForge.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IWingetService _wingetService;
    private readonly ThemeService _themeService;
    private CancellationTokenSource? _searchDebounceCts;

    [ObservableProperty]
    private ObservableCollection<PackageViewModel> _installedPackages = [];

    private List<PackageViewModel> _allInstalled = [];

    [ObservableProperty]
    private ObservableCollection<PackageViewModel> _upgradablePackages = [];

    [ObservableProperty]
    private ObservableCollection<PackageViewModel> _searchResults = [];

    [ObservableProperty]
    private PackageViewModel? _selectedInstalledPackage;

    [ObservableProperty]
    private PackageViewModel? _selectedUpgradablePackage;

    [ObservableProperty]
    private PackageViewModel? _selectedSearchResult;

    [ObservableProperty]
    private string _searchQuery = string.Empty;

    [ObservableProperty]
    private string _statusMessage = "Pronto";

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private bool _isDarkTheme = true;

    [ObservableProperty]
    private bool _showOnboarding;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInstalledPackages))]
    [NotifyPropertyChangedFor(nameof(HasNoInstalledPackages))]
    [NotifyPropertyChangedFor(nameof(ShowInstalledEmpty))]
    [NotifyPropertyChangedFor(nameof(ShowInstalledNoResults))]
    private int _installedCount;

    [ObservableProperty]
    private int _totalInstalledCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsInstalledFilterActive))]
    [NotifyPropertyChangedFor(nameof(ShowInstalledEmpty))]
    [NotifyPropertyChangedFor(nameof(ShowInstalledNoResults))]
    private string _installedFilterQuery = string.Empty;

    [ObservableProperty]
    private ObservableCollection<string> _sourceFilters = ["Todos"];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowInstalledEmpty))]
    [NotifyPropertyChangedFor(nameof(ShowInstalledNoResults))]
    private string? _selectedSourceFilter = "Todos";

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasUpdates))]
    [NotifyPropertyChangedFor(nameof(HasNoUpdates))]
    private int _updatesCount;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSearchResults))]
    [NotifyPropertyChangedFor(nameof(HasNoSearchResults))]
    private int _searchResultsCount;

    public bool HasInstalledPackages => InstalledCount > 0;

    public bool HasUpdates => UpdatesCount > 0;

    public bool HasSearchResults => SearchResultsCount > 0;

    public bool HasNoInstalledPackages => InstalledCount == 0;

    public bool HasNoUpdates => UpdatesCount == 0;

    public bool HasNoSearchResults => SearchResultsCount == 0;

    public bool IsInstalledFilterActive => !string.IsNullOrWhiteSpace(InstalledFilterQuery);
    public bool ShowInstalledEmpty => InstalledCount == 0 && !IsInstalledFilterActive;
    public bool ShowInstalledNoResults => InstalledCount == 0 && IsInstalledFilterActive;

    public string AppVersion
    {
        get
        {
            var v = Assembly.GetExecutingAssembly().GetName().Version;
            return v is not null ? $"v{v.Major}.{v.Minor}.{v.Build}" : "v1.0";
        }
    }

    private static readonly string ConfigPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinForge", "config.json");

    public MainViewModel(IWingetService wingetService, ThemeService themeService)
    {
        _wingetService = wingetService;
        _themeService = themeService;
        ShowOnboarding = !IsOnboardingCompleted();
    }

    private static bool IsOnboardingCompleted()
    {
        try
        {
            if (!System.IO.File.Exists(ConfigPath)) return false;
            var json = System.IO.File.ReadAllText(ConfigPath);
            return json.Contains("\"onboarding\":true");
        }
        catch { return false; }
    }

    private static void SetOnboardingCompleted()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(ConfigPath);
            if (dir is not null) System.IO.Directory.CreateDirectory(dir);
            System.IO.File.WriteAllText(ConfigPath, "{\"onboarding\":true}");
        }
        catch { }
    }

    [RelayCommand]
    private void DismissOnboarding()
    {
        ShowOnboarding = false;
        SetOnboardingCompleted();
    }

    [RelayCommand]
    private async Task LoadInstalledAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Carregando pacotes instalados...";

        try
        {
            var packages = await _wingetService.GetInstalledPackagesAsync();
            _allInstalled = packages.Select(PackageViewModel.FromModel).ToList();
            TotalInstalledCount = _allInstalled.Count;

            var sources = _allInstalled.Select(p => p.Source).Where(s => !string.IsNullOrWhiteSpace(s)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(s => s).ToList();
            SourceFilters.Clear();
            SourceFilters.Add("Todos");
            foreach (var s in sources)
                SourceFilters.Add(s);
            SelectedSourceFilter = "Todos";

            ApplyInstalledFilter();
            StatusMessage = $"{TotalInstalledCount} pacotes instalados";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao carregar: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task LoadUpdatesAsync()
    {
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = "Verificando atualizações...";

        try
        {
            var packages = await _wingetService.GetUpgradablePackagesAsync();
            UpgradablePackages.Clear();
            foreach (var pkg in packages)
                UpgradablePackages.Add(PackageViewModel.FromModel(pkg));
            UpdatesCount = UpgradablePackages.Count;
            StatusMessage = UpdatesCount > 0
                ? $"{UpdatesCount} atualizações disponíveis"
                : "Todos os pacotes estão atualizados";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro ao verificar atualizações: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task SearchPackagesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        if (IsBusy) return;
        IsBusy = true;
        StatusMessage = $"Buscando por '{SearchQuery}'...";

        try
        {
            var results = await _wingetService.SearchPackagesAsync(SearchQuery);
            SearchResults.Clear();
            foreach (var pkg in results)
                SearchResults.Add(PackageViewModel.FromModel(pkg));
            SearchResultsCount = SearchResults.Count;
            StatusMessage = $"{SearchResultsCount} resultados encontrados";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro na busca: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task InstallPackageAsync(PackageViewModel? package)
    {
        if (package == null || package.IsBusy) return;
        package.IsBusy = true;
        StatusMessage = $"Instalando {package.Id}...";

        try
        {
            var (success, output) = await _wingetService.InstallPackageAsync(package.Id);
            StatusMessage = success
                ? $"{package.Name} instalado com sucesso"
                : $"Falha ao instalar {package.Name}";

            if (success)
            {
                SearchResults.Remove(package);
                await LoadInstalledAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            package.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UpgradePackageAsync(PackageViewModel? package)
    {
        if (package == null || package.IsBusy) return;
        package.IsBusy = true;
        StatusMessage = $"Atualizando {package.Id}...";

        try
        {
            var (success, _) = await _wingetService.UpgradePackageAsync(package.Id);
            StatusMessage = success
                ? $"{package.Name} atualizado com sucesso"
                : $"Falha ao atualizar {package.Name}";

            if (success)
            {
                UpgradablePackages.Remove(package);
                UpdatesCount = UpgradablePackages.Count;
                await LoadInstalledAsync();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            package.IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UpgradeAllAsync()
    {
        if (IsBusy || UpgradablePackages.Count == 0) return;
        IsBusy = true;
        StatusMessage = "Atualizando todos os pacotes...";

        try
        {
            var (success, output) = await _wingetService.UpgradeAllAsync();
            StatusMessage = success
                ? "Todos os pacotes foram atualizados"
                : "Algumas atualizações falharam";

            UpgradablePackages.Clear();
            UpdatesCount = 0;
            await LoadInstalledAsync();
            await LoadUpdatesAsync();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task UninstallPackageAsync(PackageViewModel? package)
    {
        if (package == null || package.IsBusy) return;
        package.IsBusy = true;
        StatusMessage = $"Removendo {package.Id}...";

        try
        {
            var (success, _) = await _wingetService.UninstallPackageAsync(package.Id);
            StatusMessage = success
                ? $"{package.Name} removido com sucesso"
                : $"Falha ao remover {package.Name}";

            if (success)
            {
                _allInstalled.RemoveAll(p => p.Id == package.Id);
                TotalInstalledCount = _allInstalled.Count;
                ApplyInstalledFilter();
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            package.IsBusy = false;
        }
    }

    [RelayCommand]
    private void ClearInstalledFilter()
    {
        InstalledFilterQuery = string.Empty;
    }

    private void ApplyInstalledFilter()
    {
        var filtered = _allInstalled.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(InstalledFilterQuery))
        {
            var q = InstalledFilterQuery.Trim().ToLowerInvariant();
            filtered = filtered.Where(p =>
                p.Name.Contains(q, StringComparison.OrdinalIgnoreCase) ||
                p.Id.Contains(q, StringComparison.OrdinalIgnoreCase));
        }

        if (SelectedSourceFilter is not null && SelectedSourceFilter != "Todos")
        {
            filtered = filtered.Where(p =>
                string.Equals(p.Source, SelectedSourceFilter, StringComparison.OrdinalIgnoreCase));
        }

        InstalledPackages.Clear();
        foreach (var pkg in filtered)
            InstalledPackages.Add(pkg);
        InstalledCount = InstalledPackages.Count;
    }

    partial void OnSelectedSourceFilterChanged(string? value)
    {
        ApplyInstalledFilter();
    }

    partial void OnInstalledFilterQueryChanged(string value)
    {
        ApplyInstalledFilter();
    }

    partial void OnSearchQueryChanged(string value)
    {
        _searchDebounceCts?.Cancel();

        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
            SearchResultsCount = 0;
            StatusMessage = "Pronto";
            return;
        }

        var cts = new CancellationTokenSource();
        _searchDebounceCts = cts;
        var token = cts.Token;

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(300, token);
                if (!token.IsCancellationRequested)
                {
                    await Application.Current.Dispatcher.InvokeAsync(async () =>
                    {
                        if (!string.IsNullOrWhiteSpace(SearchQuery))
                            await SearchPackagesAsync();
                    });
                }
            }
            catch (TaskCanceledException) { }
        }, token);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        _searchDebounceCts?.Cancel();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _themeService.ToggleTheme();
        if (App.Current.MainWindow is Window w)
            ThemeService.SetWindowTitleBarDark(w, IsDarkTheme);
    }
}
