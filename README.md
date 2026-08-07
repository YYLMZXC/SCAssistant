# SC 助手

SCAssistant（SC 助手）是一个基于 [Avalonia UI](https://www.avaloniaui.net/) 的**跨平台**应用程序，为《生存战争》(Survivalcraft) 游戏玩家提供便捷的社区网站访问和下载管理功能。一套代码同时支持 Windows 桌面、Android 和 iOS 平台。

## 主要功能

- **快捷导航**：内置首页、[SCKey](https://www.sckey.net) 和 [SCWZ](https://scwz.top/) 一键跳转，方便快速访问生存战争社区资源。
- **内置浏览器**：基于 Avalonia 的跨平台 WebView，自动映射为各平台原生浏览器：
  - Windows：Edge WebView2
  - Android：Android WebView
  - iOS：WKWebView
- **下载管理**：支持多任务并发下载、进度显示、暂停/取消操作。
- **下载历史管理**：自动记录下载文件信息，支持查看记录、打开所在文件夹和删除记录。
- **设置管理**：可配置主页URL、搜索引擎、下载目录、最大并发下载数等。
- **跨平台支持**：同时编译为 Windows 桌面应用、Android APK 和 iOS 应用。

## 技术架构

- **UI 框架**：Avalonia UI (Avalonia 12.x, Fluent Theme)
- **运行时**：.NET 10
- **架构模式**：MVVM（CommunityToolkit.Mvvm，含依赖注入 IOC）
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **序列化**：Newtonsoft.Json

## 项目结构

```
src/SCAssistant.AvaloniaApp/
├── SCAssistant.AvaloniaApp.slnx           # 解决方案文件
├── Directory.Packages.props               # 集中包版本管理
├── SCAssistant.AvaloniaApp/               # 共享项目
│   ├── App.axaml / App.axaml.cs           # 应用入口与依赖注入配置
│   ├── ViewModels/                        # MVVM 视图模型层
│   │   ├── ViewModelBase.cs               # 基类
│   │   ├── MainViewModel.cs               # 主页面逻辑（导航、浏览器状态、下载管理）
│   │   ├── DownloadListViewModel.cs        # 下载列表逻辑
│   │   └── SettingsViewModel.cs           # 设置面板逻辑
│   ├── Views/                             # 视图层
│   │   ├── MainWindow.axaml               # 桌面端主窗口（工具栏 + WebView + 浮层面板）
│   │   ├── MainView.axaml                 # 移动端主视图
│   │   ├── HomeView.axaml                 # 主页/欢迎页面
│   │   └── SettingsView.axaml             # 设置面板
│   ├── Models/                            # 数据模型
│   │   ├── DownloadRecord.cs              # 下载记录
│   │   └── AppSettings.cs                 # 应用设置
│   ├── Services/                          # 服务层
│   │   ├── IBrowserProvider.cs            # 浏览器提供者接口
│   │   ├── BrowserProvider.cs             # 跨平台 WebView 实现
│   │   ├── SystemBrowserProvider.cs        # 系统浏览器回退方案
│   │   ├── IDownloadService.cs            # 下载服务接口
│   │   ├── DownloadService.cs             # 下载服务实现
│   │   ├── IDownloadHistoryService.cs      # 下载历史接口
│   │   ├── DownloadHistoryService.cs       # 下载历史实现
│   │   ├── ISettingsService.cs            # 设置服务接口
│   │   ├── SettingsService.cs             # 设置服务实现
│   │   ├── LogHelper.cs                   # 日志辅助
│   │   └── ServiceLocator.cs              # 服务定位器（静态访问）
│   └── Converters/                        # 值转换器
│       └── Converters.cs                  # XAML 绑定转换器
├── SCAssistant.AvaloniaApp.Desktop/        # 桌面端平台项目
├── SCAssistant.AvaloniaApp.Android/        # Android 平台项目
│   ├── MainActivity.cs
│   └── Properties/AndroidManifest.xml
└── SCAssistant.AvaloniaApp.iOS/            # iOS 平台项目
    ├── AppDelegate.cs
    ├── Main.cs
    └── Info.plist
```

## 依赖项

| 包名 | 版本 | 说明 |
|------|------|------|
| Avalonia | 12.1.0 | Avalonia UI 框架 |
| Avalonia.Themes.Fluent | 12.1.0 | Fluent 风格主题 |
| Avalonia.Fonts.Inter | 12.1.0 | Inter 字体 |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM 工具包 |
| Microsoft.Extensions.DependencyInjection | 10.0.3 | 依赖注入容器 |
| Newtonsoft.Json | 13.0.4 | JSON 序列化 |

## 如何运行

### 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows 桌面**：无需额外安装浏览器运行时
- **Android**：需要 Android SDK 及相关编译工具
- **iOS**：需要在 macOS 上使用 Xcode 进行编译

### 桌面端（Windows）

```bash
dotnet run --project src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Desktop
```

或直接使用 Visual Studio / Rider 打开 `src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.slnx` 运行。

### Android

```bash
dotnet build src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Android -c Release
```

生成的 APK 位于 `src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Android/bin/Release/net10.0-android/`。

### iOS

在 macOS 上使用 Visual Studio for Mac 或 Rider 打开解决方案，选择 iOS 目标编译运行。

## 许可证

本项目使用 MIT 许可证。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进项目。如有任何问题或建议，请通过 GitHub Issue 反馈。

---

**SCAssistant - 生存战争助手，让社区访问更便捷**
