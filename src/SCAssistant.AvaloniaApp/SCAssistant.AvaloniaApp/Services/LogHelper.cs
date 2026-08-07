namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 日志辅助类
/// </summary>
public static class LogHelper
{
    private static readonly string LogDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "SCAssistant",
        "logs");

    private static readonly object _lock = new();

    static LogHelper()
    {
        try
        {
            Directory.CreateDirectory(LogDirectory);
        }
        catch { }
    }

    /// <summary>
    /// 记录信息日志
    /// </summary>
    public static void Info(string message, string category = "General")
    {
        Log("INFO", message, category);
    }

    /// <summary>
    /// 记录警告日志
    /// </summary>
    public static void Warning(string message, string category = "General")
    {
        Log("WARNING", message, category);
    }

    /// <summary>
    /// 记录错误日志
    /// </summary>
    public static void Error(string message, string category = "General")
    {
        Log("ERROR", message, category);
    }

    /// <summary>
    /// 记录错误日志（含异常）
    /// </summary>
    public static void Error(Exception ex, string category = "General")
    {
        Log("ERROR", $"{ex.Message}\n{ex.StackTrace}", category);
    }

    private static void Log(string level, string message, string category)
    {
        try
        {
            var logEntry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{category}] {message}";
            var logFile = Path.Combine(LogDirectory, $"app_{DateTime.Now:yyyyMMdd}.log");

            lock (_lock)
            {
                File.AppendAllText(logFile, logEntry + Environment.NewLine);
            }

#if DEBUG
            System.Diagnostics.Debug.WriteLine(logEntry);
#endif
        }
        catch { }
    }
}
