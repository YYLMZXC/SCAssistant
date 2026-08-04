using System;

namespace SCAssistant.UnoApp.Models;

public class DownloadRecord
{
    public string Id { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public DateTime DownloadTime { get; set; }
    public DateTime? CompletedTime { get; set; }
    public DownloadState State { get; set; }
    public string? ErrorMessage { get; set; }
    public double Progress { get; set; }

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
