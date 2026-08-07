using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainWindow : Window
{
    private TextBox? _addressBar;

    public MainWindow()
    {
        InitializeComponent();
    }

    public MainWindow(MainViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _addressBar = this.FindControl<TextBox>("AddressBar");

        // Initialize WebView host with a basic content
        // In production, integrate Avalonia.WebView or platform-specific WebView
        InitializeWebViewHost();

        if (DataContext is MainViewModel vm)
        {
            _ = vm.InitializeAsync();
        }
    }

    private void InitializeWebViewHost()
    {
        if (WebViewHost == null) return;

        // Create a placeholder for WebView - in production, replace with actual WebView integration
        var webViewPlaceholder = new Border
        {
            Background = Avalonia.Media.Brush.Parse("#1E1E1E"),
            Child = new TextBlock
            {
                Text = "WebView will be loaded here.\nUse platform-specific WebView integration.",
                Foreground = Avalonia.Media.Brush.Parse("#666666"),
                FontSize = 16,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                TextAlignment = Avalonia.Media.TextAlignment.Center
            }
        };

        WebViewHost.Content = webViewPlaceholder;
    }

    private void BackgroundOverlay_Tap(object? sender, PointerPressedEventArgs e)
    {
        if (DataContext is MainViewModel vm)
        {
            vm.IsDownloadPanelOpen = false;
        }
    }
}
