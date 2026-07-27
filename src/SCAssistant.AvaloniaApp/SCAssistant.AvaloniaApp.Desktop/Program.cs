using System;
using System.IO;
using Avalonia;
using Exclr8Cef;
using Exclr8Cef.WebView;
using SCAssistant.AvaloniaApp.Desktop.Services;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Desktop;

sealed class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        // 处理 CEF 子进程启动（如 GPU/渲染进程）
        int subproc = Cef.ExecuteProcess(args);
        if (subproc >= 0) return subproc;

        // 注册 Exclr8Cef 浏览器
        ServiceLocator.BrowserProvider = new Exclr8CefBrowserProvider();
        ServiceLocator.DownloadHistory = new DownloadHistoryService();
        ServiceLocator.DownloadHistory.Load();

        var cachePath = Path.Combine(Path.GetTempPath(), "SCAssistant_CefCache");

        var lifetime = new ClassicDesktopStyleApplicationLifetime { Args = args };
        BuildAvaloniaApp().SetupWithLifetime(lifetime);

        var settings = new CefSettings
        {
            CachePath = cachePath,
        };

        AvaloniaSetup.InitializeForOsr(args, settings);

        try
        {
            return lifetime.Start(args);
        }
        finally
        {
            Cef.Shutdown();
            try
            {
                if (Directory.Exists(cachePath))
                    Directory.Delete(cachePath, true);
            }
            catch { /* 忽略清理错误 */ }
        }
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .UseExclr8Cef()
            .LogToTrace();
}
