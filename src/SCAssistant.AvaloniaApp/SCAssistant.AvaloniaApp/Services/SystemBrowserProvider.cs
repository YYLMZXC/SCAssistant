using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 系统浏览器打开器 — 使用系统默认浏览器打开 URL。
/// </summary>
public static class SystemBrowserProvider
{
    /// <summary>
    /// 使用系统默认浏览器打开 URL。
    /// </summary>
    public static void OpenUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        try
        {
            // 转义 URL 中的特殊字符
            url = System.Net.WebUtility.UrlEncode(url);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Process.Start("xdg-open", url);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Process.Start("open", url);
            }
            else
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }

            LogHelper.Info($"[SystemBrowser] 已打开: {url}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[SystemBrowser] 打开失败: {url}", ex);
        }
    }
}
