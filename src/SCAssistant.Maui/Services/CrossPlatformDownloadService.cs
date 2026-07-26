using System.Net.Http;
using System.IO;
using SCAssistant.Maui.Interfaces;

namespace SCAssistant.Maui.Services;

public class CrossPlatformDownloadService : IPlatformDownloadService
{
    private static readonly HttpClient _httpClient = new()
    {
        Timeout = TimeSpan.FromMinutes(5)
    };

    public async Task<string?> SaveFileAsync(string fileName, Stream contentStream)
    {
        try
        {
            var documentsPath = FileSystem.AppDataDirectory;
            var filePath = Path.Combine(documentsPath, fileName);
            
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            await contentStream.CopyToAsync(fileStream);
            
            return filePath;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public async Task<string?> SaveFileFromUrlAsync(string url, string fileName, IProgress<double>? progress = null)
    {
        try
        {
            var documentsPath = FileSystem.AppDataDirectory;
            var filePath = Path.Combine(documentsPath, fileName);
            
            using var response = await _httpClient.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            
            var totalBytes = response.Content.Headers.ContentLength;
            
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(filePath, FileMode.Create, FileAccess.Write);
            
            var buffer = new byte[81920];
            long totalRead = 0;
            int bytesRead;
            
            while ((bytesRead = await contentStream.ReadAsync(buffer, 0, buffer.Length)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                totalRead += bytesRead;
                
                if (progress != null && totalBytes.HasValue)
                {
                    progress.Report((double)totalRead / totalBytes.Value * 100);
                }
            }
            
            return filePath;
        }
        catch (Exception)
        {
            return null;
        }
    }

    public Task OpenFileAsync(string filePath)
    {
        try
        {
            if (File.Exists(filePath))
            {
                return Launcher.Default.OpenAsync(new OpenFileRequest
                {
                    File = new ReadOnlyFile(filePath)
                });
            }
        }
        catch (Exception)
        {
        }
        
        return Task.CompletedTask;
    }

    public Task OpenFolderAsync(string folderPath)
    {
        return Task.CompletedTask;
    }
}
