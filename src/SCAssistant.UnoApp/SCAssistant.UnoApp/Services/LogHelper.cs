using System;
using System.IO;

namespace SCAssistant.UnoApp.Services;

/// <summary>
/// 日志系统：同时输出到 Console 窗口、Debug 输出和日志文件。
/// 日志文件保存在 SCAssistant/Bugs/log/ 目录下，按日期命名。
/// - Windows/Linux: %LocalAppData%/SCAssistant/Bugs/log/
/// - Android: 外部存储/SCAssistant/Bugs/log/（用户可访问）
/// - iOS: {LocalApplicationData}/SCAssistant/Bugs/log/
/// </summary>
public static class LogHelper
{
    private static readonly string LogDirectory;
    private static readonly object LockObj = new();

    static LogHelper()
    {
        var appDataDir = GetAppDataDirectory();
        LogDirectory = Path.Combine(appDataDir, "SCAssistant", "Bugs", "log");
        Directory.CreateDirectory(LogDirectory);
    }

    /// <summary>
    /// 获取应用数据基础目录。
    /// Android 使用外部存储（用户可访问），其他平台使用 LocalApplicationData。
    /// </summary>
    private static string GetAppDataDirectory()
    {
#if ANDROID
        return GetAndroidStoragePath() ??
               Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#else
        return Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
#endif
    }

#if ANDROID
    /// <summary>
    /// 获取 Android 外部存储路径（应用专属外部文件目录）。
    /// 返回 null 时回退到内部存储。
    /// </summary>
    private static string? GetAndroidStoragePath()
    {
        try
        {
            var context = Android.App.Application.Context;
            var externalDir = context?.GetExternalFilesDir(null);
            if (externalDir?.AbsolutePath is { } path && !string.IsNullOrWhiteSpace(path))
            {
                return path;
            }
        }
        catch
        {
            // 获取外部存储失败，回退到内部存储
        }
        return null;
    }
#endif

    private static string GetLogFilePath()
    {
        var date = DateTime.Now.ToString("yyyy-MM-dd");
        return Path.Combine(LogDirectory, $"app_{date}.log");
    }

    /// <summary>获取日志目录路径（供外部使用，如打开日志文件夹）。</summary>
    public static string GetLogDirectory() => LogDirectory;

    public static void Debug(string message)
    {
        Write("[DEBUG]", message);
    }

    public static void Info(string message)
    {
        Write("[INFO]", message);
    }

    public static void Warn(string message)
    {
        Write("[WARN]", message);
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
        System.Diagnostics.Debug.WriteLine(line);

        try
        {
            lock (LockObj)
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
