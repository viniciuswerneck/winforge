using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WinForge.Models;
using WinForge.Services;

namespace WinForge.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly IWingetService _wingetService;
    private readonly IThemeService _themeService;
    private readonly IUserConfigurationService _userConfigService;
    private readonly SemaphoreSlim _operationGate = new(1, 1);
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
            var v = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version;
            return v is not null ? $"v{v.Major}.{v.Minor}.{v.Build}" : "v1.0";
        }
    }

    public MainViewModel(IWingetService wingetService, IThemeService themeService, IUserConfigurationService userConfigService)
    {
        _wingetService = wingetService;
        _themeService = themeService;
        _userConfigService = userConfigService;
        ShowOnboarding = !_userConfigService.IsOnboardingCompleted();
    }

    [RelayCommand]
    private void DismissOnboarding()
    {
        ShowOnboarding = false;
        _userConfigService.SetOnboardingCompleted();
    }

    [RelayCommand]
    private async Task LoadInstalledAsync()
    {
        await _operationGate.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Carregando pacotes instalados...";

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
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private async Task LoadUpdatesAsync()
    {
        await _operationGate.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Verificando atualizações...";

            var packages = await _wingetService.GetUpgradablePackagesAsync();
            UpgradablePackages = new ObservableCollection<PackageViewModel>(
                packages.Select(PackageViewModel.FromModel));
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
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private async Task SearchPackagesAsync()
    {
        if (string.IsNullOrWhiteSpace(SearchQuery)) return;
        await _operationGate.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = $"Buscando por '{SearchQuery}'...";

            var results = await _wingetService.SearchPackagesAsync(SearchQuery);
            SearchResults = new ObservableCollection<PackageViewModel>(
                results.Select(PackageViewModel.FromModel));
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
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private async Task InstallPackageAsync(PackageViewModel? package)
    {
        if (package == null || package.IsBusy) return;
        await _operationGate.WaitAsync();
        package.IsBusy = true;
        try
        {
            IsBusy = true;
            StatusMessage = $"Instalando {package.Id}...";

            var (success, _) = await _wingetService.InstallPackageAsync(package.Id);
            StatusMessage = success
                ? $"{package.Name} instalado com sucesso"
                : $"Falha ao instalar {package.Name}";

            if (success)
            {
                SearchResults.Remove(package);
                _allInstalled.Add(package);
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
            IsBusy = false;
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private async Task UpgradePackageAsync(PackageViewModel? package)
    {
        if (package == null || package.IsBusy) return;
        await _operationGate.WaitAsync();
        package.IsBusy = true;
        try
        {
            IsBusy = true;
            StatusMessage = $"Atualizando {package.Id}...";

            var (success, _) = await _wingetService.UpgradePackageAsync(package.Id);
            StatusMessage = success
                ? $"{package.Name} atualizado com sucesso"
                : $"Falha ao atualizar {package.Name}";

            if (success)
            {
                UpgradablePackages.Remove(package);
                UpdatesCount = UpgradablePackages.Count;
                // Update version in installed list optimistically
                var installed = _allInstalled.FirstOrDefault(p => p.Id == package.Id);
                if (installed is not null)
                    installed.Version = package.AvailableVersion ?? installed.Version;
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            package.IsBusy = false;
            IsBusy = false;
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private async Task UpgradeAllAsync()
    {
        await _operationGate.WaitAsync();
        try
        {
            IsBusy = true;
            StatusMessage = "Atualizando todos os pacotes...";

            var (success, _) = await _wingetService.UpgradeAllAsync();
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
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private async Task UninstallPackageAsync(PackageViewModel? package)
    {
        if (package == null || package.IsBusy) return;
        await _operationGate.WaitAsync();
        package.IsBusy = true;
        try
        {
            IsBusy = true;
            StatusMessage = $"Removendo {package.Id}...";

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
            IsBusy = false;
            _operationGate.Release();
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

        InstalledPackages = new ObservableCollection<PackageViewModel>(filtered);
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
        _searchDebounceCts?.Dispose();

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
            catch (Exception ex)
            {
                await Application.Current.Dispatcher.InvokeAsync(() =>
                {
                    StatusMessage = $"Erro na busca: {ex.Message}";
                });
            }
        }, token);
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
        _searchDebounceCts?.Cancel();
        _searchDebounceCts?.Dispose();
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _themeService.ToggleTheme();
    }
}
