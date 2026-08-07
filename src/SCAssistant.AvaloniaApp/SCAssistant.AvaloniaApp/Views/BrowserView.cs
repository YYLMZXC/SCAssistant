using System;
using System.Reflection;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 跨平台浏览器视图 — 负责创建和管理平台原生 WebView 控件。
/// 内置简易地址栏（后退/前进/地址输入/跳转），通过工厂模式支持桌面端 WebView2、移动端原生 WebView 以及 Linux/macOS WebKit。
/// </summary>
public partial class BrowserView : UserControl
{
    private IBrowserProvider? _browserProvider;
    private bool _suppressAddressSync;

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

        // 订阅地址栏更新事件
        _browserProvider.AddressChanged += OnAddressChanged;
        _browserProvider.NavigationHistoryChanged += OnNavigationHistoryChanged;

        try
        {
            // 优先使用平台工厂创建真正的浏览器控件（由各平台入口点注册）
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

    #region 地址栏事件处理

    private void BtnBack_Click(object? sender, RoutedEventArgs e)
    {
        LogHelper.Debug("[BrowserView] 地址栏后退");
        _browserProvider?.GoBack();
    }

    private void BtnForward_Click(object? sender, RoutedEventArgs e)
    {
        LogHelper.Debug("[BrowserView] 地址栏前进");
        _browserProvider?.GoForward();
    }

    private void BtnGo_Click(object? sender, RoutedEventArgs e)
    {
        NavigateFromAddressBar();
    }

    private void AddressTextBox_KeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            NavigateFromAddressBar();
            e.Handled = true;
        }
    }

    private void NavigateFromAddressBar()
    {
        if (_browserProvider == null) return;

        var target = AddressTextBox.Text?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(target))
        {
            LogHelper.Warn("[BrowserView] 地址栏导航取消: URL 为空");
            return;
        }

        // 自动补全协议
        if (!target.StartsWith("http://") && !target.StartsWith("https://") && !target.StartsWith("file://"))
        {
            target = "https://" + target;
            _suppressAddressSync = true;
            AddressTextBox.Text = target;
            _suppressAddressSync = false;
        }

        LogHelper.Info($"[BrowserView] 地址栏导航: {target}");
        _browserProvider.Navigate(target);
    }

    private void OnAddressChanged(object? sender, string url)
    {
        if (_suppressAddressSync) return;

        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            if (AddressTextBox.Text != url)
            {
                _suppressAddressSync = true;
                AddressTextBox.Text = url;
                _suppressAddressSync = false;
            }
        });
    }

    private void OnNavigationHistoryChanged(object? sender, EventArgs e)
    {
        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
        {
            BtnBack.IsEnabled = _browserProvider?.CanGoBack ?? false;
            BtnForward.IsEnabled = _browserProvider?.CanGoForward ?? false;
        });
    }

    #endregion

    #region 平台浏览器控件创建

    /// <summary>
    /// Windows 桌面 — 通过反射查找 Desktop 项目中的 WebView2 控件。
    /// </summary>
    private Control? CreateWindowsBrowserControl()
    {
        // 尝试通过工厂创建（由 Desktop 项目注入）
        if (BrowserControlFactory != null)
        {
            return BrowserControlFactory(_browserProvider!);
        }

        // 尝试通过反射加载 Desktop 项目的 WebViewBrowserControl
        try
        {
            var desktopAssembly = Assembly.Load("SCAssistant.AvaloniaApp.Desktop");
            if (desktopAssembly != null)
            {
                var webViewType = desktopAssembly.GetType("SCAssistant.AvaloniaApp.Desktop.WebViewBrowserControl");
                if (webViewType != null)
                {
                    var control = (Control?)Activator.CreateInstance(webViewType);
                    if (control != null)
                    {
                        WireBrowserProvider(control);
                        LogHelper.Info("[BrowserView] 通过反射创建 WebView2 控件");
                        return control;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[BrowserView] 反射创建 WebView2 失败: {ex.Message}");
        }

        LogHelper.Warn("[BrowserView] 无 WebView2 实现，将使用系统浏览器");
        return CreateSystemBrowserFallback();
    }

    /// <summary>
    /// Linux/macOS 桌面 — 使用 WebKit 浏览器控件。
    /// </summary>
    private Control CreateDesktopBrowserControl()
    {
        try
        {
            var control = new WebKitWebViewBrowserControl();
            WireBrowserProvider(control);
            LogHelper.Info("[BrowserView] 创建 WebKit 浏览器控件");
            return control;
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[BrowserView] WebKit 初始化失败: {ex.Message}");
            return CreateSystemBrowserFallback();
        }
    }

    /// <summary>
    /// Android 浏览器控件 — 工厂未注册时的回退方案。
    /// 实际上 Android 项目会在 MainActivity 中注册工厂，此路径很少触发。
    /// </summary>
    private Control CreateAndroidBrowserControl()
    {
        try
        {
            // 尝试通过反射加载 Android 项目的 WebViewBrowserControl
            var androidAssembly = Assembly.Load("SCAssistant.AvaloniaApp.Android");
            if (androidAssembly != null)
            {
                var webViewType = androidAssembly.GetType("SCAssistant.AvaloniaApp.Android.WebViewBrowserControl");
                if (webViewType != null)
                {
                    var control = (Control?)Activator.CreateInstance(webViewType);
                    if (control != null)
                    {
                        WireBrowserProvider(control);
                        LogHelper.Info("[BrowserView] 通过反射创建 Android WebView 控件");
                        return control;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[BrowserView] 反射创建 Android WebView 失败: {ex.Message}");
        }

        LogHelper.Warn("[BrowserView] Android WebView 工厂未注册");
        return CreatePlatformPlaceholder("Android WebView");
    }

    /// <summary>
    /// iOS 浏览器控件 — 工厂未注册时的回退方案。
    /// 实际上 iOS 项目会在 Main.cs 中注册工厂，此路径很少触发。
    /// </summary>
    private Control CreateIosBrowserControl()
    {
        try
        {
            // 尝试通过反射加载 iOS 项目的 WebViewBrowserControl
            var iosAssembly = Assembly.Load("SCAssistant.AvaloniaApp.iOS");
            if (iosAssembly != null)
            {
                var webViewType = iosAssembly.GetType("SCAssistant.AvaloniaApp.iOS.WebViewBrowserControl");
                if (webViewType != null)
                {
                    var control = (Control?)Activator.CreateInstance(webViewType);
                    if (control != null)
                    {
                        WireBrowserProvider(control);
                        LogHelper.Info("[BrowserView] 通过反射创建 iOS WebView 控件");
                        return control;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[BrowserView] 反射创建 iOS WebView 失败: {ex.Message}");
        }

        LogHelper.Warn("[BrowserView] iOS WebView 工厂未注册");
        return CreatePlatformPlaceholder("iOS WebView");
    }

    /// <summary>
    /// 将平台浏览器控件的事件桥接到 BrowserProvider。
    /// 监听平台就绪事件后通知 BrowserProvider 执行排队的导航请求。
    /// </summary>
    private void WireBrowserProvider(Control control)
    {
        if (_browserProvider is not BrowserProvider browserProvider) return;

        if (control is IBrowserProvider webView)
        {
            browserProvider.SetPlatformWebView(webView);

            // 如果平台已就绪，直接标记（SetPlatformWebView 已处理此场景）
            // 如果平台未就绪，监听 ReadyChanged 后再标记
            if (!webView.IsReady)
            {
                webView.ReadyChanged += (_, _) =>
                {
                    browserProvider.MarkPlatformReady();
                };
            }

            webView.AddressChanged += (_, url) => browserProvider.HandlePlatformAddressChanged(url);
            webView.TitleChanged += (_, title) => browserProvider.HandlePlatformTitleChanged(title);
            webView.LoadingStateChanged += (_, loading) => browserProvider.HandlePlatformLoadingStateChanged(loading);
            webView.DownloadRequested += (_, url) => browserProvider.HandlePlatformDownloadRequested(url);
            webView.NavigationHistoryChanged += (_, _) => browserProvider.HandlePlatformNavigationHistoryChanged();
        }
    }

    #endregion

    #region 回退与占位

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
            Foreground = Avalonia.Media.Brushes.DarkGray,
            HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center
        });

        panel.Children.Add(new TextBlock
        {
            Text = "将使用系统默认浏览器打开网页",
            FontSize = 13,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#666666")),
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
            Foreground = Avalonia.Media.Brushes.DarkGray,
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
                    Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#999999")),
                    FontSize = 16
                }
            }
        };
    }

    #endregion
}