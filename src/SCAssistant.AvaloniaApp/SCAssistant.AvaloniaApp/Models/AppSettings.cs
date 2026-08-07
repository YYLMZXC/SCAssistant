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
    private string _homePageUrl = "https://www.google.com";
    private string _defaultSearchEngine = "https://www.google.com/search?q=";
    private string _downloadDirectory = string.Empty;
    private int _maxConcurrentDownloads = 3;
    private bool _enableDownloadHistory = true;
    private bool _enableAdBlock;
    private int _themeIndex;
    private string _theme = "System";
    private double _fontScale = 1.0;

    // 底部标签页 URL
    private string[] _tabUrls = Array.Empty<string>();

    [JsonPropertyName("homePageUrl")]
    public string HomePageUrl
    {
        get => _homePageUrl;
        set { _homePageUrl = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("defaultSearchEngine")]
    public string DefaultSearchEngine
    {
        get => _defaultSearchEngine;
        set { _defaultSearchEngine = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("downloadDirectory")]
    public string DownloadDirectory
    {
        get => _downloadDirectory;
        set { _downloadDirectory = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("maxConcurrentDownloads")]
    public int MaxConcurrentDownloads
    {
        get => _maxConcurrentDownloads;
        set { _maxConcurrentDownloads = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("enableDownloadHistory")]
    public bool EnableDownloadHistory
    {
        get => _enableDownloadHistory;
        set { _enableDownloadHistory = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("enableAdBlock")]
    public bool EnableAdBlock
    {
        get => _enableAdBlock;
        set { _enableAdBlock = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("themeIndex")]
    public int ThemeIndex
    {
        get => _themeIndex;
        set
        {
            _themeIndex = value;
            OnPropertyChanged();
            Theme = value switch
            {
                1 => "Light",
                2 => "Dark",
                _ => "System"
            };
        }
    }

    [JsonPropertyName("theme")]
    public string Theme
    {
        get => _theme;
        set { _theme = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("fontScale")]
    public double FontScale
    {
        get => _fontScale;
        set { _fontScale = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("tabUrls")]
    public string[] TabUrls
    {
        get => _tabUrls;
        set { _tabUrls = value; OnPropertyChanged(); }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
