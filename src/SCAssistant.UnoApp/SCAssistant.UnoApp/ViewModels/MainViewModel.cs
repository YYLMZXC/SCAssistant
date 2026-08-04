using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.UnoApp.Services;

namespace SCAssistant.UnoApp.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private readonly IBrowserProvider _browser;

    [ObservableProperty]
    public partial string CurrentUrl { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string CurrentTitle { get; set; } = "SCAssistant";

    [ObservableProperty]
    public partial bool IsBrowserLoading { get; set; }

    [ObservableProperty]
    public partial bool IsDownloadListVisible { get; set; }

    [ObservableProperty]
    public partial bool IsInitialized { get; set; }

    public DownloadListViewModel DownloadList { get; }

    private const string HomeUrl = "https://test.suancaixianyu.cn/";
    private const string SCKeyUrl = "https://www.sckey.net";
    private const string SCWZUrl = "https://scwz.top/";

    public MainViewModel(IBrowserProvider browser, IDownloadHistoryService historyService)
    {
        LogHelper.Info("[主视图] 构造函数 - 初始化 MainViewModel");
        _browser = browser;
        DownloadList = new DownloadListViewModel(historyService);

        _browser.AddressChanged += (_, url) =>
        {
            LogHelper.Info($"[主视图] 地址变更 -> {url}");
            CurrentUrl = url;
        };
        _browser.TitleChanged += (_, title) =>
        {
            LogHelper.Info($"[主视图] 标题变更 -> {title}");
            CurrentTitle = title;
        };
        _browser.LoadingStateChanged += (_, loading) =>
        {
            IsBrowserLoading = loading;
        };
        LogHelper.Info("[主视图] 构造函数 - 初始化完成");
    }

    public void NavigateToHome()
    {
        LogHelper.Info($"[主视图] NavigateToHome -> {HomeUrl}");
        _browser.Initialize(HomeUrl);
    }

    public void InitializeBrowser(object window)
    {
        if (IsInitialized) return;
        LogHelper.Info("[主视图] InitializeBrowser - 首次初始化浏览器");
        IsInitialized = true;
        NavigateToHome();
    }

    [RelayCommand]
    private void NavigateHome()
    {
        LogHelper.Info($"[主视图] NavigateHome -> {HomeUrl}");
        _browser.Navigate(HomeUrl);
    }

    [RelayCommand]
    private void NavigateSCKey()
    {
        LogHelper.Info($"[主视图] NavigateSCKey -> {SCKeyUrl}");
        _browser.Navigate(SCKeyUrl);
    }

    [RelayCommand]
    private void NavigateSCWZ()
    {
        LogHelper.Info($"[主视图] NavigateSCWZ -> {SCWZUrl}");
        _browser.Navigate(SCWZUrl);
    }

    [RelayCommand]
    private void OpenDownloadList()
    {
        IsDownloadListVisible = !IsDownloadListVisible;
        LogHelper.Info($"[主视图] 下载列表切换 -> {(IsDownloadListVisible ? "打开" : "关闭")}");
    }

    /// <summary>
    /// 导航到用户自定义URL，自动补全协议前缀
    /// </summary>
    public void NavigateToCustomUrl(string? rawUrl)
    {
        if (string.IsNullOrWhiteSpace(rawUrl)) return;

        var url = rawUrl.Trim();

        // 自动补全 https:// 协议前缀
        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        LogHelper.Info($"[主视图] NavigateToCustomUrl -> {url}");
        _browser.Navigate(url);
    }
}
