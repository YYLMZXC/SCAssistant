namespace SCAssistant.Maui.Services;

public static class NavigationConfig
{
    public static string DefaultUrl => "https://test.suancaixianyu.cn/";

    public static IReadOnlyList<NavButtonConfig> NavButtons { get; } = new List<NavButtonConfig>
    {
        new("SC中文社区", "https://test.suancaixianyu.cn/"),
        new("生存战争登录钥匙", "https://www.sckey.net"),
        new("生存战争网", "https://scwz.top/")
    };
}

public record NavButtonConfig(string Text, string Url);
