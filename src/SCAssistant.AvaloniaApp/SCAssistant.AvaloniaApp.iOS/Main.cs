using UIKit;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.Views;

namespace SCAssistant.AvaloniaApp.iOS;

/// <summary>
/// iOS 应用程序入口类。
/// 注册 WKWebView 浏览器控件工厂并启动 UIApplication。
/// </summary>
public class Application
{
    /// <summary>当前 ViewController（供 WebViewBrowserControl 嵌入使用）。</summary>
    public static UIViewController? CurrentViewController { get; private set; }

    /// <summary>应用程序主入口点。</summary>
    static void Main(string[] args)
    {
        // 注册 iOS 平台浏览器控件工厂
        // 此工厂负责创建 WKWebView 控件并桥接所有事件到 BrowserProvider
        BrowserView.BrowserControlFactory = provider =>
        {
            var webView = new WebViewBrowserControl();
            if (provider is BrowserProvider browserProvider)
            {
                browserProvider.SetPlatformWebView(webView);

                // 等待平台 WebView 初始化完成后再标记就绪
                if (!webView.IsReady)
                {
                    webView.ReadyChanged += (_, _) =>
                    {
                        browserProvider.MarkPlatformReady();
                    };
                }

                webView.AddressChanged += (_, url) => browserProvider.HandlePlatformAddressChanged(url);
                webView.TitleChanged += (_, title) => browserProvider.HandlePlatformTitleChanged(title);
                webView.LoadingStateChanged += (_, loading) => browserProvider.HandlePlatformLoadingStateChanged(loading);
                webView.DownloadRequested += (_, url) => browserProvider.HandlePlatformDownloadRequested(url);
                webView.NavigationHistoryChanged += (_, _) => browserProvider.HandlePlatformNavigationHistoryChanged();
            }
            return webView;
        };

        UIApplication.Main(args, null, typeof(AppDelegate));
    }

    /// <summary>
    /// 获取当前顶层 ViewController。
    /// </summary>
    public static UIViewController? GetTopViewController()
    {
        try
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            if (window != null)
            {
                var rootVC = window.RootViewController;
                if (rootVC != null)
                {
                    return FindTopViewController(rootVC);
                }
            }
        }
        catch { }

        return null;
    }

    private static UIViewController FindTopViewController(UIViewController vc)
    {
        if (vc.PresentedViewController != null)
        {
            return FindTopViewController(vc.PresentedViewController);
        }

        if (vc is UINavigationController navVC && navVC.VisibleViewController != null)
        {
            return FindTopViewController(navVC.VisibleViewController);
        }

        if (vc is UITabBarController tabVC && tabVC.SelectedViewController != null)
        {
            return FindTopViewController(tabVC.SelectedViewController);
        }

        return vc;
    }
}