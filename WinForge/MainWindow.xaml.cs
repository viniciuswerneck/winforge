using System.Diagnostics;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Navigation;
using WinForge.Services;
using WinForge.ViewModels;

namespace WinForge;

public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;

        Loaded += (_, _) =>
        {
            ThemeService.SetWindowTitleBarDark(this, viewModel.IsDarkTheme);
        };

        Loaded += async (_, _) =>
        {
            await viewModel.LoadInstalledCommand.ExecuteAsync(null);
            await viewModel.LoadUpdatesCommand.ExecuteAsync(null);
        };
    }

    private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
    {
        using var _ = Process.Start(new ProcessStartInfo(e.Uri.ToString()) { UseShellExecute = true });
        e.Handled = true;
    }
}
