using System.IO;
using System.Windows;
using WinForge.Services;
using WinForge.ViewModels;

namespace WinForge;

public partial class App : Application
{
    public ThemeService ThemeService { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, ex) =>
        {
            File.WriteAllText("crash.log", ex.Exception.ToString());
            ex.Handled = true;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            File.WriteAllText("crash.log", ex.ExceptionObject?.ToString() ?? "unknown");
        };
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            File.WriteAllText("crash.log", ex.Exception.ToString());
        };

        base.OnStartup(e);

        var systemTheme = ThemeService.GetSystemTheme();
        ThemeService = new ThemeService();
        var wingetService = new WingetService();
        var userConfigService = new UserConfigurationService();
        var viewModel = new MainViewModel(wingetService, ThemeService, userConfigService);

        ThemeService.ApplyTheme(systemTheme);
        viewModel.IsDarkTheme = systemTheme == AppTheme.Dark;

        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();
    }
}
