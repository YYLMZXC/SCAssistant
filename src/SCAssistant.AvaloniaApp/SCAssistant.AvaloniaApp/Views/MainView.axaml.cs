using SCAssistant.AvaloniaApp.Services;
using SCAssistant.AvaloniaApp.ViewModels;
using Avalonia.Controls;

namespace SCAssistant.AvaloniaApp.Views;

public partial class MainView : UserControl
{
    private readonly IBrowserProvider _browser;

    public MainView(IBrowserProvider browser, SettingsViewModel settingsVm)
    {
        _browser = browser;
        InitializeComponent();
        SettingsPanel.DataContext = settingsVm;
        Loaded += OnLoaded;
        LogHelper.Info("[MainView] 移动端视图构造完成");
    }

    private void OnLoaded(object? sender, System.EventArgs e)
    {
        LogHelper.Info("[MainView] 视图加载 — 初始化浏览器区域");
        BrowserArea.Initialize(_browser);
        LogHelper.Info("[MainView] 浏览器区域初始化完成");
    }
}