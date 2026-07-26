namespace SCAssistant.Maui.Interfaces;

public interface IPlatformDownloadService
{
    Task<string?> SaveFileAsync(string fileName, Stream contentStream);
    Task<string?> SaveFileFromUrlAsync(string url, string fileName, IProgress<double>? progress = null);
    Task OpenFileAsync(string filePath);
    Task OpenFolderAsync(string folderPath);
}
