using System;
using Avalonia;
using AvaloniaApplication1.Desktop.Services;
using AvaloniaApplication1.Services;

namespace AvaloniaApplication1.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Register platform-specific services before building the app
        ServiceLocator.BrowserProvider = new CefBrowserProvider();
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
