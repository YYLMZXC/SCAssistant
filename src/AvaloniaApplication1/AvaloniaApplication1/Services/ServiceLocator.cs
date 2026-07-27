namespace AvaloniaApplication1.Services;

public static class ServiceLocator
{
    public static IBrowserProvider BrowserProvider { get; set; } = new PlaceholderBrowserProvider();
    public static IDownloadHistoryService DownloadHistory { get; set; } = new DownloadHistoryService();
}
