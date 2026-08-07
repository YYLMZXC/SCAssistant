using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 设置持久化服务 — 将 AppSettings 读写为本地 JSON 文件。
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly string SettingsFilePath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SCAssistant",
        "settings.json");

    private AppSettings? _cached;
    private readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public Task<AppSettings> GetSettingsAsync()
    {
        if (_cached != null)
            return Task.FromResult(_cached);

        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            if (File.Exists(SettingsFilePath))
            {
                var json = File.ReadAllText(SettingsFilePath);
                _cached = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
            }
            else
            {
                _cached = new AppSettings();
                SaveSettingsAsync(_cached).GetAwaiter().GetResult();
            }
        }
        catch
        {
            _cached = new AppSettings();
        }

        LogHelper.Info("[SettingsService] 设置已加载");
        return Task.FromResult(_cached);
    }

    public Task SaveSettingsAsync(AppSettings settings)
    {
        _cached = settings;
        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            var json = JsonSerializer.Serialize(settings, _jsonOptions);
            File.WriteAllText(SettingsFilePath, json);
            LogHelper.Info("[SettingsService] 设置已保存");
        }
        catch (Exception ex)
        {
            LogHelper.Error("保存设置失败", ex);
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync()
    {
        _cached = new AppSettings();
        try
        {
            if (File.Exists(SettingsFilePath))
                File.Delete(SettingsFilePath);
        }
        catch (Exception ex)
        {
            LogHelper.Error("重置设置失败", ex);
        }

        return Task.CompletedTask;
    }
}
