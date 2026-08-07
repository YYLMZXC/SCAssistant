namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 系统浏览器打开链接的实现
/// </summary>
public class SystemBrowserProvider
{
    /// <summary>
    /// 通过系统默认浏览器打开URL
    /// </summary>
    public static async Task OpenUrlAsync(string url)
    {
        try
        {
            if (OperatingSystem.IsWindows())
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsMacOS())
            {
                System.Diagnostics.Process.Start("open", url);
            }
            else if (OperatingSystem.IsLinux())
            {
                System.Diagnostics.Process.Start("xdg-open", url);
            }
            else if (OperatingSystem.IsAndroid())
            {
                // Android: handled through platform-specific intent
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
            else if (OperatingSystem.IsIOS())
            {
                // iOS: handled through platform-specific URL scheme
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true
                });
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "SystemBrowser");
        }

        await Task.CompletedTask;
    }
}
