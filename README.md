# SC 助手

SCAssistant（SC 助手，又名"生存战争助手"）为《生存战争》(Survivalcraft) 游戏玩家提供便捷的社区网站访问和下载管理功能。项目提供两套跨平台实现，共享相同的核心业务逻辑（MVVM + 依赖注入）：

| 实现 | UI 框架 | 渲染器 | 支持平台 |
|------|---------|--------|----------|
| `SCAssistant.AvaloniaApp` | [Avalonia UI](https://www.avaloniaui.net/) | 原生控件 | Windows、Android、iOS |
| `SCAssistant.UnoApp` | [Uno Platform](https://platform.uno/) | Skia | Windows、macOS、Linux、Android、iOS |

## 主要功能

- **快捷导航**：内置首页、[SCKey](https://www.sckey.net) 和 [SCWZ](https://scwz.top/) 一键跳转，方便快速访问生存战争社区资源。
- **内置浏览器**：基于跨平台 WebView，自动映射为各平台原生浏览器：
  - Windows：Edge WebView2
  - Android：Android WebView
  - iOS / macOS：WKWebView
- **下载管理**：支持多任务并发下载、进度显示、暂停/取消操作。
- **下载历史管理**：自动记录下载文件信息，支持查看记录、打开所在文件夹和删除记录。
- **设置管理**：可配置主页URL、搜索引擎、下载目录、最大并发下载数等。
- **跨平台支持**：
  - Avalonia 版：Windows 桌面、Android APK、iOS 应用
  - Uno Platform 版：Windows、macOS、Linux 桌面、Android APK、iOS 应用

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
```

## 如何运行

### 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows 桌面：无需额外安装浏览器运行时
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

## 许可证

本项目使用 MIT 许可证。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进项目。如有任何问题或建议，请通过 GitHub Issue 反馈。

---

**SCAssistant - 生存战争助手，让社区访问更便捷**
