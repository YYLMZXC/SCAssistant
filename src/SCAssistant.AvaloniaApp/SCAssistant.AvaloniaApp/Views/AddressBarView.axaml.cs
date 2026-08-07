using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using SCAssistant.AvaloniaApp.Services;
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
        LogHelper.Info("[AddrBarView] 构造函数开始 → 调用 InitializeComponent");
        try
        {
            InitializeComponent();
            LogHelper.Info("[AddrBarView] InitializeComponent 完成 — 开始注册事件");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[AddrBarView] InitializeComponent 失败!", ex);
            throw;
        }

        try
        {
            // 监听 DataContext 变更
            DataContextChanged += OnDataContextChanged;

            // 监听布局加载完成
            Loaded += OnLoaded;
            AttachedToVisualTree += OnAttachedToVisualTree;

            // 获取焦点时 → 标记编辑状态，阻止浏览器 URL 覆盖用户输入，并全选文本
            AddressTextBox.GotFocus += (_, _) =>
            {
                LogHelper.Debug("[AddrBarView] AddressTextBox.GotFocus 触发");
                if (DataContext is AddressBarViewModel vm)
                {
                    vm.SetEditing(true);
                }
                else
                {
                    LogHelper.Warn($"[AddrBarView] GotFocus 时 DataContext 不是 AddressBarViewModel: {DataContext?.GetType().Name ?? "null"}");
                }
                AddressTextBox.SelectAll();
            };

            // 失去焦点时 → 退出编辑状态，从浏览器同步最新 URL
            AddressTextBox.LostFocus += (_, _) =>
            {
                LogHelper.Debug("[AddrBarView] AddressTextBox.LostFocus 触发");
                if (DataContext is AddressBarViewModel vm)
                {
                    vm.SetEditing(false);
                }
                else
                {
                    LogHelper.Warn($"[AddrBarView] LostFocus 时 DataContext 不是 AddressBarViewModel: {DataContext?.GetType().Name ?? "null"}");
                }
            };

            // 回车键 → 触发导航
            AddressTextBox.KeyDown += (_, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    LogHelper.Debug("[AddrBarView] Enter 键触发导航");
                    if (DataContext is AddressBarViewModel vm)
                    {
                        vm.NavigateCommand.Execute(null);
                    }
                    else
                    {
                        LogHelper.Warn($"[AddrBarView] KeyDown 时 DataContext 不是 AddressBarViewModel: {DataContext?.GetType().Name ?? "null"}");
                    }
                    e.Handled = true;
                }
            };

            LogHelper.Info("[AddrBarView] 构造函数完成 — 所有事件注册完毕");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[AddrBarView] 事件注册失败!", ex);
            throw;
        }
    }

    /// <summary>DataContext 变更时打印详细信息用于诊断。</summary>
    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        var dc = DataContext;
        if (dc == null)
            LogHelper.Debug("[AddrBarView] DataContext 变更为 null");
        else
            LogHelper.Info($"[AddrBarView] DataContext 变更为: {dc.GetType().Name}" +
                (dc is AddressBarViewModel avm
                    ? $" (初始化={avm.IsInitialized}, UrlText='{avm.UrlText}')"
                    : " (非预期类型!)"));
    }

    /// <summary>布局加载完成回调。</summary>
    private void OnLoaded(object? sender, EventArgs e)
    {
        LogHelper.Info($"[AddrBarView] Loaded — IsVisible={IsVisible}, Bounds={Bounds}, " +
            $"DataContext={DataContext?.GetType().Name ?? "null"}");

        // 安全兜底：从浏览器强制同步一次 URL，确保地址栏始终显示当前页面地址。
        // 解决事件订阅时序 / 平台上报时序导致地址栏为空的问题。
        if (DataContext is AddressBarViewModel vm)
        {
            try
            {
                vm.SyncFromBrowser();
            }
            catch (Exception ex)
            {
                LogHelper.Error("[AddrBarView] Loaded 同步 URL 失败", ex);
            }
        }

        // 诊断内部控件状态：检查每个子控件是否可见、是否有正确的大小
        var grid = Content as Grid;
        if (grid == null)
        {
            LogHelper.Error("[AddrBarView] Loaded — Content 不是 Grid!");
        }
        else
        {
            // URL 框架现在是 Border（第3个子控件，索引2），其内部才是 TextBox
            var btnBack = grid.Children.Count > 0 ? grid.Children[0] : null;
            var btnFwd  = grid.Children.Count > 1 ? grid.Children[1] : null;
            var urlFrame = grid.Children.Count > 2 ? grid.Children[2] as Border : null;
            var btnGo   = grid.Children.Count > 3 ? grid.Children[3] : null;
            var txtBox  = urlFrame?.Child as TextBox;

            LogHelper.Info($"[AddrBarView] 子控件数={grid.Children.Count}, Grid.IsVisible={grid.IsVisible}, Grid.Bounds={grid.Bounds}");
            LogHelper.Info($"[AddrBarView] 后退Btn: IsVisible={btnBack?.IsVisible}, Bounds={btnBack?.Bounds}, IsEnabled={((btnBack as Avalonia.Controls.Button)?.IsEnabled)}");
            LogHelper.Info($"[AddrBarView] 前进Btn: IsVisible={btnFwd?.IsVisible}, Bounds={btnFwd?.Bounds}, IsEnabled={((btnFwd as Avalonia.Controls.Button)?.IsEnabled)}");
            LogHelper.Info($"[AddrBarView] URL框: IsVisible={urlFrame?.IsVisible}, Bounds={urlFrame?.Bounds}");
            LogHelper.Info($"[AddrBarView] 地址框: IsVisible={txtBox?.IsVisible}, Bounds={txtBox?.Bounds}, Text={txtBox?.Text}");
            LogHelper.Info($"[AddrBarView] GoBtn:   IsVisible={btnGo?.IsVisible}, Bounds={btnGo?.Bounds}");
        }
    }

    /// <summary>附加到视觉树回调。</summary>
    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        LogHelper.Info($"[AddrBarView] AttachedToVisualTree — Parent={e.Parent?.GetType().Name ?? "null"}, " +
            $"Root={VisualRoot?.GetType().Name ?? "null"}");
    }
}
