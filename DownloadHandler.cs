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

    public bool CanDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, string url, string requestMethod)
    {
        return true;
    }

    public bool OnBeforeDownload(IWebBrowser chromiumWebBrowser, IBrowser browser, DownloadItem downloadItem, IBeforeDownloadCallback callback)
    {
        // 触发事件通知新下载
        DownloadCreated?.Invoke(downloadItem.SuggestedFileName, downloadItem.Url);

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
