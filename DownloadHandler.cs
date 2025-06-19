using CefSharp;
using System;
using System.Windows.Forms;

public class DownloadHandler : IDownloadHandler
{
    private Form parentForm;
    public event Action<string, string> DownloadCreated;



    public DownloadHandler(Form form)
    {
        parentForm = form;
    }

    // 当是否允许下载时调用，返回true表示允许下载，false取消下载
    public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
    {
        // 你可以加逻辑判断，这里直接返回 true 允许所有下载
        return true;
    }

    // 下载开始前调用，返回 true 表示你异步调用 callback 继续下载
    public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
    {
        // 触发事件，通知有新下载
        DownloadCreated?.Invoke(downloadItem.SuggestedFileName, downloadItem.Url);

        // 下面是弹出保存对话框的UI线程调用示例（改为Invoke调用）
        bool result = false;
        parentForm.Invoke(new Action(() =>
        {
            using (var sfd = new SaveFileDialog())
            {
                sfd.FileName = downloadItem.SuggestedFileName;
                sfd.Filter = "所有文件 (*.*)|*.*";

                if (sfd.ShowDialog(parentForm) == DialogResult.OK)
                {
                    callback.Continue(sfd.FileName, showDialog: false);
                    result = true;
                }
            }
        }));

        return result;
    }


    // 下载进度更新回调
    public void OnDownloadUpdated(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IDownloadItemCallback callback)
    {
        parentForm.Invoke(new Action(() =>
        {
            parentForm.Text = $"下载 {downloadItem.PercentComplete}% - {System.IO.Path.GetFileName(downloadItem.FullPath)}";
        }));

        if (downloadItem.IsComplete)
        {
            parentForm.Invoke(new Action(() =>
            {
                MessageBox.Show(parentForm, $"文件下载完成:\n{downloadItem.FullPath}", "下载完成");
                parentForm.Text = "生存战争助手";
            }));
        }

        if (downloadItem.IsCancelled)
        {
            parentForm.Invoke(new Action(() =>
            {
                MessageBox.Show(parentForm, "下载已取消", "提示");
                parentForm.Text = "生存战争助手";
            }));
        }
    }
}
