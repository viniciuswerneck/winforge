using System.Windows;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace WinForge.Services;

public enum AppTheme { Dark, Light }

public class ThemeService : IThemeService
{
    private AppTheme _currentTheme = AppTheme.Dark;
    private ResourceDictionary? _currentThemeDict;

    public AppTheme CurrentTheme => _currentTheme;

    public event Action<AppTheme>? ThemeChanged;

    public void ToggleTheme()
    {
        ApplyTheme(_currentTheme == AppTheme.Dark ? AppTheme.Light : AppTheme.Dark);
    }

    public void ApplyTheme(AppTheme theme)
    {
        _currentTheme = theme;

        var uri = theme == AppTheme.Dark
            ? new Uri("/Styles/ThemeDark.xaml", UriKind.Relative)
            : new Uri("/Styles/ThemeLight.xaml", UriKind.Relative);

        var newDict = new ResourceDictionary { Source = uri };

        var merged = Application.Current.Resources.MergedDictionaries;

        if (_currentThemeDict != null)
            merged.Remove(_currentThemeDict);

        merged.Add(newDict);
        _currentThemeDict = newDict;

        ThemeChanged?.Invoke(theme);
    }

    public static void SetWindowTitleBarDark(Window window, bool dark)
    {
        var hwnd = new WindowInteropHelper(window).Handle;
        if (hwnd == IntPtr.Zero) return;

        var attr = dark ? 1 : 0;
        DwmSetWindowAttribute(hwnd, DWMWA_USE_IMMERSIVE_DARK_MODE, ref attr, Marshal.SizeOf<int>());
    }

    private const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

    public static AppTheme GetSystemTheme()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            if (key?.GetValue("AppsUseLightTheme") is int v)
                return v == 0 ? AppTheme.Dark : AppTheme.Light;
        }
        catch { }
        return AppTheme.Dark;
    }
}
