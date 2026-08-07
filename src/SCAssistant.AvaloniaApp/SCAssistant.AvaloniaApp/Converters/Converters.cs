using System;
using System.Globalization;
using Avalonia.Data.Converters;
using SCAssistant.AvaloniaApp.Models;

namespace SCAssistant.AvaloniaApp.Converters;

/// <summary>布尔值取反。</summary>
public class InverseBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool b) return !b;
        return false;
    }
}

/// <summary>布尔 → 可见性（true=Visible, false=Hidden）。</summary>
public class BoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && b;
    }
}

/// <summary>布尔取反 → 可见性（true=Hidden, false=Visible）。</summary>
public class InverseBoolToVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool b && !b;
    }
}

/// <summary>下载状态 → 显示文本。</summary>
public class StatusToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DownloadState.Pending => "等待中",
            DownloadState.Downloading => "下载中",
            DownloadState.Completed => "已完成",
            DownloadState.Failed => "失败",
            DownloadState.Cancelled => "已取消",
            _ => ""
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => DownloadState.Pending;
}

/// <summary>下载状态 → 颜色。</summary>
public class StatusToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            DownloadState.Completed => Avalonia.Media.Brushes.LimeGreen,
            DownloadState.Downloading => Avalonia.Media.Brushes.DodgerBlue,
            DownloadState.Failed => Avalonia.Media.Brushes.OrangeRed,
            DownloadState.Cancelled => Avalonia.Media.Brushes.Gray,
            _ => Avalonia.Media.Brushes.Gray
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => DownloadState.Pending;
}

/// <summary>文件大小（字节）→ 可读文本。</summary>
public class FileSizeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is long size && size > 0)
        {
            if (size < 1024) return $"{size} B";
            if (size < 1024 * 1024) return $"{size / 1024.0:F1} KB";
            if (size < 1024 * 1024 * 1024) return $"{size / (1024.0 * 1024):F1} MB";
            return $"{size / (1024.0 * 1024 * 1024):F2} GB";
        }
        return "-";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => 0L;
}

/// <summary>字符串非空判断。</summary>
public class StringNotEmptyConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is string s && !string.IsNullOrWhiteSpace(s);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => string.Empty;
}

/// <summary>
/// 标签页索引 → 文本颜色画刷。
/// 用法：ConverterParameter 为目标索引，当前选中索引等于目标时显示蓝色，否则灰色。
/// </summary>
public class TabIndexToBrushConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int selected && parameter is string s && int.TryParse(s, out int index))
            return selected == index
                ? new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#0078D4"))   // 选中蓝色
                : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#666666"));  // 未选中灰色
        return new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#666666"));
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Avalonia.AvaloniaProperty.UnsetValue;
}

/// <summary>
/// 整数相等比较 → 布尔。
/// 用法：ConverterParameter 为目标值，当前值等于目标值时返回 true。
/// </summary>
public class IntEqualsConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int v && parameter is string s && int.TryParse(s, out int p))
            return v == p;
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value;
}

/// <summary>
/// 下载进度（0-100 double）→ 显示文本。
/// 进度 >= 100 显示 "完成"，否则显示百分比。
/// </summary>
public class ProgressToTextConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double progress)
        {
            if (progress >= 100) return "完成";
            return $"{progress:F0}%";
        }
        return "";
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => 0.0;
}

/// <summary>
/// 整数范围判断 → 布尔。
/// 用法：ConverterParameter 格式为 "min-max"，当前值在范围内返回 true。
/// </summary>
public class IntInRangeConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is int v && parameter is string s)
        {
            var parts = s.Split('-');
            if (parts.Length == 2 && int.TryParse(parts[0], out int min) && int.TryParse(parts[1], out int max))
                return v >= min && v <= max;
        }
        return false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value;
}
