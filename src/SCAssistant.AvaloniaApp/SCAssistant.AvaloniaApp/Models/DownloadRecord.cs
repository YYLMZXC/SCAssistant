using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text.Json.Serialization;

namespace SCAssistant.AvaloniaApp.Models;

public class DownloadRecord : INotifyPropertyChanged
{
    private string _id = string.Empty;
    private string _fileName = string.Empty;
    private string _url = string.Empty;
    private string _localPath = string.Empty;
    private long _fileSize;
    private DateTime _downloadTime;
    private DateTime? _completedTime;
    private DownloadState _state;
    private string? _errorMessage;
    private double _progress;

    [JsonPropertyName("id")]
    public string Id
    {
        get => _id;
        set { _id = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("fileName")]
    public string FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("url")]
    public string Url
    {
        get => _url;
        set { _url = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("localPath")]
    public string LocalPath
    {
        get => _localPath;
        set { _localPath = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("fileSize")]
    public long FileSize
    {
        get => _fileSize;
        set { _fileSize = value; OnPropertyChanged(); OnPropertyChanged(nameof(FileSizeDisplay)); }
    }

    [JsonIgnore]
    public string FileSizeDisplay =>
        _fileSize <= 0 ? "" :
        _fileSize < 1024 ? $"{_fileSize} B" :
        _fileSize < 1024 * 1024 ? $"{_fileSize / 1024.0:F1} KB" :
        _fileSize < 1024 * 1024 * 1024 ? $"{_fileSize / (1024.0 * 1024):F1} MB" :
        $"{_fileSize / (1024.0 * 1024 * 1024):F2} GB";

    [JsonPropertyName("downloadTime")]
    public DateTime DownloadTime
    {
        get => _downloadTime;
        set { _downloadTime = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("completedTime")]
    public DateTime? CompletedTime
    {
        get => _completedTime;
        set { _completedTime = value; OnPropertyChanged(); }
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    [JsonPropertyName("state")]
    public DownloadState State
    {
        get => _state;
        set { _state = value; OnPropertyChanged(); OnPropertyChanged(nameof(StateText)); }
    }

    [JsonPropertyName("errorMessage")]
    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    [JsonPropertyName("progress")]
    public double Progress
    {
        get => _progress;
        set { _progress = value; OnPropertyChanged(); OnPropertyChanged(nameof(ProgressText)); }
    }

    [JsonIgnore]
    public string ProgressText => State == DownloadState.Completed ? "100%" :
        State == DownloadState.Downloading ? $"{Progress:F0}%" : "";

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

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    public override string ToString() => $"[{State}] {FileName}";
}

public enum DownloadState
{
    Pending,
    Downloading,
    Completed,
    Failed,
    Cancelled
}
