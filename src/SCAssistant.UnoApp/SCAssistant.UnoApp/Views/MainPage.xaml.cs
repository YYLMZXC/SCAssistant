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

        // 移动端 Safe Area 代码兜底：若 XAML VisibleBoundsPadding 未生效，
        // 则手动获取状态栏高度添加顶部填充
        ApplyMobileSafeAreaPadding();

        var browserControl = ServiceLocator.BrowserProvider.CreateBrowserControl();
        if (browserControl is UIElement uiElement)
        {
            // 直接添加到 Grid，消除 ContentControl 中间层导致的布局传递丢失
            BrowserHost.Children.Clear();
            BrowserHost.Children.Add(uiElement);
            Grid.SetRow(uiElement, 1);
            LogHelper.Info("[主页面] 浏览器控件已直接挂载到 BrowserHost Grid");
        }
        else
        {
            LogHelper.Error("[主页面] CreateBrowserControl 返回类型异常，无法挂载");
        }

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

        // Android/iOS: 等待原生 WebView 布局完成后再导航
        // 否则原生 WebView frame 可能仍为 0x0，导致白屏
        // 使用轮询检查尺寸（而非固定延迟），更可靠
        if (OperatingSystem.IsAndroid() || OperatingSystem.IsIOS())
        {
            var platform = OperatingSystem.IsAndroid() ? "Android" : "iOS";
            await WaitForWebViewLayoutAsync(platform);
        }

        if (DataContext is MainViewModel vm)
        {
            LogHelper.Info("[主页面] 调用 NavigateToHome");
            vm.NavigateToHome();
        }
    }

    /// <summary>
    /// 移动端 Safe Area 代码兜底：
    /// 若 XAML 的 VisibleBoundsPadding 未生效，手动获取状态栏高度加顶部 padding。
    /// 使用条件编译避免平台 API 在非目标平台不可用。
    /// </summary>
    private void ApplyMobileSafeAreaPadding()
    {
        double topPadding = 0;

#if ANDROID
        try
        {
            var density = Android.Content.Res.Resources.System?.DisplayMetrics?.Density ?? 1f;

            // 优先用 WindowInsets（含刘海高度），由 MainActivity.SafeAreaTopPixels 提供
            var safeAreaPx = SCAssistant.UnoApp.Droid.MainActivity.SafeAreaTopPixels;
            if (safeAreaPx > 0)
            {
                topPadding = safeAreaPx / density;
                LogHelper.Info($"[主页面] Android SafeArea (WindowInsets): {safeAreaPx}px / {density}density = {topPadding}dp");
            }

            // 降级：WindowInsets 还没回调时，先用 status_bar_height
            if (topPadding <= 0)
            {
                int resourceId = Android.Content.Res.Resources.System?.GetIdentifier(
                    "status_bar_height", "dimen", "android") ?? 0;
                if (resourceId > 0)
                {
                    var px = Android.Content.Res.Resources.System?.GetDimensionPixelSize(resourceId) ?? 0;
                    topPadding = px / density;
                }
            }
        }
        catch { /* 降级 */ }
        if (topPadding <= 0) topPadding = 44; // 刘海屏默认值
#elif IOS
        try
        {
            if (UIKit.UIApplication.SharedApplication?.Windows?.FirstOrDefault() is { } window)
            {
                topPadding = (double)window.SafeAreaInsets.Top;
            }
        }
        catch { /* 降级 */ }
        if (topPadding <= 0) topPadding = 44;
#endif

        if (topPadding > 0)
        {
            var currentPad = RootGrid.Padding;
            if (currentPad.Top < topPadding)
            {
                RootGrid.Padding = new Microsoft.UI.Xaml.Thickness(
                    currentPad.Left, topPadding, currentPad.Right, currentPad.Bottom);
                LogHelper.Info($"[主页面] Safe Area 顶部填充已设置: {topPadding}px");
            }
        }
    }

    /// <summary>
    /// 轮询等待 WebView2 获得有效尺寸后再导航。
    /// 比固定延迟更可靠，最多等待 2 秒。
    /// </summary>
    private async Task WaitForWebViewLayoutAsync(string platform)
    {
        LogHelper.Info($"[主页面] {platform}: 等待原生 WebView 布局完成...");
        var maxWait = 2000; // 最多等 2 秒
        var pollInterval = 100; // 每 100ms 检查一次
        var elapsed = 0;

        while (elapsed < maxWait)
        {
            await Task.Delay(pollInterval);
            elapsed += pollInterval;

            if (BrowserHost.ActualWidth > 0 && BrowserHost.ActualHeight > 0)
            {
                LogHelper.Info($"[主页面] {platform}: WebView 布局完成 (elapsed={elapsed}ms, {BrowserHost.ActualWidth}x{BrowserHost.ActualHeight})");
                return;
            }
        }

        LogHelper.Warn($"[主页面] {platform}: 等待超时 ({maxWait}ms), ActualWidth={BrowserHost.ActualWidth}, ActualHeight={BrowserHost.ActualHeight}");
    }
}
