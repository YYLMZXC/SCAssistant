using System.Diagnostics;

namespace SCAssistant.Maui.Services;

/// <summary>
/// 静态日志辅助类 — 通过注入的 ILogService 实现日志输出。
/// 所有 ViewModel / Service 通过此静态类记录日志，
/// 避免到处注入 ILogService。
/// </summary>
public static class LogHelper
{
    private static ILogService? _logService;

    public static void Initialize(ILogService logService)
    {
        _logService = logService;
    }

    public static void Info(string message) => _logService?.Info(message);
    public static void Warn(string message) => _logService?.Warn(message);
    public static void Error(string message, Exception? ex = null) => _logService?.Error(message, ex);
    public static void Debug(string message) => _logService?.Debug(message);
}
