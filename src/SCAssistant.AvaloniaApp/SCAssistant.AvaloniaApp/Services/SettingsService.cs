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

    /// <summary>
    /// 获取当前设置（先返回缓存）。
    /// 注意：文件 I/O 全部丢到 Task.Run 在线程池执行，避免阻塞 UI 消息循环。
    /// 此前为“伪异步”——同步读文件后返回 Task.FromResult，导致 await 并不让出 UI 线程。
    /// </summary>
    public async Task<AppSettings> GetSettingsAsync()
    {
        if (_cached != null)
        {
            LogHelper.Debug($"[SettingsService] 从缓存加载设置 (TabUrls={_cached.TabUrls?.Length ?? 0}个)");
            return _cached;
        }

        try
        {
            _cached = await Task.Run(() =>
            {
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (dir != null) Directory.CreateDirectory(dir);

                if (File.Exists(SettingsFilePath))
                {
                    LogHelper.Debug($"[SettingsService] 读取设置文件: {SettingsFilePath}");
                    var json = File.ReadAllText(SettingsFilePath);
                    var settings = JsonSerializer.Deserialize<AppSettings>(json, _jsonOptions) ?? new AppSettings();
                    LogHelper.Info($"[SettingsService] 从文件加载设置成功 (TabUrls={settings.TabUrls?.Length ?? 0}个)");
                    return settings;
                }

                LogHelper.Warn($"[SettingsService] 设置文件不存在，创建默认配置: {SettingsFilePath}");
                return new AppSettings();
            });
        }
        catch (Exception ex)
        {
            LogHelper.Error("[SettingsService] 加载设置失败，使用默认配置", ex);
            _cached = new AppSettings();
        }

        // 首次创建默认配置时异步持久化——不再用 GetAwaiter().GetResult() 阻塞 UI 线程
        if (!File.Exists(SettingsFilePath))
        {
            await SaveSettingsAsync(_cached);
        }

        return _cached;
    }

    /// <summary>持久化设置（文件 I/O 丢到线程池，不阻塞 UI）。</summary>
    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _cached = settings;
        try
        {
            await Task.Run(() =>
            {
                var dir = Path.GetDirectoryName(SettingsFilePath);
                if (dir != null) Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(settings, _jsonOptions);
                File.WriteAllText(SettingsFilePath, json);
            });
            LogHelper.Info($"[SettingsService] 设置已保存 → {SettingsFilePath}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[SettingsService] 保存设置失败: {SettingsFilePath}", ex);
        }
    }

    /// <summary>重置为默认设置（文件 I/O 丢到线程池，不阻塞 UI）。</summary>
    public async Task ResetAsync()
    {
        _cached = new AppSettings();
        try
        {
            await Task.Run(() =>
            {
                if (File.Exists(SettingsFilePath))
                {
                    File.Delete(SettingsFilePath);
                    LogHelper.Warn($"[SettingsService] 设置文件已删除: {SettingsFilePath}");
                }
            });
            LogHelper.Info("[SettingsService] 设置已重置为默认");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[SettingsService] 重置设置失败", ex);
        }
    }
}
