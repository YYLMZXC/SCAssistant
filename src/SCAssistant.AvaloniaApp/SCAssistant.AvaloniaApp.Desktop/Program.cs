using System;
using System.IO;
using Avalonia;
using SCAssistant.AvaloniaApp.Desktop.Services;
using SCAssistant.AvaloniaApp.Services;
using Xilium.CefGlue;
using Xilium.CefGlue.Common;

namespace SCAssistant.AvaloniaApp.Desktop;

sealed class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 注册 CefGlue.Next 浏览器
        ServiceLocator.BrowserProvider = new CefGlueBrowserProvider();
        ServiceLocator.DownloadHistory = new DownloadHistoryService();
        ServiceLocator.DownloadHistory.Load();

        var cachePath = Path.Combine(Path.GetTempPath(), "SCAssistant_CefCache");

        // 注册 CEF 清理回调
        AppDomain.CurrentDomain.ProcessExit += (_, _) =>
        {
            CefRuntime.Shutdown();
            try
            {
                if (Directory.Exists(cachePath))
                    Directory.Delete(cachePath, true);
            }
            catch { /* 忽略清理错误 */ }
        };

        BuildAvaloniaApp(cachePath)
            .StartWithClassicDesktopLifetime(args);
    }

    public static AppBuilder BuildAvaloniaApp(string cachePath)
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .AfterSetup(_ => CefRuntimeLoader.Initialize(new CefSettings
            {
                RootCachePath = cachePath,
                WindowlessRenderingEnabled = false
            }))
            .LogToTrace();
}
