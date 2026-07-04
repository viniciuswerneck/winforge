using System.Text.Json;

namespace WinForge.Services;

public class UserConfigurationService : IUserConfigurationService
{
    private static readonly string ConfigPath = System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "WinForge", "config.json");

    public bool IsOnboardingCompleted()
    {
        try
        {
            if (!System.IO.File.Exists(ConfigPath)) return false;
            var json = System.IO.File.ReadAllText(ConfigPath);
            var config = JsonSerializer.Deserialize<Dictionary<string, bool>>(json);
            return config is not null && config.TryGetValue("onboarding", out var val) && val;
        }
        catch
        {
            return false;
        }
    }

    public void SetOnboardingCompleted()
    {
        try
        {
            var dir = System.IO.Path.GetDirectoryName(ConfigPath);
            if (dir is not null) System.IO.Directory.CreateDirectory(dir);
            var json = JsonSerializer.Serialize(new Dictionary<string, bool> { ["onboarding"] = true });
            System.IO.File.WriteAllText(ConfigPath, json);
        }
        catch { }
    }
}
