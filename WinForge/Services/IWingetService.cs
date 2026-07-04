using WinForge.Models;

namespace WinForge.Services;

public interface IWingetService
{
    Task<List<PackageInfo>> GetInstalledPackagesAsync();
    Task<List<PackageInfo>> GetUpgradablePackagesAsync();
    Task<List<PackageInfo>> SearchPackagesAsync(string query);
    Task<(bool Success, string Output)> InstallPackageAsync(string packageId);
    Task<(bool Success, string Output)> UpgradePackageAsync(string packageId);
    Task<(bool Success, string Output)> UpgradeAllAsync();
    Task<(bool Success, string Output)> UninstallPackageAsync(string packageId);
}
