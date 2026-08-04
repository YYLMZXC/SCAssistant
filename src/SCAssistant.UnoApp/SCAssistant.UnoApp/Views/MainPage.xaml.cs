using System;
using System.Threading.Tasks;
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

    private async void OnControlLoaded(object sender, RoutedEventArgs e)
    {
        Loaded -= OnControlLoaded;
        LogHelper.Info("[主页面] Loaded - 正在创建浏览器控件");

        var browserControl = ServiceLocator.BrowserProvider.CreateBrowserControl();
        BrowserHost.Content = browserControl;
        LogHelper.Info("[主页面] 浏览器控件已挂载到 BrowserHost");

        // 移动端原生 WebView 修复：强制布局刷新确保 WebView2 获得正确的尺寸
        // Uno Skia 渲染器需要一次完整的布局传递才能将正确的 bounds 传递给原生 WebView/WKWebView
        try
        {
            BrowserHost.InvalidateArrange();
            BrowserHost.InvalidateMeasure();
            BrowserHost.UpdateLayout();
            LogHelper.Info($"[主页面] 强制布局刷新完成: BrowserHost ActualWidth={BrowserHost.ActualWidth}, ActualHeight={BrowserHost.ActualHeight}");
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[主页面] 强制布局刷新异常: {ex.Message}");
        }

        // Android/iOS: 短暂延迟确保原生 WebView 完成布局后再导航
        // 否则原生 WebView frame 可能仍为 0x0，导致白屏
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            var platform = OperatingSystem.IsAndroid() ? "Android" : "iOS";
            LogHelper.Info($"[主页面] {platform} 平台检测到，延迟 500ms 等待原生 WebView 布局完成");
            await Task.Delay(500);
            LogHelper.Info($"[主页面] 延迟后 BrowserHost: ActualWidth={BrowserHost.ActualWidth}, ActualHeight={BrowserHost.ActualHeight}");
        }

        if (DataContext is MainViewModel vm)
        {
            LogHelper.Info("[主页面] 调用 NavigateToHome");
            vm.NavigateToHome();
        }
    }
}
