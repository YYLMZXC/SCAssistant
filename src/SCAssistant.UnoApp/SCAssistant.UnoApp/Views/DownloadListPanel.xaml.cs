using Microsoft.UI.Xaml.Controls;
using SCAssistant.UnoApp.Services;

namespace SCAssistant.UnoApp.Views;

public partial class DownloadListPanel : UserControl
{
    public DownloadListPanel()
    {
        InitializeComponent();
        LogHelper.Info("[下载列表面板] 已构造");
    }
}
