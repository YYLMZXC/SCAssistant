using UIKit;
using Avalonia;
using Avalonia.iOS;
using Foundation;

namespace SCAssistant.AvaloniaApp.Platforms.iOS;

/// <summary>
/// iOS 应用程序入口（薄壳文件）。
/// WebView 控件实现与工厂注册已统一在共享层 App.axaml.cs 完成，
/// 此处仅保留 UIApplication 启动与 CurrentViewController 静态引用（供 WKWebView 嵌入使用）。
/// </summary>
public class Application
{
    /// <summary>当前 ViewController（供共享项目 WebViewBrowserControl 通过反射查找）。</summary>
    public static UIViewController? CurrentViewController { get; private set; }

    static void Main(string[] args)
    {
        UIApplication.Main(args, null, typeof(AppDelegate));
    }

    public static UIViewController? GetTopViewController()
    {
        try
        {
            var window = UIApplication.SharedApplication.KeyWindow;
            if (window != null && window.RootViewController != null)
                return FindTopViewController(window.RootViewController);
        } catch { }
        return null;
    }

    private static UIViewController FindTopViewController(UIViewController vc)
    {
        if (vc.PresentedViewController != null) return FindTopViewController(vc.PresentedViewController);
        if (vc is UINavigationController nav && nav.VisibleViewController != null) return FindTopViewController(nav.VisibleViewController);
        if (vc is UITabBarController tab && tab.SelectedViewController != null) return FindTopViewController(tab.SelectedViewController);
        return vc;
    }
}
