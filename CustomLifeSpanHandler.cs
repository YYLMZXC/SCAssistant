using CefSharp;

public class CustomContextMenuHandler : IContextMenuHandler
{
    private const int JumpMenuId = 26501;

    public void OnBeforeContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, IMenuModel model)
    {
        model.Clear(); // 清空默认菜单

        // 无论有没有链接，添加“跳转”菜单项
        model.AddItem((CefMenuCommand)JumpMenuId, "跳转");

        // 可以继续添加其他菜单项，比如刷新
        model.AddSeparator();
        model.AddItem(CefMenuCommand.Reload, "刷新");
    }

    public bool OnContextMenuCommand(IWebBrowser browserControl, IBrowser browser, IFrame frame, IContextMenuParams parameters, CefMenuCommand commandId, CefEventFlags eventFlags)
    {
        if ((int)commandId == JumpMenuId)
        {
            string targetUrl = parameters.LinkUrl;

            if (string.IsNullOrEmpty(targetUrl))
            {
                // 没有链接时跳转当前页面地址
                targetUrl = browser.MainFrame.Url;
            }

            if (!string.IsNullOrEmpty(targetUrl))
            {
                browser.MainFrame.LoadUrl(targetUrl);
            }
            return true;
        }

        return false;
    }

    public void OnContextMenuDismissed(IWebBrowser browserControl, IBrowser browser, IFrame frame)
    {
    }

    public bool RunContextMenu(IWebBrowser browserControl, IBrowser browser, IFrame frame,
        IContextMenuParams parameters, IMenuModel model, IRunContextMenuCallback callback)
    {
        return false; // 使用默认菜单
    }
}
