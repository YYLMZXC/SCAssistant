using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

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
    }

    private void CloseButton_Click(object sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
