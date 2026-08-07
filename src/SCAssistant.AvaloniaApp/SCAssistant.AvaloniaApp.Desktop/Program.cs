using System;
using System.Runtime.InteropServices;
using Avalonia;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.Views;

namespace SCAssistant.AvaloniaApp.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
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
        BrowserView.BrowserControlFactory = provider =>
        {
            var webView = new WebViewBrowserControl();
            if (provider is BrowserProvider browserProvider)
            {
                // 设置平台 WebView，使 BrowserProvider 的方法调用能到达 WebView2
                browserProvider.SetPlatformWebView(webView);

                // 将 WebViewBrowserControl 的事件桥接到 BrowserProvider
                webView.AddressChanged += (_, url) => browserProvider.HandlePlatformAddressChanged(url);
                webView.TitleChanged += (_, title) => browserProvider.HandlePlatformTitleChanged(title);
                webView.LoadingStateChanged += (_, loading) => browserProvider.HandlePlatformLoadingStateChanged(loading);
                webView.DownloadRequested += (_, url) => browserProvider.HandlePlatformDownloadRequested(url);
                webView.NavigationHistoryChanged += (_, _) => browserProvider.HandlePlatformNavigationHistoryChanged();
            }
            return webView;
        };

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();

#if DEBUG
    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
#endif
}