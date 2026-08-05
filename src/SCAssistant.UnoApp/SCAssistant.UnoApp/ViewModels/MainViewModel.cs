using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.UnoApp.Services;

namespace SCAssistant.UnoApp.ViewModels;

public class MainViewModel : INotifyPropertyChanged
{
    private readonly IBrowserProvider _browser;
    private readonly ISettingsService _settingsService;

    // ===================== 浏览器相关 =====================

    private string _currentUrl = "https://test.suancaixianyu.cn/";
    private string _statusText = "就绪";
    private bool _isLoading;
    private string _windowTitle = "生存战争助手";

    public event PropertyChangedEventHandler? PropertyChanged;

    public string CurrentUrl
    {
        get => _currentUrl;
        set { _currentUrl = value; OnPropertyChanged(); }
    }

    public string StatusText
    {
        get => _statusText;
        set { _statusText = value; OnPropertyChanged(); }
    }

    public bool IsLoading
    {
        get => _isLoading;
        set { _isLoading = value; OnPropertyChanged(); }
    }

    public string WindowTitle
    {
        get => _windowTitle;
        set { _windowTitle = value; OnPropertyChanged(); }
    }

    // ===================== 历史记录 =====================

    public ObservableCollection<string> History { get; } = new();

    // ===================== 设置面板 =====================

    private SettingsViewModel? _settings;
    public SettingsViewModel? Settings
    {
        get => _settings;
        set { _settings = value; OnPropertyChanged(); }
    }

    /// <summary>设置面板是否可见（绑定 MainPage 中设置面板的 Visibility）。</summary>
    private bool _isSettingsVisible;
    public bool IsSettingsVisible
    {
        get => _isSettingsVisible;
        set { _isSettingsVisible = value; OnPropertyChanged(); }
    }

    /// <summary>旧版兼容属性：下载列表是否可见。</summary>
    private bool _isDownloadListVisible;
    public bool IsDownloadListVisible
    {
        get => _isDownloadListVisible;
        set { _isDownloadListVisible = value; OnPropertyChanged(); }
    }

    // ===================== 下载列表（保留兼容） =====================

    public DownloadListViewModel DownloadList { get; }

    // ===================== 命令 =====================

    public IRelayCommand NavigateHomeCommand { get; }
    public IRelayCommand NavigateSCKeyCommand { get; }
    public IRelayCommand NavigateSCWZCommand { get; }
    public IRelayCommand NavigateBackCommand { get; }
    public IRelayCommand ReloadCommand { get; }
    public IRelayCommand OpenSettingsCommand { get; }
    public IRelayCommand CloseSettingsCommand { get; }

    // ===================== 构造函数 =====================

    public MainViewModel(IBrowserProvider browser, ISettingsService settingsService)
    {
        _browser = browser;
        _settingsService = settingsService;

        // 创建 DownloadListViewModel 并传入下载管线（包含 Cookie 获取）
        DownloadList = new DownloadListViewModel(
            ServiceLocator.ServiceLocatorObj.GetRequiredService<IDownloadHistoryService>(),
            ServiceLocator.ServiceLocatorObj.GetRequiredService<IDownloadService>(),
            browser);

        // 创建 SettingsViewModel
        Settings = new SettingsViewModel(_settingsService, _browser, DownloadList);

        // 命令绑定
        NavigateHomeCommand = new RelayCommand(() => NavigateTo("https://www.scbbs.top/"));
        NavigateSCKeyCommand = new RelayCommand(() => NavigateTo("https://www.sckey.net/"));
        NavigateSCWZCommand = new RelayCommand(() => NavigateTo("https://www.scwz.top/"));
        NavigateBackCommand = new RelayCommand(() => _browser.Reload()); // 简化：回到主页
        ReloadCommand = new RelayCommand(() => _browser.Reload());
        OpenSettingsCommand = new RelayCommand(() =>
        {
            LogHelper.Info("[主页] 打开设置面板");
            IsSettingsVisible = true;
        });
        CloseSettingsCommand = new RelayCommand(() =>
        {
            LogHelper.Info("[主页] 关闭设置面板");
            IsSettingsVisible = false;
        });

        // 订阅浏览器事件
        _browser.AddressChanged += (_, url) =>
        {
            CurrentUrl = url;
            if (!string.IsNullOrWhiteSpace(url))
            {
                History.Insert(0, url);
                if (History.Count > 100) History.RemoveAt(100);
            }
        };
        _browser.TitleChanged += (_, title) =>
        {
            WindowTitle = string.IsNullOrWhiteSpace(title) ? "SCAssistant" : $"SCAssistant - {title}";
        };
        _browser.LoadingStateChanged += (_, loading) =>
        {
            IsLoading = loading;
            StatusText = loading ? "加载中..." : "就绪";
        };

        // 下载请求事件：自动弹出设置面板并触发下载
        _browser.DownloadRequested += OnDownloadRequested;

        // 加载持久化设置
        _settingsService.Load();

        LogHelper.Info("[主页] MainViewModel 初始化完成");
    }

    // ===================== 导航方法 =====================

    public void NavigateTo(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        LogHelper.Info($"[主页] NavigateTo -> {url}");

        if (!url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) &&
            !url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            url = "https://" + url;
        }

        CurrentUrl = url;
        _browser.Navigate(url);
    }

    public void NavigateToCustomUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return;
        LogHelper.Info($"[主页] NavigateToCustomUrl -> {url}");
        NavigateTo(url);
    }

    public void NavigateToHome()
    {
        LogHelper.Info("[主页] NavigateToHome");
        _browser.Initialize("https://test.suancaixianyu.cn/");
        CurrentUrl = "https://test.suancaixianyu.cn/";
    }

    // ===================== 下载处理 =====================

    /// <summary>
    /// 当 BrowserProvider 检测到可下载文件时触发。
    /// 自动弹出设置面板并切换到下载标签，启动下载。
    /// </summary>
    private void OnDownloadRequested(object? sender, string url)
    {
        LogHelper.Info($"[主页] OnDownloadRequested -> {url}");

        var fileName = GetFileNameFromUrl(url);

        // 自动显示设置面板（下载标签页）
        Settings?.ShowDownloads();
        IsSettingsVisible = true;

        // 启动下载
        DownloadList.StartDownload(url, fileName);
    }

    private static string GetFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var name = System.IO.Path.GetFileName(path);
            if (!string.IsNullOrWhiteSpace(name)) return name;
        }
        catch { }
        return "download";
    }

    // ===================== INPC =====================

    protected void OnPropertyChanged([CallerMemberName] string? name = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
