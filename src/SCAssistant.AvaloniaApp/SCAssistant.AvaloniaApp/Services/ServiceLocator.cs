namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 简易服务定位器 - 用于DI容器未就绪时的回退方案
/// </summary>
public static class ServiceLocator
{
    private static readonly Dictionary<Type, object> _services = new();
    private static readonly Dictionary<Type, Func<object>> _factories = new();
    private static readonly object _lock = new();

    /// <summary>
    /// 注册服务
    /// </summary>
    public static void Register<T>(T instance) where T : class
    {
        lock (_lock)
        {
            _services[typeof(T)] = instance;
        }
    }

    /// <summary>
    /// 注册服务工厂
    /// </summary>
    public static void Register<T>(Func<T> factory) where T : class
    {
        lock (_lock)
        {
            _factories[typeof(T)] = () => factory();
        }
    }

    /// <summary>
    /// 获取服务
    /// </summary>
    public static T? Get<T>() where T : class
    {
        lock (_lock)
        {
            if (_services.TryGetValue(typeof(T), out var instance))
                return (T)instance;

            if (_factories.TryGetValue(typeof(T), out var factory))
            {
                instance = factory();
                _services[typeof(T)] = instance;
                return (T)instance;
            }

            return null;
        }
    }

    /// <summary>
    /// 清空所有注册
    /// </summary>
    public static void Clear()
    {
        lock (_lock)
        {
            _services.Clear();
            _factories.Clear();
        }
    }
}
