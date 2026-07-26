using SCAssistant.Maui.Interfaces;
using SCAssistant.Maui.Models;

namespace SCAssistant.Maui.Services;

public class DownloadManager
{
    private static DownloadManager? _instance;
    private static readonly object _lock = new();

    public static DownloadManager Instance
    {
        get
        {
            lock (_lock)
            {
                _instance ??= new DownloadManager();
                return _instance;
            }
        }
    }

    private readonly CrossPlatformDownloadService _downloadService = new();

    public event EventHandler<DownloadProgressEventArgs>? DownloadProgressChanged;
    public event EventHandler<DownloadCompletedEventArgs>? DownloadCompleted;
    public event EventHandler<string>? DownloadFailed;

    private DownloadManager() { }

    public async Task StartDownloadAsync(string url, string? suggestedFileName = null)
    {
        var fileName = suggestedFileName ?? ExtractFileNameFromUrl(url);
        
        var progress = new Progress<double>(p =>
        {
            DownloadProgressChanged?.Invoke(this, new DownloadProgressEventArgs(fileName, p));
        });

        var result = await _downloadService.SaveFileFromUrlAsync(url, fileName, progress);
        
        if (result != null)
        {
            var record = new DownloadRecord
            {
                FileName = fileName,
                Url = url,
                LocalPath = result,
                DownloadTime = DateTime.Now
            };
            
            await DownloadHistoryService.Instance.AddRecordAsync(fileName, url, result);
            DownloadCompleted?.Invoke(this, new DownloadCompletedEventArgs(record));
        }
        else
        {
            DownloadFailed?.Invoke(this, fileName);
        }
    }

    private static string ExtractFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var path = uri.AbsolutePath;
            var fileName = Path.GetFileName(path);
            
            if (string.IsNullOrEmpty(fileName) || !Path.HasExtension(fileName))
            {
                fileName = $"download_{DateTime.Now:yyyyMMdd_HHmmss}";
            }
            
            return fileName;
        }
        catch
        {
            return $"download_{DateTime.Now:yyyyMMdd_HHmmss}";
        }
    }
}

public class DownloadProgressEventArgs : EventArgs
{
    public string FileName { get; }
    public double Progress { get; }

    public DownloadProgressEventArgs(string fileName, double progress)
    {
        FileName = fileName;
        Progress = progress;
    }
}

public class DownloadCompletedEventArgs : EventArgs
{
    public DownloadRecord Record { get; }

    public DownloadCompletedEventArgs(DownloadRecord record)
    {
        Record = record;
    }
}
