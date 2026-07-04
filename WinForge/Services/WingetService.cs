using System.Diagnostics;
using System.Text.RegularExpressions;
using WinForge.Models;

namespace WinForge.Services;

public partial class WingetService : IWingetService
{
    private const string WingetExe = "winget";

    public async Task<List<PackageInfo>> GetInstalledPackagesAsync()
    {
        var (_, output) = await RunWingetAsync("list --accept-source-agreements");
        return ParseWingetListOutput(output, hasAvailable: false);
    }

    public async Task<List<PackageInfo>> GetUpgradablePackagesAsync()
    {
        var (_, output) = await RunWingetAsync("upgrade --accept-source-agreements");
        return ParseWingetListOutput(output, hasAvailable: true);
    }

    public async Task<List<PackageInfo>> SearchPackagesAsync(string query)
    {
        var (_, output) = await RunWingetAsync($"search \"{query}\" --accept-source-agreements");
        return ParseWingetListOutput(output, hasAvailable: false);
    }

    public async Task<(bool Success, string Output)> InstallPackageAsync(string packageId)
    {
        var (exitCode, output) = await RunWingetAsync($"install --exact --id \"{packageId}\" --silent --force --disable-interactivity --accept-source-agreements --accept-package-agreements");
        return (exitCode == 0 && (output.Contains("instalado com êxito") || output.Contains("successfully installed")), output);
    }

    public async Task<(bool Success, string Output)> UpgradePackageAsync(string packageId)
    {
        var (exitCode, output) = await RunWingetAsync($"upgrade --exact --id \"{packageId}\" --silent --force --disable-interactivity --accept-source-agreements --accept-package-agreements");
        return (exitCode == 0, output);
    }

    public async Task<(bool Success, string Output)> UpgradeAllAsync()
    {
        var (exitCode, output) = await RunWingetAsync("upgrade --all --silent --force --disable-interactivity --accept-source-agreements --accept-package-agreements");
        return (exitCode == 0, output);
    }

    public async Task<(bool Success, string Output)> UninstallPackageAsync(string packageId)
    {
        var (exitCode, output) = await RunWingetAsync($"uninstall --exact --id \"{packageId}\" --silent --force --disable-interactivity --accept-source-agreements", 120000);
        return (exitCode == 0 || output.Contains("desinstalado com êxito") || output.Contains("successfully uninstalled") || output.Contains("desinstalação concluída"), output);
    }

    private static async Task<(int ExitCode, string Output)> RunWingetAsync(string arguments, int timeoutMs = 120000)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = WingetExe,
                Arguments = arguments,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            }
        };

        process.Start();

        var readStdout = process.StandardOutput.ReadToEndAsync();
        var readStderr = process.StandardError.ReadToEndAsync();

        // Wait for process to exit (or timeout)
        if (await Task.WhenAny(readStdout, Task.Delay(timeoutMs)) != readStdout)
        {
            process.Kill();
            var partialOut = await readStdout;
            var partialErr = readStderr.IsCompleted ? await readStderr : "";
            return (-1, partialOut + partialErr + "\n[TIMEOUT]");
        }

        var output = await readStdout;
        var error = readStderr.IsCompleted ? await readStderr : "";
        process.WaitForExit();

        return (process.ExitCode, output + error);
    }

    private static List<PackageInfo> ParseWingetListOutput(string output, bool hasAvailable)
    {
        var packages = new List<PackageInfo>();
        var lines = output.Split('\n', StringSplitOptions.RemoveEmptyEntries);

        bool headerFound = false;
        foreach (var line in lines)
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith("---") || trimmed.StartsWith("─"))
            {
                headerFound = true;
                continue;
            }

            if (!headerFound || string.IsNullOrWhiteSpace(trimmed))
                continue;

            if (trimmed.Contains("Nenhum pacote") || trimmed.Contains("No package"))
                continue;

            if (trimmed.StartsWith("Nome") || trimmed.StartsWith("Name"))
                continue;

            if (trimmed.Contains("marca d'água") || trimmed.Contains("watermark"))
                continue;

            var package = ParsePackageLine(trimmed, hasAvailable);
            if (package != null && !string.IsNullOrWhiteSpace(package.Id))
                packages.Add(package);
        }

        return packages;
    }

    private static PackageInfo? ParsePackageLine(string line, bool hasAvailable)
    {
        try
        {
            var parts = SplitLinePreservingSpaces(line);
            if (parts.Count < 3)
                return null;

            var name = parts[0];
            var id = parts[1];
            var version = parts[2];
            string? availableVersion = null;
            var source = string.Empty;

            // hasAvailable: columns are Name, Id, Version, Available, Source
            // no available:  columns are Name, Id, Version, Source
            if (hasAvailable)
            {
                if (parts.Count >= 4) availableVersion = parts[3];
                if (parts.Count >= 5) source = parts[4];
            }
            else
            {
                if (parts.Count >= 4) source = parts[3];
            }

            return new PackageInfo
            {
                Name = name.Trim(),
                Id = id.Trim(),
                Version = version.Trim(),
                AvailableVersion = availableVersion?.Trim(),
                Source = source.Trim()
            };
        }
        catch
        {
            return null;
        }
    }

    private static List<string> SplitLinePreservingSpaces(string line)
    {
        var parts = new List<string>();
        var matches = PackageColumnPattern().Matches(line);

        foreach (Match match in matches)
        {
            parts.Add(match.Value.Trim());
        }

        return parts;
    }

    [GeneratedRegex(@"\S+(\s+\S+)*?(?=\s{2,}|\n|$)", RegexOptions.Multiline)]
    private static partial Regex PackageColumnPattern();
}
