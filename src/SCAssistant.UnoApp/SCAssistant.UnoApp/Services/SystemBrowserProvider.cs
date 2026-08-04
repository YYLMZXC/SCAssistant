using System.Diagnostics;
using System.Runtime.InteropServices;

namespace SCAssistant.UnoApp.Services;

/// <summary>
/// 系统默认浏览器打开 URL - 作为 WebView2 不可用时的回退方案。
/// </summary>
public static class SystemBrowserProvider
{
    public static void OpenUrl(string url)
    {
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = url,
                UseShellExecute = true
            };
            Process.Start(psi);
        }
        catch
        {
            // 如果打开失败，尝试另一种方式
            try
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    Process.Start(new ProcessStartInfo("cmd", $"/c start {url}")
                    {
                        CreateNoWindow = true,
                        UseShellExecute = false
                    });
                }
                else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    Process.Start("open", url);
                }
                else
                {
                    Process.Start("xdg-open", url);
                }
            }
            catch
            {
                // 失败静默处理
            }
        }
    }
}
