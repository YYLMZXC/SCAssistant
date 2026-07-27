using Avalonia.Controls;
using Avalonia.Interactivity;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += OnControlLoaded;
        DownloadListPanel.CloseRequested += (_, _) =>
        {
            if (DataContext is ViewModels.MainViewModel vm)
                vm.IsDownloadListVisible = false;
        };
    }

    private void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnControlLoaded;

        var browserControl = ServiceLocator.BrowserProvider.CreateBrowserControl();
        BrowserHost.Content = browserControl;

        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.NavigateToHome();
        }
    }
}
