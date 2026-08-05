using System;
using System.IO;
using System.Text.Json;
using SCAssistant.UnoApp.Models;

namespace SCAssistant.UnoApp.Services;

/// <summary>
/// 应用设置服务 — JSON 持久化到 %LocalAppData%/SCAssistant/settings.json。
/// </summary>
public class SettingsService : ISettingsService
{
    private static readonly string SettingsDir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SCAssistant");
    private static readonly string SettingsPath = Path.Combine(SettingsDir, "settings.json");

    public AppSettings Settings { get; private set; } = new();

    public SettingsService()
    {
        Directory.CreateDirectory(SettingsDir);
    }

    public void Load()
    {
        try
        {
            if (File.Exists(SettingsPath))
            {
                var json = File.ReadAllText(SettingsPath);
                var loaded = JsonSerializer.Deserialize<AppSettings>(json);
                if (loaded is not null)
                {
                    Settings = loaded;
                    LogHelper.Info($"[设置] 已加载: UA平台={Settings.UserAgentPlatform}");
                    return;
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[设置] 加载失败，使用默认设置", ex);
        }

        Settings = new AppSettings();
    }

    public void Save()
    {
        try
        {
            var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(SettingsPath, json);
            LogHelper.Info($"[设置] 已保存: UA平台={Settings.UserAgentPlatform}");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[设置] 保存失败", ex);
        }
    }
}
