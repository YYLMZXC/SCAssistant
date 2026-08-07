using Avalonia.Controls;
using Avalonia.Input;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// 地址栏独立视图 — 与 MainLayout 完全解耦，自己管理 GotFocus/LostFocus 编辑状态。
/// 包含后退、前进、地址输入框和跳转按钮。
/// </summary>
public partial class AddressBarView : UserControl
{
    public AddressBarView()
    {
        InitializeComponent();

        // 获取焦点时 → 标记编辑状态，阻止浏览器 URL 覆盖用户输入，并全选文本
        AddressTextBox.GotFocus += (_, _) =>
        {
            if (DataContext is AddressBarViewModel vm)
            {
                vm.SetEditing(true);
            }
            AddressTextBox.SelectAll();
        };

        // 失去焦点时 → 退出编辑状态，从浏览器同步最新 URL
        AddressTextBox.LostFocus += (_, _) =>
        {
            if (DataContext is AddressBarViewModel vm)
            {
                vm.SetEditing(false);
            }
        };

        // 回车键 → 触发导航
        AddressTextBox.KeyDown += (_, e) =>
        {
            if (e.Key == Key.Enter)
            {
                if (DataContext is AddressBarViewModel vm)
                {
                    vm.NavigateCommand.Execute(null);
                }
                e.Handled = true;
            }
        };
    }
}
