namespace SCAssistant.Maui.Services;

/// <summary>
/// 日志接口。
/// </summary>
public interface ILogService
{
    void Info(string message);
    void Warn(string message);
    void Error(string message, Exception? ex = null);
    void Debug(string message);
}
