using CefSharp;
using CefSharp.WinForms;

public class CustomLifeSpanHandler : ILifeSpanHandler
{
    // 当网页请求打开新窗口时调用
    public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser,
        IFrame frame, string targetUrl, string targetFrameName,
        WindowOpenDisposition disposition, bool userGesture,
        IPopupFeatures popupFeatures, IWindowInfo windowInfo,
        IBrowserSettings browserSettings, ref bool noJavascriptAccess,
        out IWebBrowser newBrowser)
    {
        // 拦截弹窗请求，直接在当前浏览器加载目标链接
        chromiumWebBrowser.Load(targetUrl);

        // 不创建新浏览器控件
        newBrowser = null;

        // 返回 true 表示自己处理了弹窗，阻止默认新窗口打开
        return true;
    }

    public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        // 不需要特殊处理
    }

    public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        // 返回 false 表示允许关闭窗口
        return false;
    }

    public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        // 不需要特殊处理
    }
}
