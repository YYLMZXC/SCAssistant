using System;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using SCAssistant.AvaloniaApp.Services;

namespace SCAssistant.AvaloniaApp.Views;

/// <summary>
/// Linux/macOS 平台 WebKit 浏览器控件 — 通过反射调用原生 WebKit API 实现。
/// Linux 下使用 WebKitGTK，macOS 下使用 WKWebView。
/// </summary>
public class WebKitWebViewBrowserControl : Control, IBrowserProvider, IDisposable
{
    private bool _isInitialized;
    private bool _disposed;
    private string _currentUrl = string.Empty;
    private bool _isLoading;

    // 原生 WebView 相关对象（通过反射创建）
    private object? _nativeWebView;      // WebKit.WebView (Linux) / WKWebView (macOS)
    private object? _nativeContainer;    // 容器控件
    private Type? _nativeWebViewType;
    private Type? _nativeContainerType;

    private MethodInfo? _loadUrlMethod;
    private MethodInfo? _reloadMethod;
    private MethodInfo? _goBackMethod;
    private MethodInfo? _goForwardMethod;
    private MethodInfo? _stopLoadingMethod;
    private MethodInfo? _evaluateJavaScriptMethod;
    private PropertyInfo? _urlProperty;
    private PropertyInfo? _titleProperty;
    private PropertyInfo? _canGoBackProperty;
    private PropertyInfo? _canGoForwardProperty;
    private PropertyInfo? _isLoadingProperty;

    public bool CanGoBack
    {
        get
        {
            if (_nativeWebView == null || _canGoBackProperty == null) return false;
            try { return (bool)_canGoBackProperty.GetValue(_nativeWebView)!; }
            catch { return false; }
        }
    }

    public bool CanGoForward
    {
        get
        {
            if (_nativeWebView == null || _canGoForwardProperty == null) return false;
            try { return (bool)_canGoForwardProperty.GetValue(_nativeWebView)!; }
            catch { return false; }
        }
    }

    public bool IsLoading
    {
        get
        {
            if (_nativeWebView == null || _isLoadingProperty == null) return _isLoading;
            try { return (bool)_isLoadingProperty.GetValue(_nativeWebView)!; }
            catch { return _isLoading; }
        }
    }

    public event EventHandler<string>? AddressChanged;
    public event EventHandler<string>? TitleChanged;
    public event EventHandler<bool>? LoadingStateChanged;
    public event EventHandler<string>? DownloadRequested;
    public event EventHandler? NavigationHistoryChanged;

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        if (!_isInitialized)
        {
            InitializeWebView();
        }
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnDetachedFromVisualTree(e);
        Dispose();
    }

    private void InitializeWebView()
    {
        try
        {
            if (OperatingSystem.IsLinux())
            {
                InitializeLinuxWebView();
            }
            else if (OperatingSystem.IsMacOS())
            {
                InitializeMacOSWebView();
            }
            else
            {
                LogHelper.Error("[WebKit] 不支持的操作系统");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebKit] 初始化失败", ex);
        }
    }

    private void InitializeLinuxWebView()
    {
        // Linux: 使用 WebKitGTK (通过反射)
        var gtkAssembly = Assembly.Load("WebKitSharp") ?? Assembly.Load("WebKit") ?? Assembly.Load("Gtk.WebKit");
        
        // 尝试多种可能的类型名称
        _nativeWebViewType = gtkAssembly?.GetType("WebKit.WebView") 
                            ?? gtkAssembly?.GetType("WebKitSharp.WebView")
                            ?? gtkAssembly?.GetType("Gtk.WebKit.WebView");

        if (_nativeWebViewType == null)
        {
            LogHelper.Warn("[WebKit] Linux 下未找到 WebKitGTK，将使用系统浏览器");
            return;
        }

        CreateNativeWebView();
    }

    private void InitializeMacOSWebView()
    {
        // macOS: 使用 AppKit/WebKit (通过反射)
        var assemblies = new[] { "Xamarin.Mac", "Microsoft.macOS" };
        Assembly? wkWebViewAssembly = null;
        
        foreach (var asmName in assemblies)
        {
            wkWebViewAssembly = Assembly.Load(asmName);
            if (wkWebViewAssembly != null) break;
        }

        if (wkWebViewAssembly == null)
        {
            // 尝试动态加载
            try
            {
                wkWebViewAssembly = Assembly.Load("WebKit");
            }
            catch { }
        }

        _nativeWebViewType = wkWebViewAssembly?.GetType("WebKit.WKWebView") 
                            ?? wkWebViewAssembly?.GetType("WKWebView");

        if (_nativeWebViewType == null)
        {
            LogHelper.Warn("[WebKit] macOS 下未找到 WKWebView，将使用系统浏览器");
            return;
        }

        CreateNativeWebView();
    }

    private void CreateNativeWebView()
    {
        try
        {
            if (_nativeWebViewType == null || _disposed) return;

            // 获取构造函数
            ConstructorInfo? ctor = null;
            
            // 尝试无参构造
            ctor = _nativeWebViewType.GetConstructor(Type.EmptyTypes);
            
            // 如果没有无参构造，尝试带参数的
            if (ctor == null)
            {
                var constructors = _nativeWebViewType.GetConstructors();
                foreach (var c in constructors)
                {
                    var candidateParams = c.GetParameters();
                    if (candidateParams.Length <= 2)
                    {
                        ctor = c;
                        break;
                    }
                }
            }

            if (ctor == null)
            {
                LogHelper.Error("[WebKit] 找不到 WebView 构造函数");
                return;
            }

            // 创建实例
            var ctorParams = ctor.GetParameters();
            object?[]? ctorArgs = null;
            
            if (ctorParams.Length == 0)
            {
                _nativeWebView = ctor.Invoke(null);
            }
            else if (ctorParams.Length == 1)
            {
                ctorArgs = new object?[1];
                var paramType = ctorParams[0].ParameterType;
                var paramCtor = paramType.GetConstructor(Type.EmptyTypes);
                ctorArgs[0] = paramCtor?.Invoke(null);
                _nativeWebView = ctor.Invoke(ctorArgs);
            }
            else
            {
                ctorArgs = new object?[ctorParams.Length];
                for (int i = 0; i < ctorParams.Length; i++)
                {
                    var paramType = ctorParams[i].ParameterType;
                    var paramCtor = paramType.GetConstructor(Type.EmptyTypes);
                    ctorArgs[i] = paramCtor?.Invoke(null);
                }
                _nativeWebView = ctor.Invoke(ctorArgs);
            }

            if (_nativeWebView == null)
            {
                LogHelper.Error("[WebKit] 创建 WebView 实例失败");
                return;
            }

            // 绑定属性和方法
            BindWebViewMembers();

            // 设置事件监听
            SetupEventListeners();

            // 将原生 WebView 嵌入 Avalonia 容器
            EmbedNativeWebView();

            SizeChanged += OnAvaloniaSizeChanged;

            _isInitialized = true;
            LogHelper.Info("[WebKit] WebView 初始化完成");

            if (!string.IsNullOrEmpty(_currentUrl))
            {
                Navigate(_currentUrl);
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebKit] 创建原生 WebView 失败", ex);
        }
    }

    private void BindWebViewMembers()
    {
        if (_nativeWebViewType == null) return;

        _loadUrlMethod = _nativeWebViewType.GetMethod("LoadUrl", new[] { typeof(string) })
                        ?? _nativeWebViewType.GetMethod("LoadRequest")
                        ?? _nativeWebViewType.GetMethod("Navigate", new[] { typeof(string) });
        
        _reloadMethod = _nativeWebViewType.GetMethod("Reload")
                       ?? _nativeWebViewType.GetMethod("Refresh");
        
        _goBackMethod = _nativeWebViewType.GetMethod("GoBack");
        _goForwardMethod = _nativeWebViewType.GetMethod("GoForward");
        _stopLoadingMethod = _nativeWebViewType.GetMethod("StopLoading");
        
        // macOS: EvaluateJavaScript
        _evaluateJavaScriptMethod = _nativeWebViewType.GetMethod("EvaluateJavaScript", 
            new[] { typeof(string) })
            ?? _nativeWebViewType.GetMethod("EvaluateJavaScript", 
                new[] { typeof(string), typeof(object) });
        
        _urlProperty = _nativeWebViewType.GetProperty("Url") 
                    ?? _nativeWebViewType.GetProperty("URL");
        _titleProperty = _nativeWebViewType.GetProperty("Title");
        _canGoBackProperty = _nativeWebViewType.GetProperty("CanGoBack");
        _canGoForwardProperty = _nativeWebViewType.GetProperty("CanGoForward");
        _isLoadingProperty = _nativeWebViewType.GetProperty("IsLoading")
                           ?? _nativeWebViewType.GetProperty("Loading");
    }

    private void SetupEventListeners()
    {
        if (_nativeWebView == null || _nativeWebViewType == null) return;

        try
        {
            // 尝试绑定 Linux WebKit 事件
            // LoadStarted, LoadFinished, LoadFailed
            var loadStartedEvent = _nativeWebViewType.GetEvent("LoadStarted");
            if (loadStartedEvent != null)
            {
                loadStartedEvent.AddEventHandler(_nativeWebView, 
                    new EventHandler((_, _) =>
                    {
                        _isLoading = true;
                        LoadingStateChanged?.Invoke(this, true);
                    }));
            }

            var loadFinishedEvent = _nativeWebViewType.GetEvent("LoadFinished");
            if (loadFinishedEvent != null)
            {
                loadFinishedEvent.AddEventHandler(_nativeWebView,
                    new EventHandler((_, _) =>
                    {
                        _isLoading = false;
                        LoadingStateChanged?.Invoke(this, false);
                        NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
                        UpdateUrlAndTitle();
                    }));
            }

            // 尝试绑定 macOS WKWebView 事件
            var didFinishNavigationEvent = _nativeWebViewType.GetEvent("DidFinishNavigation");
            if (didFinishNavigationEvent != null)
            {
                didFinishNavigationEvent.AddEventHandler(_nativeWebView,
                    new EventHandler((_, _) =>
                    {
                        _isLoading = false;
                        LoadingStateChanged?.Invoke(this, false);
                        NavigationHistoryChanged?.Invoke(this, EventArgs.Empty);
                        UpdateUrlAndTitle();
                    }));
            }

            var didStartProvisionalNavigationEvent = _nativeWebViewType.GetEvent("DidStartProvisionalNavigation");
            if (didStartProvisionalNavigationEvent != null)
            {
                didStartProvisionalNavigationEvent.AddEventHandler(_nativeWebView,
                    new EventHandler((_, _) =>
                    {
                        _isLoading = true;
                        LoadingStateChanged?.Invoke(this, true);
                    }));
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[WebKit] 事件绑定部分失败: {ex.Message}");
        }
    }

    private void EmbedNativeWebView()
    {
        if (_nativeWebView == null) return;

        try
        {
            // 获取平台句柄
            if (VisualRoot is TopLevel topLevel)
            {
                var handle = topLevel.TryGetPlatformHandle();
                if (handle != null)
                {
                    // 在 Linux 上获取 GTK 容器
                    // 在 macOS 上获取 NSView 容器
                    // 通过反射获取原生视图类型

                    // 尝试通过平台句柄获取原生容器
                    var handleType = handle.GetType();
                    var nativeViewProperty = handleType.GetProperty("NativeView") 
                                          ?? handleType.GetProperty("View");
                    
                    if (nativeViewProperty != null)
                    {
                        var nativeView = nativeViewProperty.GetValue(handle);
                        if (nativeView != null)
                        {
                            _nativeContainer = nativeView;
                            AddNativeViewToContainer(nativeView);
                        }
                    }
                }
            }

            if (_nativeContainer == null)
            {
                LogHelper.Warn("[WebKit] 未能嵌入原生 WebView — 将作为独立窗口显示");
            }
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebKit] 嵌入原生 WebView 失败", ex);
        }
    }

    private void AddNativeViewToContainer(object container)
    {
        try
        {
            // 尝试多种方法将原生 WebView 添加到容器
            var containerType = container.GetType();
            var addMethod = containerType.GetMethod("Add") 
                          ?? containerType.GetMethod("AddChild")
                          ?? containerType.GetMethod("PackStart");

            if (addMethod != null && _nativeWebView != null)
            {
                addMethod.Invoke(container, new[] { _nativeWebView });
            }
        }
        catch (Exception ex)
        {
            LogHelper.Warn($"[WebKit] 添加视图到容器失败: {ex.Message}");
        }
    }

    private void UpdateUrlAndTitle()
    {
        try
        {
            if (_urlProperty != null && _nativeWebView != null)
            {
                var url = _urlProperty.GetValue(_nativeWebView)?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(url) && url != _currentUrl)
                {
                    _currentUrl = url;
                    AddressChanged?.Invoke(this, url);
                }
            }

            if (_titleProperty != null && _nativeWebView != null)
            {
                var title = _titleProperty.GetValue(_nativeWebView)?.ToString() ?? string.Empty;
                if (!string.IsNullOrEmpty(title))
                {
                    TitleChanged?.Invoke(this, title);
                }
            }
        }
        catch { }
    }

    private void OnAvaloniaSizeChanged(object? sender, SizeChangedEventArgs e)
    {
        // Linux/macOS 下原生控件的布局需要通过父容器自动管理
    }

    #region IBrowserProvider Implementation

    public void Navigate(string url)
    {
        _currentUrl = url;

        if (!_isInitialized || _nativeWebView == null || _loadUrlMethod == null) return;

        try
        {
            if (!url.StartsWith("http://") && !url.StartsWith("https://") && !url.StartsWith("file://"))
            {
                url = "https://" + url;
            }

            _loadUrlMethod.Invoke(_nativeWebView, new object[] { url });
            LogHelper.Debug($"[WebKit] 导航: {url}");
        }
        catch (Exception ex)
        {
            LogHelper.Error($"[WebKit] 导航失败: {url}", ex);
        }
    }

    public void Reload()
    {
        if (_nativeWebView == null || _reloadMethod == null) return;
        try
        {
            _reloadMethod.Invoke(_nativeWebView, null);
            LogHelper.Debug("[WebKit] 刷新");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebKit] 刷新失败", ex);
        }
    }

    public void GoBack()
    {
        if (_nativeWebView == null || _goBackMethod == null || !CanGoBack) return;
        try
        {
            _goBackMethod.Invoke(_nativeWebView, null);
            LogHelper.Debug("[WebKit] 后退");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebKit] 后退失败", ex);
        }
    }

    public void GoForward()
    {
        if (_nativeWebView == null || _goForwardMethod == null || !CanGoForward) return;
        try
        {
            _goForwardMethod.Invoke(_nativeWebView, null);
            LogHelper.Debug("[WebKit] 前进");
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebKit] 前进失败", ex);
        }
    }

    public string GetCurrentUrl()
    {
        if (_urlProperty != null && _nativeWebView != null)
        {
            try
            {
                return _urlProperty.GetValue(_nativeWebView)?.ToString() ?? _currentUrl;
            }
            catch { }
        }
        return _currentUrl;
    }

    public string GetTitle()
    {
        if (_titleProperty != null && _nativeWebView != null)
        {
            try
            {
                return _titleProperty.GetValue(_nativeWebView)?.ToString() ?? string.Empty;
            }
            catch { }
        }
        return string.Empty;
    }

    public async Task<string> ExecuteScriptAsync(string script)
    {
        if (_nativeWebView == null || _evaluateJavaScriptMethod == null) return string.Empty;

        try
        {
            var tcs = new TaskCompletionSource<string>();

            // macOS: WKWebView.EvaluateJavaScript(script, completionHandler)
            // Linux: WebKit.WebView.ExecuteScript(script) - 可能同步
            var parameters = _evaluateJavaScriptMethod.GetParameters();
            
            if (parameters.Length == 2)
            {
                // 异步版本 (macOS)
                // 使用通用的 Action 委托避免 NSError 类型依赖
                var actionType = parameters[1].ParameterType;
                
                object completionHandler = null!;
                
                if (actionType.IsGenericType && actionType.GetGenericArguments().Length == 2)
                {
                    // 创建符合委托签名的通用委托
                    // 使用 Expression.Lambda 或直接创建强类型委托
                    var argTypes = actionType.GetGenericArguments();
                    
                    // 创建一个简单的 Action<object?, object?> 作为基础
                    Action<object?, object?> handler = (result, error) =>
                    {
                        if (error != null)
                            tcs.TrySetException(new Exception("JavaScript execution failed"));
                        else
                            tcs.TrySetResult(result?.ToString() ?? string.Empty);
                    };
                    
                    // 将其包装为所需的委托类型
                    // 通过 Delegate.CreateDelegate 直接从 lambda 创建
                    var invokeMethod = actionType.GetMethod("Invoke");
                    if (invokeMethod != null)
                    {
                        // 动态创建一个满足签名的委托
                        // 使用 Expression 构建 lambda 表达式
                        var paramExpr1 = System.Linq.Expressions.Expression.Parameter(argTypes[0], "result");
                        var paramExpr2 = System.Linq.Expressions.Expression.Parameter(argTypes[1], "error");
                        
                        var callExpr = System.Linq.Expressions.Expression.Call(
                            System.Linq.Expressions.Expression.Constant(handler),
                            typeof(Action<object?, object?>).GetMethod("Invoke")!,
                            System.Linq.Expressions.Expression.Convert(paramExpr1, typeof(object)),
                            System.Linq.Expressions.Expression.Convert(paramExpr2, typeof(object)));
                        
                        var lambdaExpr = System.Linq.Expressions.Expression.Lambda(
                            actionType,
                            callExpr,
                            paramExpr1,
                            paramExpr2);
                        
                        completionHandler = lambdaExpr.Compile();
                    }
                }
                
                _evaluateJavaScriptMethod.Invoke(_nativeWebView, new object?[]
                {
                    script,
                    completionHandler
                });
                return await tcs.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }
            else
            {
                // 同步版本 (Linux)
                var result = _evaluateJavaScriptMethod.Invoke(_nativeWebView, new object?[] { script });
                return result?.ToString() ?? string.Empty;
            }
        }
        catch
        {
            return string.Empty;
        }
    }

    public void Initialize()
    {
        if (!_isInitialized)
        {
            InitializeWebView();
        }
    }

    #endregion

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        try
        {
            if (_nativeWebView != null)
            {
                _stopLoadingMethod?.Invoke(_nativeWebView, null);

                // 移除事件处理
                _nativeWebView = null;
            }

            _nativeContainer = null;
            _nativeWebViewType = null;
            _nativeContainerType = null;

            SizeChanged -= OnAvaloniaSizeChanged;
        }
        catch (Exception ex)
        {
            LogHelper.Error("[WebKit] 释放资源时出错", ex);
        }
    }
}