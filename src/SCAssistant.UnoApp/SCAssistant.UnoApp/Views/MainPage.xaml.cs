using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SCAssistant.UnoApp.Services;
using SCAssistant.UnoApp.ViewModels;

namespace SCAssistant.UnoApp.Views;

public partial class MainPage : Page
{
    public MainPage()
    {
        InitializeComponent();

        // Inject DataContext via DI
        DataContext = ServiceLocator.ServiceLocatorObj.GetRequiredService<MainViewModel>();

        Loaded += OnControlLoaded;

        DownloadListPanelControl.CloseRequested += (_, _) =>
        {
            if (DataContext is MainViewModel vm)
                vm.IsDownloadListVisible = false;
        };
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnControlLoaded;

        var browserControl = ServiceLocator.BrowserProvider.CreateBrowserControl();
        BrowserHost.Content = browserControl;

        if (DataContext is MainViewModel vm)
        {
            vm.NavigateToHome();
        }
    }
}
