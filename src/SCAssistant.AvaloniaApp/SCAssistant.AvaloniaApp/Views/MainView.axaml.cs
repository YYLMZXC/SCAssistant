using Avalonia.Controls;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 移动端主视图 — 用于 Android/iOS 单视图生命周期。
/// 实际的 UI 布局由共享的 MainLayout 提供。
/// </summary>
public partial class MainView : UserControl
{
    public MainView(MainLayout mainLayout)
    {
        InitializeComponent();
        Content = mainLayout;
        LogHelper.Info("[MainView] 移动端视图构造完成");
    }
}
