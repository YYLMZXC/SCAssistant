using CommunityToolkit.Mvvm.ComponentModel;
using SCAssistant.Maui.Services;

namespace SCAssistant.Maui.ViewModels;

/// <summary>
/// MainViewModel — 浏览器主视图的 ViewModel。
/// 组合管理 AddressBar / Home / Download 子 ViewModel。
/// </summary>
public partial class MainViewModel : ViewModelBase
{
    private readonly IBrowserProvider _browser;

    [ObservableProperty]
    private bool _isHomePage = true;

    [ObservableProperty]
    private bool _isLoading;

    public AddressBarViewModel AddressBar { get; }
    public HomeViewModel Home { get; }
    public DownloadListViewModel Downloads { get; }
    public SettingsViewModel Settings { get; }

    /// <summary>
    /// 主页快捷链接列表。
    /// </summary>
    public static List<(string Name, string Url)> QuickLinks { get; } = new()
    {
        ("Survivalcraft 官网", "https://kaalus.wordpress.com/"),
        ("SC 中文社区", "https://www.survivalcraft.cn/"),
        ("SC 中文论坛", "https://www.survivalcraft.cn/forum/"),
        ("GitHub", "https://github.com"),
    };

    public MainViewModel(
        IBrowserProvider browser,
        AddressBarViewModel addressBar,
        HomeViewModel home,
        DownloadListViewModel downloads,
        SettingsViewModel settings)
    {
        _browser = browser;
        AddressBar = addressBar;
        Home = home;
        Downloads = downloads;
        Settings = settings;

        _browser.AddressChanged += (_, _) =>
        {
            IsHomePage = string.IsNullOrWhiteSpace(_browser.GetCurrentUrl());
        };

        _browser.LoadingStateChanged += (_, loading) =>
        {
            IsLoading = loading;
        };
    }
}
