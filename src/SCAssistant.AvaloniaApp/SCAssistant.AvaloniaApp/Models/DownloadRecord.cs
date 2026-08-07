using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SCAssistant.AvaloniaApp.Models;

/// <summary>
/// 下载记录数据模型 — 表示单个文件的下载任务及其状态。
/// 支持属性变更通知，用于 MVVM 双向绑定。
/// </summary>
public class DownloadRecord : INotifyPropertyChanged
{
    /// <summary>唯一标识符（由下载服务生成）。</summary>
    private string _id = string.Empty;

    /// <summary>下载文件名。</summary>
    private string _fileName = string.Empty;

    /// <summary>下载源 URL。</summary>
    private string _url = string.Empty;

    /// <summary>本地保存路径。</summary>
    private string _localPath = string.Empty;

    /// <summary>文件大小（字节）。</summary>
    private long _fileSize;

    /// <summary>下载开始时间。</summary>
    private DateTime _downloadTime;

    /// <summary>下载完成时间（可为空）。</summary>
    private DateTime? _completedTime;

    /// <summary>当前下载状态。</summary>
    private DownloadState _state;

    /// <summary>错误信息（仅在失败时有值）。</summary>
    private string? _errorMessage;

    /// <summary>下载进度（0-100）。</summary>
    private double _progress;

    /// <summary>下载任务的唯一标识符。</summary>
    [JsonPropertyName("id")]
    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    /// <summary>下载文件名。</summary>
    [JsonPropertyName("fileName")]
    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    /// <summary>下载源 URL。</summary>
    [JsonPropertyName("url")]
    public string Url
    {
        get => _url;
        set { _url = value; OnPropertyChanged(); }
    }

    /// <summary>本地保存路径。</summary>
    [JsonPropertyName("localPath")]
    public string LocalPath
    {
        get => _localPath;
        set { _localPath = value; OnPropertyChanged(); }
    }

    /// <summary>文件大小（字节），修改时自动刷新 FileSizeDisplay。</summary>
    [JsonPropertyName("fileSize")]
    public long FileSize
    {
        get => _fileSize;
        set { _fileSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileSizeDisplay)); }
    }

    /// <summary>文件大小的可读显示文本（如 "1.5 MB"），不序列化。</summary>
    [JsonIgnore]
    public string FileSizeDisplay =>
        _fileSize <= 0 ? "" :
        _fileSize < 1024 ? $"{_fileSize} B" :
        _fileSize < 1024 * 1024 ? $"{_fileSize / 1024.0:F1} KB" :
        _fileSize < 1024 * 1024 * 1024 ? $"{_fileSize / (1024.0 * 1024):F1} MB" :
        $"{_fileSize / (1024.0 * 1024 * 1024):F2} GB";

    /// <summary>下载开始时间。</summary>
    [JsonPropertyName("downloadTime")]
    public DateTime DownloadTime
    {
        get => _downloadTime;
        set { _downloadTime = value; OnPropertyChanged(); }
    }

    /// <summary>下载完成时间（可为空）。</summary>
    [JsonPropertyName("completedTime")]
    public DateTime? CompletedTime
    {
        get => _completedTime;
        set { _completedTime = value; OnPropertyChanged(); }
    }

    /// <summary>当前下载状态，修改时自动刷新 StateText。</summary>
    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("state")]
    public DownloadState State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StateText)); }
    }

    /// <summary>错误信息（仅在失败时有值）。</summary>
    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    /// <summary>下载进度（0-100），修改时自动刷新 ProgressText。</summary>
    [JsonPropertyName("progress")]
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
    }

    /// <summary>进度文本显示：已完成→100%，下载中→百分比，否则为空。</summary>
    [JsonIgnore]
    public string ProgressText => State == DownloadState.Completed ? "100%" :
        State == DownloadState.Downloading ? $"{Progress:F0}%" : "";

    /// <summary>状态文本显示（中文）。</summary>
    [JsonIgnore]
    public string StateText => State switch
    {
        DownloadState.Pending => "等待中",
        DownloadState.Downloading => "下载中",
        DownloadState.Completed => "已完成",
        DownloadState.Failed => "失败",
        DownloadState.Cancelled => "已取消",
        _ => ""
    };

    public event PropertyChangedEventHandler? PropertyChanged;

    /// <summary>触发属性变更通知。</summary>
    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => $"[{State}] {FileName}";
}

/// <summary>下载状态枚举。</summary>
public enum DownloadState
{
    /// <summary>等待中。</summary>
    Pending,
    /// <summary>下载中。</summary>
    Downloading,
    /// <summary>已完成。</summary>
    Completed,
    /// <summary>失败。</summary>
    Failed,
    /// <summary>已取消。</summary>
    Cancelled
}
