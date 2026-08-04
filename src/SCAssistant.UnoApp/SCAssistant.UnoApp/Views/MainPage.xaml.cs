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
        LogHelper.Info("[主页面] 已构造，DataContext 已设置");

        Loaded += OnControlLoaded;

        DownloadListPanelControl.CloseRequested += (_, _) =>
        {
            LogHelper.Info("[主页面] 下载列表面板关闭请求");
            if (DataContext is MainViewModel vm)
                vm.IsDownloadListVisible = false;
        };
    }

    private void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnControlLoaded;
        LogHelper.Info("[主页面] Loaded - 正在创建浏览器控件");

        var browserControl = ServiceLocator.BrowserProvider.CreateBrowserControl();
        BrowserHost.Content = browserControl;
        LogHelper.Info("[主页面] 浏览器控件已挂载到 BrowserHost");

        if (DataContext is MainViewModel vm)
        {
            LogHelper.Info("[主页面] 调用 NavigateToHome");
            vm.NavigateToHome();
        }
    }
}
