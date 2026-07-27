using System;
using System.Diagnostics;
using System.Runtime.Versioning;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 系统默认浏览器降级方案。
/// 当平台不支持内嵌浏览器（如 Linux/macOS 桌面）时，使用系统默认浏览器打开链接。
/// 导航按钮会将 URL 传递给系统浏览器打开。
/// </summary>
public class SystemBrowserProvider : IBrowserProvider
{
#pragma warning disable CS0067
    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;
#pragma warning restore CS0067

    public string CurrentUrl => string.Empty;
    public string CurrentTitle => "系统浏览器模式";
    public bool IsLoading => false;

    public Control CreateBrowserControl()
    {
        return new TextBlock
        {
            Text = "当前平台使用系统默认浏览器\n点击导航按钮即可在外部浏览器中打开页面",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16,
            TextAlignment = TextAlignment.Center
        };
    }

    public void Initialize(string startUrl) => Navigate(startUrl);

    public void Navigate(string url)
    {
        try
        {
            OpenUrl(url);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError($"SystemBrowserProvider: 无法打开 URL {url} - {ex.Message}");
        }
    }

    public void Reload() { }

    [SupportedOSPlatformGuard("windows")]
    private static bool IsWindows => OperatingSystem.IsWindows();

    [SupportedOSPlatformGuard("macos")]
    private static bool IsMacOS => OperatingSystem.IsMacOS();

    [SupportedOSPlatformGuard("linux")]
    private static bool IsLinux => OperatingSystem.IsLinux();

    private static void OpenUrl(string url)
    {
        if (OperatingSystem.IsWindows())
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
        else if (OperatingSystem.IsMacOS())
        {
            Process.Start("open", url);
        }
        else if (OperatingSystem.IsLinux())
        {
            Process.Start("xdg-open", url);
        }
        else
        {
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }
    }
}
