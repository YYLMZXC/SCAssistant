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

    [ObservableProperty]
    private string _searchText = string.Empty;

    public HomeViewModel(IBrowserProvider browser)
    {
        _browser = browser;
    }

    [RelayCommand]
    private void Search(string? query)
    {
        var q = query?.Trim();
        if (string.IsNullOrWhiteSpace(q))
        {
            LogHelper.Warn("[HomeVM] 搜索: 查询为空");
            return;
        }

        // 如果是网址，直接导航
        if (q.Contains('.') && !q.Contains(' '))
        {
            var url = q.StartsWith("http") ? q : $"https://{q}";
            LogHelper.Info($"[HomeVM] 直接导航: {url}");
            _browser.Navigate(url);
        }
        else
        {
            // 否则使用搜索引擎
            var searchUrl = $"https://www.google.com/search?q={Uri.EscapeDataString(q)}";
            LogHelper.Info($"[HomeVM] 搜索: {q}");
            _browser.Navigate(searchUrl);
        }
    }

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