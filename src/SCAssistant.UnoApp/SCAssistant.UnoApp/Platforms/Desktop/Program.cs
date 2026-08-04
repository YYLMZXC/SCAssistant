using System;
using System.Runtime.InteropServices;
using SCAssistant.UnoApp.Services;
using Uno.UI.Hosting;

namespace SCAssistant.UnoApp;

internal class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // 桌面端分配控制台窗口方便调试查看日志
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            AllocConsole();
        }
        LogHelper.Info("[App] Starting");

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            LogHelper.Error($"[App] Unhandled exception: {e.ExceptionObject}");
        };

        var host = UnoPlatformHostBuilder.Create()
            .App(() => new App())
            .UseX11()
            .UseLinuxFrameBuffer()
            .UseMacOS()
            .UseWin32()
            .Build();

        host.Run();
    }

    [DllImport("kernel32.dll")]
    private static extern bool AllocConsole();
}
