# SC 助手

SCAssistant（SC 助手，又名"生存战争助手"）为《生存战争》(Survivalcraft) 游戏玩家提供便捷的社区网站访问和下载管理功能。项目提供四套实现，其中 Avalonia、Uno Platform 和 MAUI 为跨平台 MVVM 架构，WindowsForms 为传统桌面实现：

| 实现 | UI 框架 | 浏览器引擎 | 运行时 | 支持平台 |
|------|---------|-----------|--------|----------|
| `SCAssistant.AvaloniaApp` | [Avalonia UI](https://www.avaloniaui.net/) | 原生 WebView | .NET 10 | Windows、Android、iOS、macOS、Linux |
| `SCAssistant.UnoApp` | [Uno Platform](https://platform.uno/) | Uno WebView2 | .NET 10 | Windows、macOS、Linux、Android、iOS |
| `SCAssistant.MauiApp` | [.NET MAUI](https://dotnet.microsoft.com/apps/maui) + [Open-MAUI-Linux](https://github.com/open-maui/maui-linux) | 原生 WebView | .NET 10 | Windows、Android、iOS、macOS、Linux |
| `SCAssistant.WindowsForms` | Windows Forms | [CefSharp](https://cefsharp.github.io/) | .NET Framework 4.7.2 | Windows |

## 主要功能

- **快捷导航**：内置首页、[SCKey](https://www.sckey.net) 和 [SCWZ](https://scwz.top/) 一键跳转，方便快速访问生存战争社区资源。
- **内置浏览器**：基于 WebView，各实现使用不同引擎：
  - Avalonia 版：Edge WebView2（Windows）、Android WebView、WKWebView（iOS/macOS）
  - Uno Platform 版：Uno WebView2（映射为各平台原生 WebView）
  - MAUI 版：Edge WebView2（Windows）、Android WebView、WKWebView（iOS/macOS）、Linux WebView（Open-MAUI-Linux）
  - WindowsForms 版：CefSharp（Chromium Embedded Framework）
- **下载管理**：支持多任务并发下载、进度显示、暂停/取消操作。
- **下载历史管理**：自动记录下载文件信息，支持查看记录、打开所在文件夹和删除记录。
- **设置管理**：可配置主页URL、搜索引擎、下载目录、最大并发下载数等。
- **跨平台支持**：
  - Avalonia 版：Windows、Android、iOS、macOS、Linux
  - Uno Platform 版：Windows、macOS、Linux、Android、iOS
  - MAUI 版：Windows、Android、iOS、macOS、Linux
  - WindowsForms 版：仅 Windows 桌面

## 技术架构

- **架构模式**：MVVM（CommunityToolkit.Mvvm，含依赖注入）
- **运行时**：.NET 10
- **序列化**：Newtonsoft.Json

### Avalonia 版

| 组件 | 版本 |
|------|------|
| Avalonia UI | 12.1.0 |
| Fluent Theme | 12.1.0 |
| DI 容器 | Microsoft.Extensions.DependencyInjection 10.0.3 |

### Uno Platform 版（单项目架构）

| 组件 | 版本 |
|------|------|
| Uno Platform SDK | 6.6.42 |
| Uno Toolkit / ThemeService | 随 SDK |
| 渲染器 | Skia |
| DI 容器 | CommunityToolkit.Mvvm（内置） |

### MAUI 版

| 组件 | 版本 |
|------|------|
| .NET MAUI | 10.0 |
| Open-MAUI-Linux | 10.0.70.4 |
| DI 容器 | Microsoft.Extensions.DependencyInjection |

### WindowsForms 版

| 组件 | 版本 |
|------|------|
| .NET Framework | 4.7.2 |
| CefSharp（浏览器引擎） | 135.0.220 |
| Newtonsoft.Json | 13.0.3 |

## 项目结构

```
src/
├── SCAssistant.AvaloniaApp/                          # Avalonia UI 实现
│   ├── SCAssistant.AvaloniaApp.slnx                  # 解决方案文件
│   ├── Directory.Packages.props                      # 集中包版本管理
│   ├── SCAssistant.AvaloniaApp/                      # 共享项目
│   │   ├── App.axaml / App.axaml.cs                  # 应用入口与依赖注入配置
│   │   ├── ViewModels/                               # MVVM 视图模型层
│   │   │   ├── ViewModelBase.cs                      # 基类
│   │   │   ├── MainViewModel.cs                      # 主页面逻辑
│   │   │   ├── DownloadListViewModel.cs              # 下载列表逻辑
│   │   │   └── SettingsViewModel.cs                  # 设置面板逻辑
│   │   ├── Views/                                    # 视图层
│   │   │   ├── MainWindow.axaml                      # 桌面端主窗口
│   │   │   ├── MainView.axaml                        # 移动端主视图
│   │   │   ├── HomeView.axaml                        # 主页/欢迎页面
│   │   │   └── SettingsView.axaml                    # 设置面板
│   │   ├── Models/                                   # 数据模型
│   │   ├── Services/                                 # 服务层（浏览器、下载、设置、历史）
│   │   └── Converters/                               # 值转换器
│   ├── SCAssistant.AvaloniaApp.Desktop/              # 桌面端平台项目
│   ├── SCAssistant.AvaloniaApp.Android/              # Android 平台项目
│   └── SCAssistant.AvaloniaApp.iOS/                  # iOS 平台项目
│
└── SCAssistant.UnoApp/                               # Uno Platform 实现
    ├── SCAssistant.UnoApp.slnx                       # 解决方案文件
    ├── global.json                                   # Uno SDK 版本声明
    └── SCAssistant.UnoApp/                           # 单项目（多平台）
        ├── SCAssistant.UnoApp.csproj                 # 项目文件（TargetFrameworks: Android/iOS/Desktop）
        ├── App.xaml / App.xaml.cs                    # 应用入口与依赖注入配置
        ├── ViewModels/                               # MVVM 视图模型层
        ├── Views/                                    # 视图层
        │   ├── MainPage.xaml                         # 主页面
        │   ├── DownloadListPanel.xaml                # 下载列表面板
        │   └── SettingsPanel.xaml                    # 设置面板
        ├── Models/                                   # 数据模型
        ├── Services/                                 # 服务层
        ├── Converters/                               # 值转换器
        ├── Assets/                                   # 图标与启动画面
        └── Platforms/                                # 平台入口文件
            ├── Android/                              # Android（MainActivity、Manifest）
            ├── Desktop/                              # 桌面端（Win32、X11、macOS）
            └── iOS/                                  # iOS（Info.plist、Entitlements）

├── SCAssistant.MauiApp/                               # .NET MAUI 实现
│   ├── SCAssistant.MauiApp.slnx                       # 解决方案文件
│   ├── SCAssistant.MauiApp/                          # 共享项目
│   │   ├── App.xaml / App.xaml.cs                    # 应用入口与依赖注入配置
│   │   ├── AppShell.xaml / AppShell.xaml.cs          # Shell 导航
│   │   ├── MainPage.xaml / MainPage.xaml.cs          # 主页面
│   │   ├── ViewModels/                               # MVVM 视图模型层
│   │   ├── Views/                                    # 视图层
│   │   ├── Models/                                   # 数据模型
│   │   ├── Services/                                 # 服务层（浏览器、下载、设置、历史）
│   │   └── Converters/                               # 值转换器
│   ├── SCAssistant.MauiApp.WinUI/                    # Windows 平台项目
│   ├── SCAssistant.MauiApp.Droid/                    # Android 平台项目
│   ├── SCAssistant.MauiApp.iOS/                      # iOS 平台项目
│   ├── SCAssistant.MauiApp.Mac/                      # macOS 平台项目
│   └── SCAssistant.MauiApp.Linux/                    # Linux 平台项目（Open-MAUI-Linux）

└── SCAssistant.WindowsForms/                         # Windows Forms + CefSharp 实现
    ├── SCAssistant.WindowsForms.sln                  # 解决方案文件
    ├── SCAssistant.WindowsForms/                     # 项目目录
    │   ├── SCAssistant.WindowsForms.csproj           # 项目文件（.NET Framework 4.7.2）
    │   ├── Program.cs                                # 应用入口
    │   ├── MainForm.cs / MainForm.Designer.cs        # 主窗体（内嵌 CefSharp 浏览器）
    │   ├── DownloadListForm.cs                       # 下载列表窗体
    │   ├── DownloadHandler.cs                        # CefSharp 下载处理器
    │   ├── DownloadRecord.cs                         # 下载记录数据模型
    │   ├── ContextMenuHandler.cs                     # 自定义右键菜单
    │   ├── CustomLifeSpanHandler.cs                  # 生命周期处理
    │   └── Properties/                               # 程序集信息与资源
    └── packages/                                     # 本地 NuGet 包
```

## 如何运行

### 环境要求

- **Avalonia / Uno Platform**：[.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **WindowsForms**：[.NET Framework 4.7.2 SDK](https://dotnet.microsoft.com/download/dotnet-framework/net472)
- Android：需要 Android SDK 及相关编译工具
- iOS / macOS：需要在 macOS 上使用 Xcode 进行编译

### Avalonia 版

**桌面端（Windows）**

```bash
dotnet run --project src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Desktop
```

或直接使用 Visual Studio / Rider 打开 `src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.slnx` 运行。

**Android**

```bash
dotnet build src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Android -c Release
```

生成的 APK 位于 `src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Android/bin/Release/net10.0-android/`。

**iOS**

在 macOS 上使用 Visual Studio for Mac 或 Rider 打开解决方案，选择 iOS 目标编译运行。

### Uno Platform 版

**桌面端（Windows / macOS / Linux）**

```bash
dotnet run --project src/SCAssistant.UnoApp/SCAssistant.UnoApp/SCAssistant.UnoApp.csproj
```

或直接使用 Visual Studio / Rider 打开 `src/SCAssistant.UnoApp/SCAssistant.UnoApp.slnx`，选择对应桌面目标运行。

**Android**

```bash
dotnet build src/SCAssistant.UnoApp/SCAssistant.UnoApp/SCAssistant.UnoApp.csproj -c Release -f net10.0-android
```

**iOS**

在 macOS 上打开解决方案，选择 iOS 目标编译运行。

### MAUI 版

**Windows 桌面**

```bash
dotnet run --project src/SCAssistant.MauiApp/SCAssistant.MauiApp.WinUI
```

**Linux 桌面**

首先安装系统依赖（Ubuntu/Debian）：

```bash
sudo apt install libx11-dev libxrandr-dev libxcursor-dev libxi-dev libgl1-mesa-dev libfontconfig1-dev
```

然后运行：

```bash
dotnet run --project src/SCAssistant.MauiApp/SCAssistant.MauiApp.Linux
```

**Android**

```bash
dotnet build src/SCAssistant.MauiApp/SCAssistant.MauiApp.Droid -c Release
```

**iOS / macOS**

在 macOS 上打开 `src/SCAssistant.MauiApp/SCAssistant.MauiApp.slnx`，选择 iOS 或 Mac 目标编译运行。

### WindowsForms 版

**Windows 桌面**

使用 Visual Studio 打开 `src/SCAssistant.WindowsForms/SCAssistant.WindowsForms.sln` 编译运行。 也可以使用 MSBuild：

```bash
msbuild src/SCAssistant.WindowsForms/SCAssistant.WindowsForms.sln -t:Build -p:Configuration=Release
```

注意：CefSharp 依赖本地 `packages/` 目录中的 NuGet 包，首次编译前请确保包已正确还原。

## 许可证

本项目使用 MIT 许可证。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进项目。如有任何问题或建议，请通过 GitHub Issue 反馈。

---

**SCAssistant - 生存战争助手，让社区访问更便捷**
