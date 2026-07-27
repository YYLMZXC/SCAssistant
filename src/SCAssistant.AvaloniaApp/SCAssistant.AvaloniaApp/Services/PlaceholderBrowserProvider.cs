using System;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 占位浏览器实现，作为 ServiceLocator 的初始默认值。
/// 各平台入口点应在应用启动时替换为对应平台的 IBrowserProvider 实现：
///   - Windows Desktop → CefBrowserProvider
///   - Android → AndroidBrowserProvider
///   - iOS → iOSBrowserProvider
///   - Linux/macOS Desktop → SystemBrowserProvider
/// </summary>
public class PlaceholderBrowserProvider : IBrowserProvider
{
#pragma warning disable CS0067 // Events are unused in placeholder
    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;
#pragma warning restore CS0067

    public string CurrentUrl => string.Empty;
    public string CurrentTitle => "浏览器未初始化";
    public bool IsLoading => false;

    public Control CreateBrowserControl()
    {
        return new TextBlock
        {
            Text = "浏览器未正确注册\n请检查平台入口点是否配置了 IBrowserProvider",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16,
            TextAlignment = TextAlignment.Center
        };
    }

    public void Initialize(string startUrl) { }
    public void Navigate(string url) { }
    public void Reload() { }
}
