using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 桌面端主窗口 — 用于 Windows/Linux/macOS 经典桌面生命周期。
/// 实际的 UI 布局由共享的 MainLayout 提供。
/// </summary>
public partial class MainWindow : Window
{
    public MainWindow(MainLayout mainLayout)
    {
        InitializeComponent();
        Content = mainLayout;
        LogHelper.Info("[MainWindow] 桌面窗口构造完成");
    }
}
