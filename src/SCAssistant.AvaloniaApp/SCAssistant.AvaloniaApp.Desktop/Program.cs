using System;
using System.Runtime.InteropServices;
using Avalonia;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.Views;

namespace SCAssistant.AvaloniaApp.Desktop;

/// <summary>
/// Windows 桌面端程序入口。
/// 注册 WebView2 浏览器控件工厂并启动 Avalonia 经典桌面生命周期。
/// </summary>
sealed class Program
{
    /// <summary>桌面应用程序主入口点。</summary>
    [STAThread]
    public static void Main(string[] args)
    {
        // DEBUG 模式下附加控制台窗口用于查看日志输出
#if DEBUG
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AllocConsole();
            Console.Title = "SCAssistant - 调试日志";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            Console.InputEncoding = System.Text.Encoding.UTF8;
        }
#endif

        // 注册 Windows 桌面端 WebView2 工厂
        // 此工厂负责创建 WebView2 控件并桥接所有事件到 BrowserProvider
        BrowserView.BrowserControlFactory = provider =>
        {
            var webView = new WebViewBrowserControl();
            if (provider is BrowserProvider browserProvider)
            {
                // 将平台 WebView 注入 BrowserProvider，使其方法调用能到达 WebView2
                browserProvider.SetPlatformWebView(webView);

                // 等待 WebView2 初始化完成后再标记就绪，触发排队导航执行
                if (!webView.IsReady)
                {
                    webView.ReadyChanged += (_, _) =>
                    {
                        browserProvider.MarkPlatformReady();
                    };
                }

                // 将 WebViewBrowserControl 的所有事件桥接到 BrowserProvider
                webView.AddressChanged += (_, url) => browserProvider.HandlePlatformAddressChanged(url);
                webView.TitleChanged += (_, title) => browserProvider.HandlePlatformTitleChanged(title);
                webView.LoadingStateChanged += (_, loading) => browserProvider.HandlePlatformLoadingStateChanged(loading);
                webView.DownloadRequested += (_, url) => browserProvider.HandlePlatformDownloadRequested(url);
                webView.NavigationHistoryChanged += (_, _) => browserProvider.HandlePlatformNavigationHistoryChanged();
            }
            return webView;
        };

        // 启动 Avalonia 经典桌面生命周期
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    /// <summary>配置 Avalonia 应用构建器。</summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()      // 自动检测当前平台
            .WithInterFont()          // 使用 Inter 字体
            .LogToTrace();            // 日志输出到 Trace

#if DEBUG
    /// <summary>Win32 API：为进程分配控制台窗口（仅 DEBUG 模式）。</summary>
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
#endif
}