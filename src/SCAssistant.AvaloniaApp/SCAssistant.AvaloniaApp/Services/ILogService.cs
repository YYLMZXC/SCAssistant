namespace SCAssistant.AvaloniaApp.Services;

/// <summary>
/// 日志服务接口。
/// </summary>
public interface ILogService
{
    /// <summary>获取日志目录路径。</summary>
    string GetLogDirectory();

    /// <summary>记录调试日志。</summary>
    void Debug(string message);

    /// <summary>记录信息日志。</summary>
    void Info(string message);

    /// <summary>记录警告日志。</summary>
    void Warn(string message);

    /// <summary>记录错误日志，可选附带异常。</summary>
    void Error(string message, Exception? ex = null);
}
