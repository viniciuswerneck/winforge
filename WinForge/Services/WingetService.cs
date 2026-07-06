using System.Diagnostics;
using System.Text.RegularExpressions;
using WinForge.Models;

namespace WinForge.Services;

public partial class WingetService : IWingetService
{
    private const string WingetExe = "winget";

    public async Task<List<PackageInfo>> GetInstalledPackagesAsync()
    {
        var (_, output) = await RunWingetAsync(["list", "--accept-source-agreements"]);
        return ParseWingetListOutput(output);
    }

    public async Task<List<PackageInfo>> GetUpgradablePackagesAsync()
    {
        var (_, output) = await RunWingetAsync(["upgrade", "--accept-source-agreements"]);
        return ParseWingetListOutput(output);
    }

    public async Task<List<PackageInfo>> SearchPackagesAsync(string query)
    {
        var (_, output) = await RunWingetAsync(["search", query, "--accept-source-agreements"]);
        return ParseWingetListOutput(output);
    }

    public async Task<(bool Success, string Output)> InstallPackageAsync(string packageId)
    {
        ValidatePackageId(packageId);
        var (exitCode, output) = await RunWingetAsync(
        [
            "install", "--exact", "--id", packageId,
            "--silent", "--force", "--disable-interactivity",
            "--accept-source-agreements", "--accept-package-agreements"
        ]);
        return (IsSuccess(exitCode, output), output);
    }

    public async Task<(bool Success, string Output)> UpgradePackageAsync(string packageId)
    {
        ValidatePackageId(packageId);
        var (exitCode, output) = await RunWingetAsync(
        [
            "upgrade", "--exact", "--id", packageId,
            "--silent", "--force", "--disable-interactivity",
            "--accept-source-agreements", "--accept-package-agreements"
        ]);
        return (IsSuccess(exitCode, output), output);
    }

    public async Task<(bool Success, string Output)> UpgradePackageForceHashAsync(string packageId)
    {
        ValidatePackageId(packageId);
        var (exitCode, output) = await RunWingetAsync(
        [
            "upgrade", "--exact", "--id", packageId,
            "--silent", "--force", "--ignore-security-hash", "--disable-interactivity",
            "--accept-source-agreements", "--accept-package-agreements"
        ]);
        return (IsSuccess(exitCode, output), output);
    }

    public async Task<(bool Success, string Output)> UpgradeAllAsync()
    {
        var (exitCode, output) = await RunWingetAsync(
        [
            "upgrade", "--all",
            "--silent", "--force", "--disable-interactivity",
            "--accept-source-agreements", "--accept-package-agreements"
        ], 600000);
        return (IsSuccess(exitCode, output), output);
    }

    public async Task<(bool Success, string Output)> UninstallPackageAsync(string packageId)
    {
        ValidatePackageId(packageId);
        var (exitCode, output) = await RunWingetAsync(
        [
            "uninstall", "--exact", "--id", packageId,
            "--silent", "--force", "--disable-interactivity",
            "--accept-source-agreements"
        ], 120000);
        return (IsSuccess(exitCode, output), output);
    }

    public async Task<string> GetPackageDetailsAsync(string packageId)
    {
        ValidatePackageId(packageId);
        var (_, output) = await RunWingetAsync(
        [
            "show", "--exact", "--id", packageId,
            "--accept-source-agreements"
        ]);
        return output;
    }

    private static bool IsSuccess(int exitCode, string output)
    {
        var lower = output.ToLowerInvariant();

        if (lower.Contains("nenhuma atualização disponível")
            || lower.Contains("no update was found")
            || lower.Contains("no applicable update")
            || lower.Contains("nenhuma versão de pacote")
            || lower.Contains("hash do instalador não corresponde")
            || lower.Contains("installer hash does not match")
            || lower.Contains("an error occurred")
            || lower.Contains("ocorreu um erro"))
            return false;

        if (exitCode == 0)
            return true;

        return lower.Contains("successfully installed")
            || lower.Contains("instalado com sucesso")
            || lower.Contains("successfully upgraded")
            || lower.Contains("atualizado com sucesso")
            || lower.Contains("successfully uninstalled")
            || lower.Contains("desinstalado com sucesso");
    }

    private static void ValidatePackageId(string packageId)
    {
        if (string.IsNullOrWhiteSpace(packageId))
            throw new ArgumentException("Package ID cannot be empty");
        if (!PackageIdPattern().IsMatch(packageId))
            throw new ArgumentException($"Package ID '{packageId}' contains invalid characters");
    }

    private static async Task<(int ExitCode, string Output)> RunWingetAsync(string[] arguments, int timeoutMs = 120000)
    {
        using var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = WingetExe,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
                StandardErrorEncoding = System.Text.Encoding.UTF8
            }
        };

        foreach (var arg in arguments)
            process.StartInfo.ArgumentList.Add(arg);

        process.Start();

        var readStdout = process.StandardOutput.ReadToEndAsync();
        var readStderr = process.StandardError.ReadToEndAsync();

        var bothStreams = Task.WhenAll(readStdout, readStderr);
        var completed = await Task.WhenAny(bothStreams, Task.Delay(timeoutMs));

        if (completed != bothStreams)
        {
            try { process.Kill(true); } catch { }
            try { await process.WaitForExitAsync(); } catch { }
            return (-1, "[TIMEOUT]");
        }

        var stdout = await readStdout;
        var stderr = await readStderr;

        return (process.ExitCode, stdout + stderr);
    }

    private static readonly Dictionary<string, string[]> ColumnAliases = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Name"] = ["Name", "Nome"],
        ["Id"] = ["Id", "ID"],
        ["Version"] = ["Version", "Versão", "Versao"],
        ["Available"] = ["Available", "Disponível", "Disponivel"],
        ["Source"] = ["Source", "Origem"]
    };

    private static List<PackageInfo> ParseWingetListOutput(string output)
    {
        var packages = new List<PackageInfo>();
        var lines = output.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries);

        int separatorIdx = -1;
        for (int i = 0; i < lines.Length; i++)
        {
            var trimmed = lines[i].Trim();
            if (trimmed.StartsWith("---") || trimmed.StartsWith("─"))
            {
                separatorIdx = i;
                break;
            }
        }

        if (separatorIdx < 1)
            return packages;

        var header = lines[separatorIdx - 1];
        var colNames = new[] { "Name", "Id", "Version", "Available", "Source" };
        var colStarts = new List<(string Name, int Start)>();

        foreach (var col in colNames)
        {
            if (!ColumnAliases.TryGetValue(col, out var aliases))
                aliases = [col];

            int bestPos = -1;
            foreach (var alias in aliases)
            {
                int pos = header.IndexOf(alias, StringComparison.OrdinalIgnoreCase);
                if (pos >= 0 && (bestPos < 0 || pos < bestPos))
                    bestPos = pos;
            }
            if (bestPos >= 0)
                colStarts.Add((col, bestPos));
        }

        if (colStarts.Count < 3)
            return packages;

        for (int i = separatorIdx + 1; i < lines.Length; i++)
        {
            var line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            if (line.Contains("Nenhum pacote") || line.Contains("No package"))
                continue;

            var name = ExtractColumn(line, colStarts, "Name");
            var id = ExtractColumn(line, colStarts, "Id");
            var version = ExtractColumn(line, colStarts, "Version");
            var available = ExtractColumn(line, colStarts, "Available");
            var source = ExtractColumn(line, colStarts, "Source");

            if (string.IsNullOrWhiteSpace(id))
                continue;

            packages.Add(new PackageInfo
            {
                Name = name,
                Id = id,
                Version = version,
                AvailableVersion = string.IsNullOrWhiteSpace(available) ? null : available,
                Source = source
            });
        }

        return packages;
    }

    private static string ExtractColumn(string line, List<(string Name, int Start)> colStarts, string colName)
    {
        for (int i = 0; i < colStarts.Count; i++)
        {
            if (colStarts[i].Name != colName)
                continue;

            int start = colStarts[i].Start;
            if (start >= line.Length)
                return string.Empty;

            int end = (i + 1 < colStarts.Count) ? colStarts[i + 1].Start : line.Length;
            end = Math.Min(end, line.Length);

            return line.Substring(start, end - start).Trim();
        }

        return string.Empty;
    }

    [GeneratedRegex(@"^[A-Za-z0-9._+\- ]+$")]
    private static partial Regex PackageIdPattern();
}
