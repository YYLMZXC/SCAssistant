namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 日志系统：同时输出到 Console 窗口、Debug 输出和日志文件。
/// 日志文件保存在 SCAssistant/Bugs/log/ 目录下，按日期命名。
/// - Windows/Linux: %LocalAppData%/SCAssistant/Bugs/log/
/// - Android: 外部存储/SCAssistant/Bugs/log/（用户可访问）
/// - iOS: {LocalApplicationData}/SCAssistant/Bugs/log/
/// </summary>
public class LogService : ILogService
{
    private readonly string _logDirectory;
    private readonly object _lock = new();

    public LogService()
    {
        var appDataDir = GetAppDataDirectory();
        _logDirectory = Path.Combine(appDataDir, "SCAssistant", "Bugs", "log");
        try { Directory.CreateDirectory(_logDirectory); } catch { }
    }

    /// <summary>
    /// 获取应用数据基础目录。
    /// Android 使用外部存储（用户可访问），其他平台使用 LocalApplicationData。
    /// </summary>
    private static string GetAppDataDirectory()
    {
#if ANDROID
        try
        {
            var context = Android.App.Application.Context;
            var externalDir = context?.GetExternalFilesDir(null);
            if (externalDir?.AbsolutePath is { } path && !string.IsNullOrWhiteSpace(path))
            {
                return Path.GetDirectoryName(path) ?? path;
            }
        }
        catch { }
#endif
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
    }

    private string GetLogFilePath()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        return Path.Combine(_logDirectory, $"app_{date}.log");
    }

    /// <summary>获取日志目录路径（供外部使用，如打开日志文件夹）。</summary>
    public string GetLogDirectory() => _logDirectory;

    public void Debug(string message)
    {
        Write("[DEBUG]", message);
    }

    public void Info(string message)
    {
        Write("[INFO]", message);
    }

    public void Warn(string message)
    {
        Write("[WARN]", message);
    }

    public void Error(string message, Exception? ex = null)
    {
        var text = ex is not null ? $"{message} | Exception: {ex}" : message;
        Write("[ERROR]", text);
    }

    private void Write(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var line = $"{timestamp} {level} {message}";

        Console.WriteLine(line);
        System.Diagnostics.Debug.WriteLine(line);

        try
        {
            lock (_lock)
            {
                File.AppendAllText(GetLogFilePath(), line + Environment.NewLine);
            }
        }
        catch
        {
            // 写文件失败不抛异常
        }
    }
}

/// <summary>
/// 日志静态辅助类 — 全局静态入口，委托给 ILogService。
/// 需要先调用 Initialize 注入实例。
/// </summary>
public static class LogHelper
{
    private static ILogService? _service;

    /// <summary>初始化日志服务（由 App 启动时调用）。</summary>
    public static void Initialize(ILogService service)
    {
        _service = service;
        service.Info("[日志系统] 已初始化");
    }

    /// <summary>获取日志目录路径。</summary>
    public static string GetLogDirectory() => _service?.GetLogDirectory() ?? string.Empty;

    public static void Debug(string message) => _service?.Debug(message);
    public static void Info(string message) => _service?.Info(message);
    public static void Warn(string message) => _service?.Warn(message);
    public static void Error(string message, Exception? ex = null) => _service?.Error(message, ex);
}
