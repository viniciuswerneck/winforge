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
        return (exitCode == 0, output);
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
        return (exitCode == 0, output);
    }

    public async Task<(bool Success, string Output)> UpgradeAllAsync()
    {
        var (exitCode, output) = await RunWingetAsync(
        [
            "upgrade", "--all",
            "--silent", "--force", "--disable-interactivity",
            "--accept-source-agreements", "--accept-package-agreements"
        ]);
        return (exitCode == 0, output);
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
        return (exitCode == 0, output);
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

        if (await Task.WhenAny(readStdout, Task.Delay(timeoutMs)) != readStdout)
        {
            try { process.Kill(); } catch { }
            var partialOut = await readStdout;
            var partialErr = readStderr.IsCompletedSuccessfully ? await readStderr : "";
            return (-1, partialOut + partialErr + "\n[TIMEOUT]");
        }

        var output = await readStdout;
        var error = readStderr.IsCompletedSuccessfully ? await readStderr : "";
        process.WaitForExit();

        return (process.ExitCode, output + error);
    }

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
            int pos = header.IndexOf(col, StringComparison.OrdinalIgnoreCase);
            if (pos >= 0)
                colStarts.Add((col, pos));
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
