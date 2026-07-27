
# SC 助手

SCAssistant（SC 助手）是一个基于 [Avalonia UI](https://avaloniaui.net/) 的**跨平台**应用程序，为《生存战争》(Survivalcraft) 游戏玩家提供便捷的社区网站访问和下载管理功能。支持 Windows、Android 和 iOS 平台。

## 主要功能

- **快捷导航**：内置首页、[SCKey](https://www.sckey.net) 和 [SCWZ](https://scwz.top/) 一键跳转，方便快速访问生存战争社区资源。
- **内置浏览器**：各平台均使用原生浏览器内核，提供流畅的浏览体验：
  - Windows：基于 WebView2（Edge Chromium）
  - Android：基于系统原生 WebView
  - iOS：基于 WKWebView
- **下载历史管理**：自动记录下载文件信息，支持查看、打开文件夹和删除记录。
- **跨平台支持**：一套代码，同时编译为 Windows 桌面应用、Android APK 和 iOS 应用。

## 技术架构

- **UI 框架**：Avalonia UI 12 + Fluent 主题
- **运行时**：.NET 10
- **架构模式**：MVVM（使用 CommunityToolkit.Mvvm）
- **序列化**：Newtonsoft.Json

## 项目结构

```
src/SCAssistant.AvaloniaApp/
├── SCAssistant.AvaloniaApp/               # 共享核心项目（net10.0）
│   ├── App.axaml / App.axaml.cs           # 应用入口
│   ├── ViewModels/                        # MVVM 视图模型层
│   │   ├── ViewModelBase.cs               # 基类
│   │   ├── MainViewModel.cs               # 主页面逻辑
│   │   └── DownloadListViewModel.cs       # 下载列表逻辑
│   ├── Views/                             # MVVM 视图层
│   │   ├── MainWindow.axaml               # 桌面主窗口
│   │   ├── MainView.axaml                 # 主用户控件
│   │   └── DownloadListWindow.axaml       # 下载列表弹出面板
│   ├── Models/                            # 数据模型
│   │   └── DownloadRecord.cs              # 下载记录
│   └── Services/                          # 服务层
│       ├── IBrowserProvider.cs            # 浏览器提供者接口
│       ├── IDownloadHistoryService.cs     # 下载历史接口
│       ├── DownloadHistoryService.cs      # 下载历史实现
│       ├── ServiceLocator.cs              # 服务定位器
│       ├── PlaceholderBrowserProvider.cs  # 占位浏览器（未适配平台时使用）
│       └── SystemBrowserProvider.cs       # 系统浏览器降级方案
├── SCAssistant.AvaloniaApp.Desktop/       # 桌面项目（net10.0-windows）
│   ├── Program.cs                         # 桌面入口点
│   └── Services/WebView2BrowserProvider.cs # WebView2 浏览器实现
├── SCAssistant.AvaloniaApp.Android/       # Android 项目（net10.0-android）
│   ├── MainActivity.cs                    # Android 主 Activity
│   └── Services/AndroidBrowserProvider.cs  # Android 原生 WebView 实现
├── SCAssistant.AvaloniaApp.iOS/           # iOS 项目（net10.0-ios）
│   ├── AppDelegate.cs                     # iOS 应用代理
│   └── Services/iOSBrowserProvider.cs      # iOS WKWebView 实现
├── SCAssistant.AvaloniaApp.slnx           # 解决方案文件
└── Directory.Packages.props               # 中央包版本管理
```

## 依赖项

| 包名 | 版本 | 说明 |
|------|------|------|
| Avalonia | 12.1.0 | Avalonia UI 核心框架 |
| Avalonia.Themes.Fluent | 12.1.0 | Fluent 风格主题 |
| Avalonia.Fonts.Inter | 12.1.0 | Inter 字体 |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM 工具包 |
| Newtonsoft.Json | 13.0.3 | JSON 序列化 |
| Avalonia.Desktop | 12.1.0 | 桌面平台支持 |
| Avalonia.Controls.WebView | 12.0.1 | WebView2 浏览器控件 |
| Avalonia.Android | 12.1.0 | Android 平台支持 |
| Avalonia.iOS | 12.1.0 | iOS 平台支持 |

## 如何运行

### 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows**：需要安装 [WebView2 运行时](https://developer.microsoft.com/microsoft-edge/webview2/)（Windows 11 已内置）
- **Android**：需要 Android SDK 及相关编译工具
- **iOS**：需要在 macOS 上使用 Xcode 进行编译

### 桌面端（Windows）

```bash
cd src/SCAssistant.AvaloniaApp
dotnet run --project SCAssistant.AvaloniaApp.Desktop
```

或直接使用 Visual Studio / Rider 打开 `SCAssistant.AvaloniaApp.slnx`，选择 Desktop 项目运行。

### Android

```bash
cd src/SCAssistant.AvaloniaApp
dotnet build SCAssistant.AvaloniaApp.Android -c Release
```

生成的 APK 位于 `SCAssistant.AvaloniaApp.Android/bin/Release/net10.0-android/`。

### iOS

在 macOS 上使用 Visual Studio for Mac 或 Rider 打开解决方案，选择 iOS 项目编译运行。

## 许可证

本项目使用 MIT 许可证。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进项目。如有任何问题或建议，请通过 GitHub Issue 反馈。

---

**SCAssistant - 生存战争助手，让社区访问更便捷**
