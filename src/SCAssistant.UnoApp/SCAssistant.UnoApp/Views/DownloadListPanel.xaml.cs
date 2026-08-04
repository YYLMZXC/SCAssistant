using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using SCAssistant.UnoApp.Services;

namespace SCAssistant.UnoApp.Views;

public partial class DownloadListPanel : UserControl
{
    /// <summary>
    /// 外部订阅此事件以响应关闭操作。
    /// </summary>
    public event EventHandler? CloseRequested;

    public DownloadListPanel()
    {
        InitializeComponent();
        LogHelper.Info("[下载列表面板] 已构造");
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        LogHelper.Info("[下载列表面板] 关闭按钮点击");
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
