using System;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using CommunityToolkit.Mvvm.DependencyInjection;
using SCAssistant.AvaloniaApp.ViewModels;

namespace SCAssistant.AvaloniaApp;

/// <summary>
/// View 定位器 — 通过 ViewModel 类型匹配对应的 View。
/// </summary>
public class ViewLocator : IDataTemplate
{
    public Control? Build(object? param)
    {
        if (param is null) return null;

        var vmName = param.GetType().FullName;
        if (vmName is null) return null;

        // ViewModel 名称 → View 名称 (MainViewModel → MainView, SettingsViewModel → SettingsView)
        var viewName = vmName
            .Replace("ViewModels", "Views")
            .Replace("ViewModel", "View");

        var viewType = Type.GetType(viewName);
        if (viewType is null) return new TextBlock { Text = $"View Not Found: {viewName}" };

        var control = (Control?)Activator.CreateInstance(viewType);
        if (control != null)
        {
            control.DataContext = param;
        }

        return control;
    }

    public bool Match(object? data)
    {
        return data is ViewModelBase;
    }
}
