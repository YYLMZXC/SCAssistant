using System;
using System.Runtime.InteropServices;
using Avalonia;

namespace SCAssistant.AvaloniaApp;

/// <summary>
/// 桌面端程序入口（Windows / Linux / macOS 共用，仅编译进桌面 TFM）。
/// WebView2 控件与工厂注册已统一在共享层 App.axaml.cs 完成，
/// 此处仅保留 Win32 控制台分配（调试用）和启动 Avalonia 桌面生命周期。
/// </summary>
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
