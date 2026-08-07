using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SCAssistant.AvaloniaApp.Models;

/// <summary>
/// 应用设置数据模型，支持属性变更通知。
/// </summary>
public class AppSettings : INotifyPropertyChanged
{
    /// <summary>主页 URL，默认为 Survivalcraft 测试站。</summary>
    private string _homePageUrl = "https://test.suancaixianyu.cn/";

    /// <summary>默认搜索引擎模板，{query} 将被替换为搜索关键词。</summary>
    private string _defaultSearchEngine = "https://www.google.com/search?q=";

    /// <summary>文件下载保存目录。</summary>
    private string _downloadDirectory = string.Empty;

    /// <summary>最大并行下载数量，默认 3。</summary>
    private int _maxConcurrentDownloads = 3;

    /// <summary>是否启用下载历史记录。</summary>
    private bool _enableDownloadHistory = true;

    /// <summary>是否启用广告拦截。</summary>
    private bool _enableAdBlock;

    /// <summary>主题索引：0=System, 1=Light, 2=Dark。</summary>
    private int _themeIndex;

    /// <summary>主题名称（System/Light/Dark），由 ThemeIndex 自动推导。</summary>
    private string _theme = "System";

    /// <summary>字体缩放比例，默认 1.0。</summary>
    private double _fontScale = 1.0;

    /// <summary>底部 4 个标签页 URL 数组。</summary>
    private string[] _tabUrls = Array.Empty<string>();

    /// <summary>主页 URL。</summary>
    [JsonPropertyName("homePageUrl")]
    public string HomePageUrl
    {
        get => _homePageUrl;
        set { _homePageUrl = value; OnPropertyChanged(); }
    }

    /// <summary>默认搜索引擎 URL 模板。</summary>
    [JsonPropertyName("defaultSearchEngine")]
    public string DefaultSearchEngine
    {
        get => _defaultSearchEngine;
        set { _defaultSearchEngine = value; OnPropertyChanged(); }
    }

    /// <summary>文件下载保存目录路径。</summary>
    [JsonPropertyName("downloadDirectory")]
    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set { _downloadDirectory = value; OnPropertyChanged(); }
    }

    /// <summary>最大并行下载数量（1-10）。</summary>
    [JsonPropertyName("maxConcurrentDownloads")]
    public int MaxConcurrentDownloads
    {
        get => _maxConcurrentDownloads;
        set { _maxConcurrentDownloads = value; OnPropertyChanged(); }
    }

    /// <summary>是否启用下载历史记录。</summary>
    [JsonPropertyName("enableDownloadHistory")]
    public bool EnableDownloadHistory
    {
        get => _enableDownloadHistory;
        set { _enableDownloadHistory = value; OnPropertyChanged(); }
    }

    /// <summary>是否启用广告拦截功能。</summary>
    [JsonPropertyName("enableAdBlock")]
    public bool EnableAdBlock
    {
        get => _enableAdBlock;
        set { _enableAdBlock = value; OnPropertyChanged(); }
    }

    /// <summary>
    /// 主题索引：0=System（跟随系统），1=Light（浅色），2=Dark（深色）。
    /// 设置时自动同步更新 Theme 属性。
    /// </summary>
    [JsonPropertyName("themeIndex")]
    public int ThemeIndex
    {
        get => _themeIndex;
        set
        {
            _themeIndex = value;
            OnPropertyChanged();
            // 索引映射到主题名称
            Theme = value switch
            {
                1 => "Light",
                2 => "Dark",
                _ => "System"
            };
        }
    }

    /// <summary>主题名称（System/Light/Dark）。</summary>
    [JsonPropertyName("theme")]
    public string Theme
    {
        get => _theme;
        set { _theme = value; OnPropertyChanged(); }
    }

    /// <summary>字体缩放比例，1.0 为默认大小。</summary>
    [JsonPropertyName("fontScale")]
    public double FontScale
    {
        get => _fontScale;
        set { _fontScale = value; OnPropertyChanged(); }
    }

    /// <summary>底部 4 个标签页 URL 数组。</summary>
    [JsonPropertyName("tabUrls")]
    public string[] TabUrls
    {
        get => _tabUrls;
        set { _tabUrls = value; OnPropertyChanged(); }
    }

    /// <summary>属性变更事件，用于 MVVM 绑定更新。</summary>
    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>触发属性变更通知。</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
