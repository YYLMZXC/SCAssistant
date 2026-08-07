using System;
using System.Runtime.InteropServices;
using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 跨平台浏览器视图 — 负责创建和管理平台原生 WebView 控件。
/// 通过工厂模式支持桌面端 WebView2 和移动端原生 WebView。
/// </summary>
public partial class BrowserView : UserControl
{
    private IBrowserProvider? _browserProvider;

    /// <summary>
    /// 平台浏览器控件工厂 — 由各平台项目设置。
    /// 传入 IBrowserProvider，返回实际的浏览器控件。
    /// </summary>
    public static Func<IBrowserProvider, Control>? BrowserControlFactory { get; set; }

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

        try
        {
            // 优先使用平台工厂创建真正的浏览器控件
            if (BrowserControlFactory != null)
            {
                var control = BrowserControlFactory(browserProvider);
                WebViewContainer.Content = control;
                LogHelper.Info("[BrowserView] 通过工厂创建浏览器控件");
                return;
            }

            // 回退：根据运行时平台选择实现
            Control? browserControl = null;

            if (OperatingSystem.IsWindows())
            {
                browserControl = CreateWindowsBrowserControl();
            }
            else if (OperatingSystem.IsAndroid())
            {
                browserControl = CreateAndroidBrowserControl();
            }
            else if (OperatingSystem.IsIOS())
            {
                browserControl = CreateIosBrowserControl();
            }
            else if (OperatingSystem.IsLinux() || OperatingSystem.IsMacOS())
            {
                browserControl = CreateDesktopBrowserControl();
            }

            if (browserControl != null)
            {
                WebViewContainer.Content = browserControl;
                LogHelper.Info($"[BrowserView] 浏览器控件已创建 (平台: {Environment.OSVersion})");
            }
            else
            {
                ShowPlaceholder("浏览器初始化中...");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[BrowserView] 浏览器初始化失败", ex);
            ShowPlaceholder($"浏览器加载失败: {ex.Message}");
        }
    }

    /// <summary>
    /// Windows 桌面 — 通过反射创建 WebView2 控件。
    /// </summary>
    private Control? CreateWindowsBrowserControl()
    {
        // 尝试通过工厂创建（由 Desktop 项目注入）
        if (BrowserControlFactory != null)
        {
            return BrowserControlFactory(_browserProvider!);
        }

        // 无工厂时使用系统浏览器
        LogHelper.Warn("[BrowserView] 无 WebView2 工厂，将使用系统浏览器");
        return CreateSystemBrowserFallback();
    }

    /// <summary>
    /// 通用桌面回退方案 — 使用系统默认浏览器。
    /// </summary>
    private Control CreateDesktopBrowserControl()
    {
        return CreateSystemBrowserFallback();
    }

    /// <summary>
    /// Android 浏览器控件。
    /// </summary>
    private Control CreateAndroidBrowserControl()
    {
        // Android 原生 WebView 需要在 Android 项目中实现
        // 这里提供占位，实际实现通过工厂注入
        LogHelper.Info("[BrowserView] Android WebView 需要通过工厂注册");
        return CreatePlatformPlaceholder("Android WebView");
    }

    /// <summary>
    /// iOS 浏览器控件。
    /// </summary>
    private Control CreateIosBrowserControl()
    {
        LogHelper.Info("[BrowserView] iOS WebView 需要通过工厂注册");
        return CreatePlatformPlaceholder("iOS WebView");
    }

    /// <summary>
    /// 系统浏览器回退 — 点击链接在系统浏览器中打开。
    /// </summary>
    private Control CreateSystemBrowserFallback()
    {
        var panel = new StackPanel
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 16
        };

        panel.Children.Add(new TextBlock
        {
            Text = "🌐",
            FontSize = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = "浏览器",
            FontSize = 20,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = Avalonia.Media.Brushes.White,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = "将使用系统默认浏览器打开网页",
            FontSize = 13,
            Foreground = Avalonia.Media.Brushes.Gray,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        panel.Children.Add(new Button
        {
            Content = "打开网页",
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0078D4")),
            Foreground = Avalonia.Media.Brushes.White,
            CornerRadius = new Avalonia.CornerRadius(6),
            Padding = new Avalonia.Thickness(20, 10),
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Command = new CommunityToolkit.Mvvm.Input.RelayCommand(() =>
            {
                if (_browserProvider != null)
                {
                    SystemBrowserProvider.OpenUrl(_browserProvider?.GetCurrentUrl() ?? "https://www.scbbs.top/");
                }
            })
        });

        return panel;
    }

    private Control CreatePlatformPlaceholder(string platform)
    {
        var panel = new StackPanel
        {
            VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
            Spacing = 12
        };

        panel.Children.Add(new TextBlock
        {
            Text = "📱",
            FontSize = 48,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = $"{platform} 区域",
            FontSize = 16,
            Foreground = Avalonia.Media.Brushes.White,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        return panel;
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
                    Foreground = Avalonia.Media.Brushes.Gray,
                    FontSize = 16
                }
            }
        };
    }
}