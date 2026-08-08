namespace SCAssistant.UnoApp.Models;

/// <summary>
/// 浏览器标识平台。
/// </summary>
public enum UserAgentPlatform
{
    /// <summary>跟随设备默认 UA</summary>
    Auto = 0,
    /// <summary>桌面浏览器（Windows Chrome）</summary>
    Desktop = 1,
    /// <summary>移动浏览器（Android Chrome）</summary>
    Mobile = 2
}

/// <summary>
/// 应用设置数据模型。
/// </summary>
public class AppSettings
{
    public UserAgentPlatform UserAgentPlatform { get; set; } = UserAgentPlatform.Auto;
}
