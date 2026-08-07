using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;
using SCAssistant.AvaloniaApp.Views;

namespace SCAssistant.AvaloniaApp;

/// <summary>
/// Avalonia 应用程序入口 — 负责 DI 容器初始化、平台生命周期适配和启动流程。
/// BrowserControlFactory 在此处统一注册（条件编译），各平台入口点无需再重复注册。
/// </summary>
public partial class App : Avalonia.Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _serviceProvider = ConfigureServices();
        Ioc.Default.ConfigureServices(_serviceProvider);

        // ═══════════════════════════════════════════════════════════
        // 统一注册 BrowserControlFactory（以桌面端为标准的实现模式）。
        // 各平台入口点（Program.cs / MainActivity / AppDelegate）不再负责注册工厂。
        // ═══════════════════════════════════════════════════════════
        BrowserView.BrowserControlFactory = provider =>
        {
            var webView = new WebViewBrowserControl();
            if (provider is BrowserProvider browserProvider)
            {
                browserProvider.SetPlatformWebView(webView);

                if (!webView.IsReady)
                {
                    webView.ReadyChanged += (_, _) => browserProvider.MarkPlatformReady();
                }

                webView.AddressChanged += (_, url) => browserProvider.HandlePlatformAddressChanged(url);
                webView.TitleChanged += (_, title) => browserProvider.HandlePlatformTitleChanged(title);
                webView.LoadingStateChanged += (_, loading) => browserProvider.HandlePlatformLoadingStateChanged(loading);
                webView.DownloadRequested += (_, url) => browserProvider.HandlePlatformDownloadRequested(url);
                webView.NavigationHistoryChanged += (_, _) => browserProvider.HandlePlatformNavigationHistoryChanged();
            }
            return webView;
        };
        LogHelper.Info("[App] BrowserControlFactory 已在共享层注册");

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            LogHelper.Info("[App] 平台: 桌面端");
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            LogHelper.Info("[App] 平台: 移动端");
            var mainView = _serviceProvider.GetRequiredService<MainView>();
            mainView.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            singleView.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
        LogHelper.Info("[App] 框架初始化完成");
    }

    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDownloadHistoryService, DownloadHistoryService>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IBrowserProvider, BrowserProvider>();

        var logService = new LogService();
        LogHelper.Initialize(logService);
        services.AddSingleton<ILogService>(logService);
        LogHelper.Info("[App] 服务注册完成 (Settings/Download/History/Browser/Log)");

        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DownloadListViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddTransient<AddressBarViewModel>();
        services.AddSingleton<MainViewModel>();

        services.AddTransient<MainLayout>();
        services.AddTransient<MainWindow>();
        services.AddTransient<MainView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<HomeView>();
        LogHelper.Info("[App] ViewModel/View 注册完成");

        return services.BuildServiceProvider();
    }
}
