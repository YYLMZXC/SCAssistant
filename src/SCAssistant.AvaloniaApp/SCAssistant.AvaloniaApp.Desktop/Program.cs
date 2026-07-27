using System;
using Avalonia;
using SCAssistant.AvaloniaApp.Desktop.Services;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 注册 Windows WebView2 浏览器
        ServiceLocator.BrowserProvider = new WebView2BrowserProvider();
        ServiceLocator.DownloadHistory = new DownloadHistoryService();
        ServiceLocator.DownloadHistory.Load();

        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
