using CefSharp;
using System.Diagnostics;
using System.Windows.Forms;

public class CustomContextMenuHandler : IContextMenuHandler
{
    private const int OpenLinkInCurrentTab = 26501;
    private const int OpenLinkInExternalBrowser = 26502;
    private const int CopyLinkAddress = 26503;
    private const int CopyPageUrl = 26504;

    public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame,
                                    IContextMenuParams parameters, IMenuModel model)
    {
        // 保留默认菜单项（不要 model.Clear()）

        string url = parameters.LinkUrl ?? parameters.SourceUrl;

        model.AddSeparator();
        model.AddItem((CefMenuCommand)OpenLinkInCurrentTab, "在当前标签打开链接");
        model.AddItem((CefMenuCommand)OpenLinkInExternalBrowser, "在默认浏览器中打开链接");
        model.AddItem((CefMenuCommand)CopyLinkAddress, "复制链接地址");

        if (!string.IsNullOrEmpty(browser.MainFrame.Url))
        {
            model.AddSeparator();
            model.AddItem((CefMenuCommand)CopyPageUrl, "复制页面地址");
        }
    }

    public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame,
                                     IContextMenuParams parameters, CefMenuCommand commandId,
                                     CefEventFlags eventFlags)
    {
        string url = parameters.LinkUrl ?? parameters.SourceUrl;

        switch ((int)commandId)
        {
            case OpenLinkInCurrentTab:
                if (!string.IsNullOrEmpty(url))
                {
                    browser.MainFrame.LoadUrl(url);
                }
                else
                {
                    MessageBox.Show("无效的链接。");
                }
                return true;

            case OpenLinkInExternalBrowser:
                if (!string.IsNullOrEmpty(url))
                {
                    Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
                }
                else
                {
                    MessageBox.Show("无效的链接。");
                }
                return true;

            case CopyLinkAddress:
                if (!string.IsNullOrEmpty(url))
                {
                    Clipboard.SetText(url);
                }
                else
                {
                    Clipboard.SetText("（空链接）");
                }
                return true;

            case CopyPageUrl:
                string pageUrl = browser.MainFrame.Url;
                if (!string.IsNullOrEmpty(pageUrl))
                {
                    Clipboard.SetText(pageUrl);
                }
                return true;
        }

        return false; // 保留默认菜单功能（如复制/粘贴）
    }

    public void OnContextMenuDismissed(IWebBrowser chromiumWebBrowser, IBrowser browser, IFrame frame)
    {
        // 可选
    }

    public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame,
                               IContextMenuParams parameters, IMenuModel model,
                               IRunContextMenuCallback callback)
    {
        return false; // 使用默认右键菜单弹出方式
    }
}
