using System.Windows;
using WinForge.Services;
using WinForge.ViewModels;

namespace WinForge;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        var systemTheme = ThemeService.GetSystemTheme();
        var themeService = new ThemeService();
        var wingetService = new WingetService();
        var viewModel = new MainViewModel(wingetService, themeService);

        themeService.ApplyTheme(systemTheme);
        viewModel.IsDarkTheme = systemTheme == AppTheme.Dark;

        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();
    }
}
