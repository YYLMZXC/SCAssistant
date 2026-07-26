using SCAssistant.Maui.Services;

namespace SCAssistant.Maui;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        LoadQuickLinks();
        
        DownloadManager.Instance.DownloadProgressChanged += OnDownloadProgressChanged;
        DownloadManager.Instance.DownloadCompleted += OnDownloadCompleted;
        DownloadManager.Instance.DownloadFailed += OnDownloadFailed;
    }

    private void LoadQuickLinks()
    {
        QuickLinksBar.Children.Clear();
        
        foreach (var btnConfig in NavigationConfig.NavButtons)
        {
            var button = new Button
            {
                Text = btnConfig.Text,
                BackgroundColor = Colors.White,
                TextColor = Color.FromArgb("#512BD4"),
                CornerRadius = 8,
                HeightRequest = 32,
                Padding = new Thickness(12, 4),
                FontSize = 12
            };
            button.Clicked += (s, e) => NavigateTo(btnConfig.Url);
            QuickLinksBar.Children.Add(button);
        }
    }

    private void NavigateTo(string url)
    {
        BrowserWebView.Source = url;
        UrlEntry.Text = url;
    }

    private void OnGoBackClicked(object? sender, EventArgs e)
    {
        if (BrowserWebView.CanGoBack)
            BrowserWebView.GoBack();
    }

    private void OnGoForwardClicked(object? sender, EventArgs e)
    {
        if (BrowserWebView.CanGoForward)
            BrowserWebView.GoForward();
    }

    private void OnReloadClicked(object? sender, EventArgs e)
    {
        BrowserWebView.Reload();
    }

    private void OnNavigateClicked(object? sender, EventArgs e)
    {
        if (!string.IsNullOrWhiteSpace(UrlEntry.Text))
        {
            var url = UrlEntry.Text.Trim();
            if (!url.StartsWith("http"))
                url = "https://" + url;
            NavigateTo(url);
        }
    }

    private void OnUrlSubmitted(object? sender, EventArgs e)
    {
        OnNavigateClicked(sender, e);
    }

    private void OnWebNavigating(object? sender, WebNavigatingEventArgs e)
    {
        if (e.Url != null)
        {
            UrlEntry.Text = e.Url;
            
            if (IsDownloadUrl(e.Url))
            {
                e.Cancel = true;
                _ = DownloadManager.Instance.StartDownloadAsync(e.Url);
            }
        }
    }

    private void OnWebNavigated(object? sender, WebNavigatedEventArgs e)
    {
        if (e.Url != null)
        {
            UrlEntry.Text = e.Url;
        }
    }

    private static readonly string[] DownloadExtensions = new[]
    {
        ".apk", ".zip", ".rar", ".7z", ".exe", ".msi", ".dmg",
        ".iso", ".img", ".tar", ".gz", ".bz2",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".ppt", ".pptx",
        ".mp3", ".mp4", ".avi", ".mkv", ".mov", ".wmv",
        ".jpg", ".jpeg", ".png", ".gif", ".bmp", ".tiff", ".webp",
        ".wav", ".flac", ".ogg", ".aac", ".wma",
        ".jar", ".war", ".dll", ".so", ".dylib",
        ".txt", ".csv", ".xml", ".json", ".html", ".css", ".js",
        ".cs", ".java", ".py", ".cpp", ".h", ".go", ".rs"
    };

    private bool IsDownloadUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var extension = Path.GetExtension(uri.AbsolutePath).ToLowerInvariant();
            return DownloadExtensions.Contains(extension);
        }
        catch
        {
            return false;
        }
    }

    private void OnDownloadProgressChanged(object? sender, DownloadProgressEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Title = $"下载中: {e.FileName} ({e.Progress:F0}%)";
        });
    }

    private void OnDownloadCompleted(object? sender, DownloadCompletedEventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            Title = "生存战争助手";
            await DisplayAlertAsync("下载完成", $"文件 {e.Record.FileName} 下载成功！", "确定");
        });
    }

    private void OnDownloadFailed(object? sender, string e)
    {
        MainThread.BeginInvokeOnMainThread(async () =>
        {
            Title = "生存战争助手";
            await DisplayAlertAsync("下载失败", $"文件 {e} 下载失败，请重试。", "确定");
        });
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _ = DownloadHistoryService.Instance.LoadHistoryAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        DownloadManager.Instance.DownloadProgressChanged -= OnDownloadProgressChanged;
        DownloadManager.Instance.DownloadCompleted -= OnDownloadCompleted;
        DownloadManager.Instance.DownloadFailed -= OnDownloadFailed;
    }
}
