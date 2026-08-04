using System;
using System.Diagnostics;
using System.IO;

namespace SCAssistant.UnoApp.Services;

/// <summary>
/// 简易调试日志：同时输出到 Console 窗口、Debug 输出和日志文件。
/// </summary>
public static class LogHelper
{
    private static readonly string LogFilePath;
    private static readonly object LockObj = new();

    static LogHelper()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "SCAssistant");
        Directory.CreateDirectory(dir);
        LogFilePath = Path.Combine(dir, "app.log");
    }

    public static void Info(string message)
    {
        Write("[INFO]", message);
    }

    public static void Error(string message, Exception? ex = null)
    {
        var text = ex is not null ? $"{message} | Exception: {ex}" : message;
        Write("[ERROR]", text);
    }

    private static void Write(string level, string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
        var line = $"{timestamp} {level} {message}";

        Console.WriteLine(line);
        Debug.WriteLine(line);

        try
        {
            lock (LockObj)
            {
                File.AppendAllText(LogFilePath, line + Environment.NewLine);
            }
        }
        catch
        {
            // 写文件失败不抛异常
        }
    }
}
