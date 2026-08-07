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

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        _serviceProvider = ConfigureServices();

        // 设置全局 DI 容器
        Ioc.Default.ConfigureServices(_serviceProvider);

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // 桌面端 — 浏览器 MainWindow
            var mainWindow = _serviceProvider.GetRequiredService<MainWindow>();
            mainWindow.DataContext = _serviceProvider.GetRequiredService<MainViewModel>();
            desktop.MainWindow = mainWindow;

        }
        else if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
        {
            // 移动端 — 浏览器 MainView
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

        // 服务注册
        services.AddSingleton<ISettingsService, SettingsService>();
        services.AddSingleton<IDownloadHistoryService, DownloadHistoryService>();
        services.AddSingleton<IDownloadService, DownloadService>();
        services.AddSingleton<IBrowserProvider, BrowserProvider>();
        // 日志服务 — 先创建实例以初始化全局静态入口
        var logService = new LogService();
        LogHelper.Initialize(logService);
        services.AddSingleton<ILogService>(logService);

        // ViewModel 注册
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<DownloadListViewModel>();
        services.AddTransient<HomeViewModel>();
        services.AddSingleton<MainViewModel>();

        // View 注册
        services.AddTransient<MainWindow>();
        services.AddTransient<MainView>();
        services.AddTransient<SettingsView>();
        services.AddTransient<HomeView>();

        return services.BuildServiceProvider();
    }
}
