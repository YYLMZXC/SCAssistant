using System.Globalization;

namespace SCAssistant.Maui.Converters;

/// <summary>
/// Bool 转 Visibility（true=Visible, false=Collapsed）。
/// </summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b)
        {
            var invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);
            return (invert ? !b : b);
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b ? b : false;
}

/// <summary>
/// Bool 取反。
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is bool b && !b;
}

/// <summary>
/// 进度 0.0-1.0 转百分比字符串。
/// </summary>
public class ProgressToPercentConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double d)
            return $"{(d * 100):F0}%";
        return "0%";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// 字节数转可读大小字符串。
/// </summary>
public class BytesToSizeConverter : IValueConverter
{
    private static readonly string[] SizeUnits = { "B", "KB", "MB", "GB" };

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long bytes)
        {
            if (bytes <= 0) return "0 B";

            var order = Math.Min(SizeUnits.Length - 1, (int)Math.Log(bytes, 1024));
            var size = bytes / Math.Pow(1024, order);
            return $"{size:F1} {SizeUnits[order]}";
        }
        return "0 B";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}

/// <summary>
/// 下载状态转颜色。
/// </summary>
public class DownloadStatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is Models.DownloadStatus status)
        {
            return status switch
            {
                Models.DownloadStatus.Downloading => Colors.DodgerBlue,
                Models.DownloadStatus.Completed => Colors.LimeGreen,
                Models.DownloadStatus.Failed => Colors.OrangeRed,
                Models.DownloadStatus.Cancelled => Colors.Gray,
                _ => Colors.Gray
            };
        }
        return Colors.Gray;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotImplementedException();
}
