using System.Diagnostics;

namespace SCAssistant.Maui.Services;

/// <summary>
/// 控制台/调试器日志实现。
/// </summary>
public class DebugLogService : ILogService
{
    public void Info(string message) => Trace.WriteLine($"[INFO]  {message}");
    public void Warn(string message) => Trace.WriteLine($"[WARN]  {message}");
    public void Error(string message, Exception? ex = null)
    {
        Trace.WriteLine($"[ERROR] {message}");
        if (ex != null) Trace.WriteLine($"[ERROR] {ex}");
    }
    public void Debug(string message) => Trace.WriteLine($"[DEBUG] {message}");
}
