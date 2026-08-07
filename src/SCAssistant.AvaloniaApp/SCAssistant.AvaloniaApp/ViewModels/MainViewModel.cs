using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Models;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 主视图模型 - 管理导航和全局状态
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly ISettingsService _settingsService;
    private readonly IDownloadHistoryService _downloadHistoryService;
    private readonly IDownloadService _downloadService;

    [ObservableProperty]
    private ViewModelBase? _currentView;

    [ObservableProperty]
    private string _currentUrl = "https://www.google.com";

    [ObservableProperty]
    private string _addressBarUrl = "https://www.google.com";

    [ObservableProperty]
    private bool _canGoBack;

    [ObservableProperty]
    private bool _canGoForward;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private string _pageTitle = "SCAssistant";

    [ObservableProperty]
    private bool _isSettingsOpen;

    [ObservableProperty]
    private bool _isDownloadPanelOpen;

    [ObservableProperty]
    private string _downloadUrl = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DownloadRecord> _downloadRecords = new();

    public AppSettings Settings { get; private set; } = new();

    // Navigation stacks
    private readonly Stack<string> _backStack = new();
    private readonly Stack<string> _forwardStack = new();

    public MainViewModel(
        ISettingsService settingsService,
        IDownloadHistoryService downloadHistoryService,
        IDownloadService downloadService)
    {
        _settingsService = settingsService;
        _downloadHistoryService = downloadHistoryService;
        _downloadService = downloadService;
        Title = "SCAssistant";
    }

    public async Task InitializeAsync()
    {
        Settings = await _settingsService.GetSettingsAsync();
        CurrentUrl = Settings.HomePageUrl;
        AddressBarUrl = Settings.HomePageUrl;
        await LoadDownloadHistoryAsync();
    }

    [RelayCommand]
    private void NavigateToUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;

        // Normalize URL
        var normalizedUrl = NormalizeUrl(url);

        if (!string.IsNullOrEmpty(CurrentUrl) && CurrentUrl != normalizedUrl)
        {
            _backStack.Push(CurrentUrl);
            _forwardStack.Clear();
        }

        CurrentUrl = normalizedUrl;
        AddressBarUrl = normalizedUrl;
        CanGoBack = _backStack.Count > 0;
        CanGoForward = _forwardStack.Count > 0;
    }

    [RelayCommand]
    private void GoBack()
    {
        if (_backStack.Count > 0)
        {
            _forwardStack.Push(CurrentUrl);
            CurrentUrl = _backStack.Pop();
            AddressBarUrl = CurrentUrl;
            CanGoBack = _backStack.Count > 0;
            CanGoForward = true;
        }
    }

    [RelayCommand]
    private void GoForward()
    {
        if (_forwardStack.Count > 0)
        {
            _backStack.Push(CurrentUrl);
            CurrentUrl = _forwardStack.Pop();
            AddressBarUrl = CurrentUrl;
            CanGoBack = true;
            CanGoForward = _forwardStack.Count > 0;
        }
    }

    [RelayCommand]
    private void Refresh()
    {
        // 强制刷新：设置URL后由WebView控件处理
        var url = CurrentUrl;
        CurrentUrl = string.Empty;
        CurrentUrl = url;
    }

    [RelayCommand]
    private void GoHome()
    {
        NavigateToUrl(Settings.HomePageUrl);
    }

    [RelayCommand]
    private void ToggleSettings()
    {
        IsSettingsOpen = !IsSettingsOpen;
    }

    [RelayCommand]
    private void ToggleDownloadPanel()
    {
        IsDownloadPanelOpen = !IsDownloadPanelOpen;
    }

    [RelayCommand]
    private async Task StartDownload()
    {
        if (string.IsNullOrWhiteSpace(DownloadUrl)) return;

        try
        {
            var record = new DownloadRecord
            {
                Url = DownloadUrl,
                Status = "Downloading",
                Progress = 0
            };

            DownloadRecords.Add(record);
            await _downloadHistoryService.AddRecordAsync(record);

            var downloadId = await _downloadService.StartDownloadAsync(DownloadUrl);
            DownloadUrl = string.Empty;

            // Monitor progress
            _ = MonitorDownloadAsync(downloadId, record);
        }
        catch (Exception ex)
        {
            LogHelper.Error(ex, "MainViewModel");
        }
    }

    [RelayCommand]
    private async Task SaveSettings()
    {
        await _settingsService.SaveSettingsAsync(Settings);
        IsSettingsOpen = false;
    }

    [RelayCommand]
    private async Task OpenInSystemBrowser(string? url)
    {
        if (!string.IsNullOrEmpty(url))
        {
            await SystemBrowserProvider.OpenUrlAsync(url);
        }
    }

    public void UpdateNavigationState(string url)
    {
        CurrentUrl = url;
        AddressBarUrl = url;
    }

    public void UpdateLoadingState(bool isLoading)
    {
        IsLoading = isLoading;
    }

    public void UpdatePageTitle(string title)
    {
        if (!string.IsNullOrEmpty(title))
        {
            PageTitle = title;
        }
    }

    private string NormalizeUrl(string url)
    {
        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (!url.Contains('.') && !url.Contains('/') && !url.Contains(' '))
        {
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(url);
        }

        if (url.Contains(' ') || (!url.Contains('.') && !url.Contains('/')))
        {
            return "https://www.google.com/search?q=" + Uri.EscapeDataString(url);
        }

        return "https://" + url;
    }

    private async Task LoadDownloadHistoryAsync()
    {
        try
        {
            var records = await _downloadHistoryService.GetRecordsAsync();
            DownloadRecords = new ObservableCollection<DownloadRecord>(records);
        }
        catch { }
    }

    private async Task MonitorDownloadAsync(string downloadId, DownloadRecord record)
    {
        while (true)
        {
            try
            {
                var progress = await _downloadService.GetProgressAsync(downloadId);
                var speed = await _downloadService.GetSpeedAsync(downloadId);

                record.Progress = progress;
                record.Speed = speed;

                if (progress >= 100)
                {
                    record.Status = "Completed";
                    record.CompletedAt = DateTime.Now;
                    await _downloadHistoryService.UpdateRecordAsync(record);
                    break;
                }
                else if (progress == -1)
                {
                    record.Status = "Failed";
                    record.ErrorMessage = "下载失败";
                    await _downloadHistoryService.UpdateRecordAsync(record);
                    break;
                }
                else if (progress == -2)
                {
                    record.Status = "Cancelled";
                    await _downloadHistoryService.UpdateRecordAsync(record);
                    break;
                }

                await Task.Delay(500);
            }
            catch
            {
                break;
            }
        }
    }
}
