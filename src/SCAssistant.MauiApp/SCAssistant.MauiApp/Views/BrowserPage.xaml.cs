using SCAssistant.Maui.Services;
using SCAssistant.Maui.ViewModels;

namespace SCAssistant.Maui.Views;

public partial class BrowserPage : ContentPage
{
    private readonly IBrowserProvider _browser;
    private readonly MainViewModel _mainViewModel;

    public BrowserPage(MainViewModel mainViewModel, IBrowserProvider browser)
    {
        InitializeComponent();
        _mainViewModel = mainViewModel;
        _browser = browser;

        BindingContext = mainViewModel;
        SetupWebView();
        SetupQuickLinks();
        SetupReloadButton();
    }

    private void SetupWebView()
    {
        if (_browser is BrowserProvider bp)
        {
            bp.SetWebView(BrowserWebView);
        }

        // 监听 WebView 导航完成事件来显示/隐藏主页覆盖层
        BrowserWebView.Navigated += (s, e) =>
        {
            _mainViewModel.IsHomePage = false;
        };
    }

    private void SetupQuickLinks()
    {
        foreach (var (name, url) in MainViewModel.QuickLinks)
        {
            var btn = new Button
            {
                Text = name,
                FontSize = 14,
                Padding = new Thickness(16, 8),
                Margin = new Thickness(4),
                CornerRadius = 8,
                BackgroundColor = Colors.Transparent,
                BorderColor = Application.Current?.RequestedTheme == AppTheme.Dark
                    ? Color.FromArgb("#555555")
                    : Color.FromArgb("#CCCCCC"),
                BorderWidth = 1
            };
            btn.Clicked += (_, _) => _mainViewModel.Home.NavigateQuickLinkCommand.Execute(url);
            QuickLinksContainer.Children.Add(btn);
        }
    }

    private void SetupReloadButton()
    {
        BtnReload.Clicked += (_, _) => _browser.Reload();
    }
}
