using System;
using Avalonia.Controls;
using Avalonia.Layout;

namespace SCAssistant.AvaloniaApp.Services;

public class PlaceholderBrowserProvider : IBrowserProvider
{
#pragma warning disable CS0067 // Events are unused in placeholder
    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler? BrowserCrashed;
#pragma warning restore CS0067

    public string CurrentUrl => string.Empty;
    public string CurrentTitle => "暂不支持";
    public bool IsLoading => false;

    public Control CreateBrowserControl()
    {
        return new TextBlock
        {
            Text = "浏览器仅支持 Windows 平台",
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            FontSize = 16
        };
    }

    public void Initialize(string startUrl) { }
    public void Navigate(string url) { }
    public void Reload() { }
}
