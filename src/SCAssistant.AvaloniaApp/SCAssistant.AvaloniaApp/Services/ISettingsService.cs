using System.Threading.Tasks;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 设置存储服务接口。
/// </summary>
public interface ISettingsService
{
    /// <summary>获取当前设置（先返回缓存）。</summary>
    Task<AppSettings> GetSettingsAsync();

    /// <summary>持久化设置。</summary>
    Task SaveSettingsAsync(AppSettings settings);

    /// <summary>重置为默认设置。</summary>
    Task ResetAsync();
}
