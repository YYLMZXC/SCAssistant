using System;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 跨平台浏览器视图 — 负责创建和管理平台原生 WebView 控件。
/// </summary>
public partial class BrowserView : UserControl
{
    private IBrowserProvider? _browserProvider;

    public BrowserView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 初始化 WebView — 需要绑定到 IBrowserProvider。
    /// </summary>
    public void Initialize(IBrowserProvider browserProvider)
    {
        _browserProvider = browserProvider;

#if ANDROID
        InitializeAndroidWebView();
#elif IOS
        InitializeIosWebView();
#else
        InitializeDesktopWebView();
#endif
    }

    private void InitializeDesktopWebView()
    {
        // 桌面端 — 创建本地浏览器控件
        // Windows: WebView2
        // Linux: WebKitGTK
        // macOS: WKWebView

        try
        {
            var nativeControl = CreateDesktopBrowserControl();
            if (nativeControl != null)
            {
                WebViewContainer.Content = nativeControl;
                LogHelper.Info("[BrowserView] 桌面浏览器控件已创建");
            }
            else
            {
                ShowPlaceholder("浏览器初始化中...");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[BrowserView] 桌面浏览器初始化失败", ex);
            ShowPlaceholder("浏览器引擎加载失败");
        }
    }

    private Control? CreateDesktopBrowserControl()
    {
        // 尝试加载平台浏览器控件
        var platformControl = TryCreatePlatformControl();
        if (platformControl != null) return platformControl;

        // 回退到 WebView2（仅 Windows）
#if WINDOWS
        platformControl = TryCreateWebView2();
#endif
        return platformControl;
    }

    private Control? TryCreatePlatformControl()
    {
        try
        {
            // 尝试调用 Chromely 或类似库创建的控件
            var provider = _browserProvider as BrowserProvider;
            provider?.Initialize();
        }
        catch
        {
            // 忽略
        }

        return null;
    }

#if WINDOWS
    private Control? TryCreateWebView2()
    {
        try
        {
            // WebView2 创建逻辑
            // 需要 Microsoft.Web.WebView2 包
            // var webView = new Microsoft.Web.WebView2.Wpf.WebView2();
            // 或使用 Avalonia.Win32 原生宿主
            var placeholder = new Border
            {
                Background = Avalonia.Media.Brushes.White,
                Child = new TextBlock
                {
                    Text = "WebView2 浏览器区域\n\n将在此处加载网页内容",
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Foreground = Avalonia.Media.Brushes.Gray,
                    FontSize = 14
                }
            };
            return placeholder;
        }
        catch
        {
            return null;
        }
    }
#endif

    private void InitializeAndroidWebView()
    {
        try
        {
            var placeholder = new TextBlock
            {
                Text = "Android WebView 将在此加载",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 14
            };

            WebViewContainer.Content = placeholder;
            LogHelper.Info("[BrowserView] Android WebView 占位");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[BrowserView] Android WebView 初始化失败", ex);
        }
    }

    private void InitializeIosWebView()
    {
        try
        {
            var placeholder = new TextBlock
            {
                Text = "iOS WebView 将在此加载",
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 14
            };

            WebViewContainer.Content = placeholder;
            LogHelper.Info("[BrowserView] iOS WebView 占位");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[BrowserView] iOS WebView 初始化失败", ex);
        }
    }

    private void ShowPlaceholder(string message)
    {
        WebViewContainer.Content = new Border
        {
            Background = Avalonia.Media.Brushes.White,
            Child = new TextBlock
            {
                Text = message,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                Foreground = Avalonia.Media.Brushes.Gray,
                FontSize = 16
            }
        };
    }
}
