using CefSharp;
using System.Windows.Forms;
using System;

public class DownloadHandler : IDownloadHandler
{
    private Form parentForm;
    public event Action<string, string> DownloadCreated;

    // 新增：用于确认下载路径时回调
    public Action<string> OnDownloadConfirmed;

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
                    OnDownloadConfirmed?.Invoke(sfd.FileName); // ✅ 通知保存路径
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
