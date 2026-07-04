namespace WinForge.Services;

public interface IThemeService
{
    AppTheme CurrentTheme { get; }
    event Action<AppTheme>? ThemeChanged;
    void ToggleTheme();
    void ApplyTheme(AppTheme theme);
}
