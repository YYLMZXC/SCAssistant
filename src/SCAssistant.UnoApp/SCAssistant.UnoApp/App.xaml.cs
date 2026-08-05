using System.IO;
using System.Runtime.InteropServices;
using CommunityToolkit.Mvvm.DependencyInjection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using SCAssistant.UnoApp.Services;
using SCAssistant.UnoApp.ViewModels;
using SCAssistant.UnoApp.Views;
using Uno.Resizetizer;

namespace SCAssistant.UnoApp;

public partial class App : Application
{
    private Window? _mainWindow;

    public App()
    {
        this.InitializeComponent();
        LogHelper.Info("[应用] 构造函数 - 正在配置服务");
        ConfigureServices();
    }

    protected override void OnLaunched(LaunchActivatedEventArgs args)
    {
        LogHelper.Info("[应用] OnLaunched - 正在创建窗口");
        _mainWindow = new Window();

        var mainPage = new MainPage();
        _mainWindow.Content = mainPage;
        _mainWindow.Title = "SCAssistant - 生存战争助手";
        _mainWindow.SetWindowIcon();

        _mainWindow.Activate();

        // Win32 Skia 后端：通过原生 API 设置窗口图标
        if (OperatingSystem.IsWindows())
        {
            _mainWindow.DispatcherQueue.TryEnqueue(SetNativeWindowIcon);
        }

        LogHelper.Info("[应用] 窗口已激活");
    }

    private static void ConfigureServices()
    {
        var services = new ServiceCollection();

        // Core services
        var downloadHistory = new DownloadHistoryService();
        downloadHistory.Load();
        services.AddSingleton<IDownloadHistoryService>(downloadHistory);

        var downloadService = new DownloadService();
        services.AddSingleton<IDownloadService>(downloadService);

        var browserProvider = new BrowserProvider();
        services.AddSingleton<IBrowserProvider>(browserProvider);

        // ViewModels
        services.AddSingleton<MainViewModel>();
        services.AddSingleton<DownloadListViewModel>();

        var provider = services.BuildServiceProvider();
        Ioc.Default.ConfigureServices(provider);

        // Populate static ServiceLocator
        ServiceLocator.BrowserProvider = browserProvider;
        ServiceLocator.DownloadHistory = downloadHistory;
        ServiceLocator.DownloadService = downloadService;
        ServiceLocator.ServiceLocatorObj = new ServiceLocatorInstance();

        LogHelper.Info("[应用] 服务配置完成");
    }

    /// <summary>
    /// 通过 Win32 API 设置原生窗口图标（Skia/Win32 后端需要）
    /// </summary>
    private static void SetNativeWindowIcon()
    {
        var iconPath = Path.Combine(System.AppContext.BaseDirectory, "icon.ico");
        if (!File.Exists(iconPath))
        {
            LogHelper.Warn($"[窗口图标] icon.ico 未找到: {iconPath}");
            return;
        }

        // 从文件加载图标
        var hIcon = LoadImage(IntPtr.Zero, iconPath, IMAGE_ICON, 0, 0, LR_LOADFROMFILE | LR_SHARED);
        if (hIcon == IntPtr.Zero)
        {
            LogHelper.Warn($"[窗口图标] 加载 icon.ico 失败: {iconPath}");
            return;
        }

        // 通过窗口标题查找窗口句柄
        var hwnd = FindWindow(null, "SCAssistant - 生存战争助手");
        if (hwnd == IntPtr.Zero)
        {
            LogHelper.Warn("[窗口图标] 未能找到原生窗口句柄");
            return;
        }

        // WM_SETICON: ICON_SMALL (0) + ICON_BIG (1)
        SendMessage(hwnd, 0x0080, (IntPtr)0, hIcon); // ICON_SMALL - 任务栏
        SendMessage(hwnd, 0x0080, (IntPtr)1, hIcon); // ICON_BIG   - 标题栏/Alt+Tab

        LogHelper.Info("[窗口图标] 原生窗口图标已设置");
    }

    // --- Win32 P/Invoke ---

    private const uint IMAGE_ICON = 1;
    private const uint LR_LOADFROMFILE = 0x00000010;
    private const uint LR_SHARED = 0x00008000;

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr LoadImage(
        IntPtr hinst,
        string lpszName,
        uint uType,
        int cxDesired,
        int cyDesired,
        uint fuLoad);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr FindWindow(string? lpClassName, string? lpWindowName);

    [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
}
