using System;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace SCAssistant.AvaloniaApp.Views;

public partial class DownloadListWindow : UserControl
{
    /// <summary>
    /// 外部订阅此事件以响应关闭操作。
    /// </summary>
    public event EventHandler? CloseRequested;

    public DownloadListWindow()
    {
        InitializeComponent();
    }

    private void CloseButton_Click(object? sender, RoutedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
