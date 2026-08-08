namespace SCAssistant.Maui.Services;

/// <summary>
/// 通过系统浏览器打开外部链接。
/// </summary>
public class SystemBrowserProvider
{
    public async Task OpenAsync(string url)
    {
        await Browser.Default.OpenAsync(url, BrowserLaunchMode.SystemPreferred);
    }
}
