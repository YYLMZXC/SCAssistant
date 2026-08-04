# SC 助手

SCAssistant（SC 助手）是一个基于 [Uno Platform](https://platform.uno/) 的**跨平台**应用程序，为《生存战争》(Survivalcraft) 游戏玩家提供便捷的社区网站访问和下载管理功能。一套代码同时支持 Windows 桌面、Android 和 iOS 平台。

## 主要功能

- **快捷导航**：内置首页、[SCKey](https://www.sckey.net) 和 [SCWZ](https://scwz.top/) 一键跳转，方便快速访问生存战争社区资源。
- **内置浏览器**：基于 Uno Platform 的跨平台 WebView2，自动映射为各平台原生浏览器：
  - Windows：Edge WebView2
  - Android：Android WebView
  - iOS：WKWebView
  - 桌面 Skia 环境：自动回退到系统默认浏览器打开
- **下载历史管理**：自动记录下载文件信息，支持查看记录、打开所在文件夹和删除记录。
- **跨平台支持**：单一项目（Uno Single Project）同时编译为 Windows 桌面应用、Android APK 和 iOS 应用。

## 技术架构

- **UI 框架**：Uno Platform（Uno.Sdk，SkiaRenderer + WinUI 风格）
- **运行时**：.NET 10
- **架构模式**：MVVM（CommunityToolkit.Mvvm，含依赖注入 IOC）
- **依赖注入**：Microsoft.Extensions.DependencyInjection
- **序列化**：Newtonsoft.Json

## 项目结构

```
src/SCAssistant.UnoApp/
├── SCAssistant.UnoApp.slnx           # 解决方案文件
├── global.json                       # .NET SDK 版本锁定
├── nuget.config                      # NuGet 源配置
└── SCAssistant.UnoApp/               # 单一项目（Uno Single Project）
    ├── App.xaml / App.xaml.cs        # 应用入口与依赖注入配置
    ├── ViewModels/                   # MVVM 视图模型层
    │   ├── ViewModelBase.cs          # 基类
    │   ├── MainViewModel.cs          # 主页面逻辑（导航、浏览器状态）
    │   └── DownloadListViewModel.cs  # 下载列表逻辑
    ├── Views/                        # 视图层
    │   ├── MainPage.xaml             # 主页面（工具栏 + 浏览器宿主 + 下载列表浮层）
    │   └── DownloadListPanel.xaml    # 下载列表弹出面板
    ├── Models/                       # 数据模型
    │   └── DownloadRecord.cs         # 下载记录
    ├── Services/                     # 服务层
    │   ├── IBrowserProvider.cs       # 浏览器提供者接口
    │   ├── BrowserProvider.cs        # 跨平台 WebView2 实现
    │   ├── SystemBrowserProvider.cs  # 系统浏览器回退方案
    │   ├── IDownloadHistoryService.cs # 下载历史接口
    │   ├── DownloadHistoryService.cs  # 下载历史实现
    │   └── ServiceLocator.cs          # 服务定位器（静态访问）
    ├── Converters/                   # XAML 值转换器
    ├── Platforms/                    # 平台特定入口与配置
    │   ├── Desktop/Program.cs        # 桌面入口
    │   ├── Android/                  # Android 入口与清单
    │   └── iOS/                      # iOS 入口与配置
    └── Assets/                       # 应用资源
```

## 依赖项

| 包名 | 版本 | 说明 |
|------|------|------|
| Uno.Sdk | - | Uno Platform SDK（单项目构建，版本由 global.json 锁定） |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM 工具包 |
| Newtonsoft.Json | 13.0.3 | JSON 序列化 |

## 如何运行

### 环境要求

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows 桌面**：无需额外安装浏览器运行时（使用系统 Edge WebView2）
- **Android**：需要 Android SDK 及相关编译工具
- **iOS**：需要在 macOS 上使用 Xcode 进行编译

### 桌面端（Windows）

```bash
dotnet run --project src/SCAssistant.UnoApp/SCAssistant.UnoApp -f net10.0-desktop
```

或直接使用 Visual Studio / Rider 打开 `src/SCAssistant.UnoApp/SCAssistant.UnoApp.slnx` 运行。

### Android

```bash
dotnet build src/SCAssistant.UnoApp/SCAssistant.UnoApp -f net10.0-android -c Release
```

生成的 APK 位于 `src/SCAssistant.UnoApp/SCAssistant.UnoApp/bin/Release/net10.0-android/`。

### iOS

在 macOS 上使用 Visual Studio for Mac 或 Rider 打开解决方案，选择 iOS 目标编译运行。

## 许可证

本项目使用 MIT 许可证。

## 贡献

欢迎提交 Issue 和 Pull Request 来改进项目。如有任何问题或建议，请通过 GitHub Issue 反馈。

---

**SCAssistant - 生存战争助手，让社区访问更便捷**
