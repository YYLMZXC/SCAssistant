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
/// </summary>
public partial class App : Application
{
    /// <summary>全局 DI 服务提供者。</summary>
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    /// <summary>
    /// 框架初始化完成后根据生命周期类型创建对应的主视图。
    /// 桌面端（Windows/Linux/macOS）使用 MainWindow + 经典桌面生命周期，
    /// 移动端（Android/iOS）使用 MainView + 单视图生命周期。
    /// </summary>
    public override void OnFrameworkInitializationCompleted()
    {
        _serviceProvider = ConfigureServices();

        // 设置全局 DI 容器（CommunityToolkit.Mvvm 使用）
        Ioc.Default.ConfigureServices(_serviceProvider);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            LogHelper.Info("[App] 平台: 桌面端");
            // 桌面端 — 使用经典桌面窗口
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;
        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            LogHelper.Info("[App] 平台: 移动端");
            // 移动端 — 使用单视图容器
            var mainView = _serviceProvider.GetRequiredService<MainView>();
            mainView.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            singleView.MainView = mainView;
        }

        base.OnFrameworkInitializationCompleted();
        LogHelper.Info("[App] 框架初始化完成");
    }

    /// <summary>
    /// 配置依赖注入容器，注册所有服务、ViewModel 和 View。
    /// 日志服务需要最先创建以初始化全局静态入口 LogHelper。
    /// </summary>
    private static IServiceProvider ConfigureServices()
    {
        var services = new ServiceCollection();

        // ─── 服务注册 ───
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDownloadHistoryService, DownloadHistoryService>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IBrowserProvider, BrowserProvider>();

        // 日志服务 — 先创建实例以初始化全局静态入口 LogHelper
        var logService = new LogService();
        LogHelper.Initialize(logService);
        services.AddSingleton<ILogService>(logService);

        LogHelper.Info("[App] 服务注册完成 (Settings/Download/History/Browser/Log)");

        // ─── ViewModel 注册 ───
        // MainViewModel 为单例（全局唯一），其他 ViewModel 为瞬态（每次创建新实例）
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DownloadListViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddSingleton<MainViewModel>();

        // ─── View 注册 ───
        services.AddTransient<MainWindow>();
        services.AddTransient<MainView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<HomeView>();

        LogHelper.Info("[App] ViewModel/View 注册完成");

        return services.BuildServiceProvider();
    }
}
