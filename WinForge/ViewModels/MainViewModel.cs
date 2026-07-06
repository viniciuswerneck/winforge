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

    [ObservableProperty]
    private ObservableCollection<UpgradeResult> _batchResults = [];

    [ObservableProperty]
    private bool _isBatchUpgrading;

    [ObservableProperty]
    private bool _hasBatchResults;

    [ObservableProperty]
    private int _batchCurrentIndex;

    [ObservableProperty]
    private int _batchTotal;

    [ObservableProperty]
    private bool _showPackageInfo;

    [ObservableProperty]
    private string _packageInfoTitle = string.Empty;

    [ObservableProperty]
    private string _packageInfoDetails = string.Empty;

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

            var (success, output) = await _wingetService.UpgradePackageAsync(package.Id);
            StatusMessage = success
                ? $"{package.Name} atualizado com sucesso"
                : $"Falha ao atualizar {package.Name}";

            if (success)
            {
                UpgradablePackages.Remove(package);
                UpdatesCount = UpgradablePackages.Count;
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
            var packages = UpgradablePackages.ToList();
            if (packages.Count == 0) return;

            BatchResults.Clear();
            foreach (var p in packages)
                BatchResults.Add(new UpgradeResult { Name = p.Name, Id = p.Id, Status = UpgradeStatus.Pending });
            IsBatchUpgrading = true;
            BatchTotal = packages.Count;
            HasBatchResults = true;

            int successCount = 0;
            int failCount = 0;

            for (int i = 0; i < packages.Count; i++)
            {
                var pkg = packages[i];
                BatchCurrentIndex = i + 1;
                StatusMessage = $"[{i + 1}/{packages.Count}] Atualizando {pkg.Name}...";

                BatchResults[i].Status = UpgradeStatus.InProgress;

                var (success, output) = await _wingetService.UpgradePackageAsync(pkg.Id);

                if (!success && IsHashMismatch(output))
                {
                    StatusMessage = $"[{i + 1}/{packages.Count}] {pkg.Name}: hash incompatível, tentando forçar...";
                    (success, output) = await _wingetService.UpgradePackageForceHashAsync(pkg.Id);
                }

                if (success)
                {
                    BatchResults[i].Status = UpgradeStatus.Success;
                    successCount++;
                    UpgradablePackages.Remove(pkg);
                    var installed = _allInstalled.FirstOrDefault(p => p.Id == pkg.Id);
                    if (installed is not null)
                        installed.Version = pkg.AvailableVersion ?? installed.Version;
                }
                else
                {
                    BatchResults[i].Status = UpgradeStatus.Failed;
                    var reason = ExtractFailureReason(output);
                    BatchResults[i].ErrorMessage = reason;
                    failCount++;
                }
            }

            UpdatesCount = UpgradablePackages.Count;
            StatusMessage = $"Concluído: {successCount} atualizado(s), {failCount} falha(s)";
        }
        catch (Exception ex)
        {
            StatusMessage = $"Erro: {ex.Message}";
        }
        finally
        {
            IsBatchUpgrading = false;
            _operationGate.Release();
        }
    }

    [RelayCommand]
    private void DismissBatchResults()
    {
        BatchResults.Clear();
        IsBatchUpgrading = false;
        HasBatchResults = false;
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
        if (string.IsNullOrWhiteSpace(value))
        {
            SearchResults.Clear();
            SearchResultsCount = 0;
            StatusMessage = "Pronto";
        }
    }

    [RelayCommand]
    private void ClearSearch()
    {
        SearchQuery = string.Empty;
    }

    [RelayCommand]
    private void ToggleTheme()
    {
        IsDarkTheme = !IsDarkTheme;
        _themeService.ToggleTheme();
    }

    [RelayCommand]
    private async Task ShowPackageInfoAsync(PackageViewModel? package)
    {
        if (package == null) return;
        try
        {
            PackageInfoTitle = package.Name;
            PackageInfoDetails = "Carregando informações...";
            ShowPackageInfo = true;

            var details = await _wingetService.GetPackageDetailsAsync(package.Id);
            PackageInfoDetails = FormatPackageInfo(details);
        }
        catch (Exception ex)
        {
            PackageInfoDetails = $"Erro ao buscar informações: {ex.Message}";
        }
    }

    [RelayCommand]
    private void DismissPackageInfo()
    {
        ShowPackageInfo = false;
    }

    private static string FormatPackageInfo(string raw)
    {
        var lines = raw.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        var result = new List<string>();
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (string.IsNullOrEmpty(trimmed)) continue;
            if (trimmed.StartsWith("---")) continue;
            result.Add(trimmed);
        }
        return string.Join("\n", result);
    }

    private static string ExtractFailureReason(string output)
    {
        var lower = output.ToLowerInvariant();

        if (lower.Contains("hash do instalador não corresponde") || lower.Contains("installer hash does not match"))
            return "Hash do instalador desatualizado no manifesto winget";
        if (lower.Contains("privilégios de administrador") || lower.Contains("administrator privileges") || lower.Contains("0x80073d28"))
            return "Necessita executar como administrador";
        if (lower.Contains("nenhuma atualização disponível") || lower.Contains("no update was found"))
            return "Nenhuma atualização disponível";
        if (lower.Contains("no applicable update") || lower.Contains("nenhuma versão de pacote"))
            return "Nenhuma versão compatível";
        if (lower.Contains("nenhum pacote instalado") || lower.Contains("no installed package"))
            return "Pacote não reconhecido pelo winget";
        if (lower.Contains("an error occurred") || lower.Contains("ocorreu um erro"))
            return "Erro durante a operação";

        var lines = output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.Length > 0 && trimmed.Length < 120 && !trimmed.StartsWith("---"))
                return trimmed;
        }

        return "Erro desconhecido";
    }

    private static bool IsHashMismatch(string output)
    {
        var lower = output.ToLowerInvariant();
        return lower.Contains("hash do instalador não corresponde")
            || lower.Contains("installer hash does not match");
    }
}
