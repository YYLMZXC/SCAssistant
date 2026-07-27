using System;
using Avalonia.Controls;

namespace AvaloniaApplication1.Services;

public interface IBrowserProvider
{
    Control CreateBrowserControl();
    void Navigate(string url);
    string CurrentUrl { get; }
    string CurrentTitle { get; }
    bool IsLoading { get; }

    event EventHandler<string>? AddressChanged;
    event EventHandler<string>? TitleChanged;
    event EventHandler<bool>? LoadingStateChanged;
    event EventHandler? BrowserCrashed;

    void Reload();
    void Initialize(string startUrl);
}
