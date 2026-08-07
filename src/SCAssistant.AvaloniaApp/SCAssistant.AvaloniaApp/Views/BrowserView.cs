using System;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 跨平台浏览器视图 — 负责创建和管理平台原生 WebView 控件。
/// 浏览器控件实现已合并到共享项目 WebViewBrowserControl.cs（单文件条件编译版），
/// 通过 App.axaml.cs 统一注册 BrowserControlFactory，此处只负责绑定并装载控件。
/// </summary>
public partial class BrowserView : UserControl
{
    private IBrowserProvider? _browserProvider;

    /// <summary>
    /// 平台浏览器控件工厂 — 由共享项目 App.axaml.cs 统一注册。
    /// 传入 IBrowserProvider，返回 WebViewBrowserControl（条件编译自动适配当前平台）。
    /// </summary>
    public static Func<IBrowserProvider, Control>? BrowserControlFactory { get; set; }

    public BrowserView()
    {
        InitializeComponent();
    }

    /// <summary>
    /// 初始化 WebView — 绑定到 BrowserProvider 并创建平台浏览器控件。
    /// 优先使用已注册的 BrowserControlFactory；若为空则直接创建 WebViewBrowserControl（同效果）。
    /// </summary>
    public void Initialize(IBrowserProvider browserProvider)
    {
        _browserProvider = browserProvider;

        try
        {
            Control browserControl;
            if (BrowserControlFactory != null)
            {
                browserControl = BrowserControlFactory(browserProvider);
                LogHelper.Info("[BrowserView] 通过 BrowserControlFactory 创建浏览器控件");
            }
            else
            {
                // 工厂未注册时的兜底路径（理论上不会触发，由 App.axaml.cs 保证工厂已注册）
                var webView = new WebViewBrowserControl();
                if (browserProvider is BrowserProvider bp) WireProvider(bp, webView);
                browserControl = webView;
                LogHelper.Info("[BrowserView] 直接创建 WebViewBrowserControl（工厂兜底）");
            }

            WebViewContainer.Content = browserControl;
        }
        catch (Exception ex)
        {
            LogHelper.Error("[BrowserView] 浏览器初始化失败", ex);
            ShowPlaceholder($"浏览器加载失败: {ex.Message}");
        }
    }

    /// <summary>工厂兜底路径下，将平台 WebView 事件桥接到 BrowserProvider。</summary>
    private static void WireProvider(BrowserProvider bp, WebViewBrowserControl wv)
    {
        bp.SetPlatformWebView(wv);
        if (!wv.IsReady) wv.ReadyChanged += (_, _) => bp.MarkPlatformReady();
        wv.AddressChanged += (_, url) => bp.HandlePlatformAddressChanged(url);
        wv.TitleChanged += (_, title) => bp.HandlePlatformTitleChanged(title);
        wv.LoadingStateChanged += (_, loading) => bp.HandlePlatformLoadingStateChanged(loading);
        wv.DownloadRequested += (_, url) => bp.HandlePlatformDownloadRequested(url);
        wv.NavigationHistoryChanged += (_, _) => bp.HandlePlatformNavigationHistoryChanged();
    }

    private void ShowPlaceholder(string message)
    {
        WebViewContainer.Content = new StackPanel
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#999999")),
                    FontSize = 16
                }
            }
        };
    }
}
