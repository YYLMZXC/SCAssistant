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
        {
            LogHelper.Debug($"[SettingsService] 从缓存加载设置 (TabUrls={_cached.TabUrls?.Length ?? 0}个)");
            return Task.FromResult(_cached);
        }

        try
        {
            var dir = Path.GetDirectoryName(SettingsFilePath);
            if (dir != null) Directory.CreateDirectory(dir);

            if (File.Exists(SettingsFilePath))
            {
                LogHelper.Debug($"[SettingsService] 读取设置文件: {SettingsFilePath}");
                var json = File.ReadAllText(SettingsFilePath);
                _cached = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
                LogHelper.Info($"[SettingsService] 从文件加载设置成功 (TabUrls={_cached.TabUrls?.Length ?? 0}个)");
            }
            else
            {
                LogHelper.Warn($"[SettingsService] 设置文件不存在，创建默认配置: {SettingsFilePath}");
                _cached = new AppSettings();
                SaveSettingsAsync(_cached).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[SettingsService] 加载设置失败，使用默认配置", ex);
            _cached = new AppSettings();
        }

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
            LogHelper.Info($"[SettingsService] 设置已保存 → {SettingsFilePath}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[SettingsService] 保存设置失败: {SettingsFilePath}", ex);
        }

        return Task.CompletedTask;
    }

    public Task ResetAsync()
    {
        _cached = new AppSettings();
        try
        {
            if (File.Exists(SettingsFilePath))
            {
                File.Delete(SettingsFilePath);
                LogHelper.Warn($"[SettingsService] 设置文件已删除: {SettingsFilePath}");
            }
            LogHelper.Info("[SettingsService] 设置已重置为默认");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[SettingsService] 重置设置失败", ex);
        }

        return Task.CompletedTask;
    }
}
