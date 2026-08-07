using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.DependencyInjection;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp;

/// <summary>
/// View 定位器 — 通过命名约定将 ViewModel 自动匹配到对应的 View。
/// 规则：ViewModels.MainViewModel → Views.MainView,
///       ViewModels.SettingsViewModel → Views.SettingsView 等。
/// </summary>
public class ViewLocator : IDataTemplate
{
    /// <summary>根据 ViewModel 类型反射创建对应的 View 实例并绑定 DataContext。</summary>
    public Control? Build(object? param)
    {
        if (param is null) return null;

        var vmName = param.GetType().FullName;
        if (vmName is null) return null;

        // 命名约定转换: ViewModels → Views, ViewModel → View
        // 例如: SCAssistant.AvaloniaApp.ViewModels.MainViewModel
        //   → SCAssistant.AvaloniaApp.Views.MainView
        var viewName = vmName
            .Replace("ViewModels", "Views")
            .Replace("ViewModel", "View");

        var viewType = Type.GetType(viewName);
        if (viewType is null) return new TextBlock { Text = $"View Not Found: {viewName}" };

        // 通过反射创建 View 实例并设置 DataContext
        var control = (Control?)Activator.CreateInstance(viewType);
        if (control != null)
        {
            control.DataContext = param;
        }

        return control;
    }

    /// <summary>匹配规则：所有继承自 ViewModelBase 的对象都适用此模板。</summary>
    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
