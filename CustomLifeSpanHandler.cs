using CefSharp;
using CefSharp.WinForms;

public class CustomLifeSpanHandler : ILifeSpanHandler
{
    // 当弹出新窗口请求时，调用此方法
    public bool OnBeforePopup(IWebBrowser chromiumWebBrowser, IBrowser browser,
        IFrame frame, string targetUrl, string targetFrameName,
        WindowOpenDisposition disposition, bool userGesture,
        IPopupFeatures popupFeatures, IWindowInfo windowInfo,
        IBrowserSettings browserSettings, ref bool noJavascriptAccess,
        out IWebBrowser newBrowser)
    {
        // 这里直接让当前浏览器加载新地址，阻止弹出新窗口
        chromiumWebBrowser.Load(targetUrl);
        newBrowser = null;

        // 返回 true 表示我们自己处理了，不要弹出新窗口
        return true;
    }

    public void OnAfterCreated(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        // 不需要处理
    }

    public bool DoClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        return false;
    }

    public void OnBeforeClose(IWebBrowser chromiumWebBrowser, IBrowser browser)
    {
        // 不需要处理
    }
}
