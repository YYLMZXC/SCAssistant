using System;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.ViewModels;

/// <summary>
/// 主页 ViewModel — 显示欢迎页及常用快捷链接。
/// </summary>
public partial class HomeViewModel : ViewModelBase
{
    private readonly IBrowserProvider _browser;

    /// <summary>搜索框文本。</summary>
    [ObservableProperty]
    private string _searchText = string.Empty;

    public HomeViewModel(IBrowserProvider browser)
    {
        _browser = browser;
    }

    /// <summary>
    /// 执行搜索或 URL 导航。
    /// 自动识别：含 "." 且不含空格视为 URL 直接导航，否则使用 Google 搜索。
    /// </summary>
    [RelayCommand]
    private void Search(string? query)
    {
        var q = query?.Trim();
        if (string.IsNullOrWhiteSpace(q))
        {
            LogHelper.Warn("[HomeVM] 搜索: 查询为空");
            return;
        }

        // 智能判断：含点号且无空格 → 视为 URL 直接导航
        if (q.Contains('.') && !q.Contains(' '))
        {
            var url = q.StartsWith("http") ? q : $"https://{q}";
            LogHelper.Info($"[HomeVM] 直接导航: {url}");
            _browser.Navigate(url);
        }
        else
        {
            // 使用 Google 搜索引擎
            var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(q)}";
            LogHelper.Info($"[HomeVM] 搜索: {q}");
            _browser.Navigate(searchUrl);
        }
    }

    /// <summary>点击快捷链接直接导航。</summary>
    [RelayCommand]
    private void NavigateQuickLink(string? url)
    {
        if (!string.IsNullOrWhiteSpace(url))
        {
            LogHelper.Info($"[HomeVM] 快速导航: {url}");
            _browser.Navigate(url);
        }
    }
}