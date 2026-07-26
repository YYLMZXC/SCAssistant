namespace SCAssistant.Maui.Models;

public class DownloadRecord
{
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string LocalPath { get; set; } = string.Empty;
    public DateTime DownloadTime { get; set; } = DateTime.Now;
}
