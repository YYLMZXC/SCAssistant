using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        InitializeComponent();
        Loaded += OnControlLoaded;
    }

    private async void OnControlLoaded(object? sender, RoutedEventArgs e)
    {
        Loaded -= OnControlLoaded;

        var browserControl = ServiceLocator.BrowserProvider.CreateBrowserControl();
        BrowserHost.Content = browserControl;

        // Allow CEF subprocess to start up, then navigate to home
        await Task.Delay(500);

        if (DataContext is ViewModels.MainViewModel vm)
        {
            vm.NavigateToHome();
        }
    }
}
