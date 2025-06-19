

# SC 助手

SCAssistant 是一个用于与 Chromium 浏览器集成并管理下载任务的 Windows Forms 应用程序。该应用程序允许用户通过内嵌的浏览器界面进行浏览，并通过图形界面跟踪和管理下载文件。

## 主要功能

- 使用 Chromium 浏览器浏览网页。
- 自定义下载处理逻辑，支持下载前确认和下载进度更新。
- 下载记录管理界面，支持查看、打开文件夹和删除下载记录。
- 集成安装程序，支持用户通过 SetupForm 安装应用程序。

## 项目结构

- `MainForm.cs` 和 `MainForm.Designer.cs`: 主界面逻辑和 UI 设计。
- `DownloadHandler.cs`: 下载处理逻辑，实现下载确认和进度更新。
- `DownloadListForm.cs` 和 `DownloadListForm.Designer.cs`: 下载记录管理界面。
- `Setup/SetupForm.cs` 和 `SetupForm.Designer.cs`: 应用程序安装界面和逻辑。
- `Program.cs`: 应用程序入口点。
- `SCAssistant.csproj` 和 `Setup/Setup.csproj`: 项目配置文件。
- `res/` 和 `Setup/res/`: 包含应用程序图标和其他资源文件。

## 依赖项

本项目依赖于 [CefSharp](https://github.com/cefsharp/CefSharp)，用于嵌入 Chromium 浏览器到 Windows Forms 应用程序中。

## 如何运行

1. 确保你已安装 .NET Framework 4.7 或更高版本。
2. 下载并安装 CefSharp 的依赖项。
3. 打开项目目录并运行 `SCAssistant.sln`。
4. 编译并运行 `SCAssistant` 项目。

## 如何安装

1. 运行 Setup 文件夹中的 `SetupForm`。
2. 选择安装路径并点击“下一步”开始安装。
3. 安装完成后，可以在桌面或安装目录中启动应用程序。

## 事件与交互

- 当用户点击下载链接时，`DownloadHandler` 会触发 `OnDownloadCreated` 事件。
- 下载记录会自动保存在 `DownloadListForm` 中，并支持双击打开文件或右键菜单操作。
- 安装过程中，`SetupForm` 会提供进度条和状态更新以引导用户完成安装。

## 许可证

本项目使用 MIT 许可证，请参阅项目根目录中的 `LICENSE` 文件以了解详细信息。

## 贡献

欢迎贡献和改进！如果你发现任何问题或有改进建议，请提交 PR 或 issue。在贡献之前，请确保阅读并理解本项目的贡献指南。

## 联系

如果你有任何问题或需要帮助，请访问 [Gitee 项目页面](https://gitee.com/projects) 或通过电子邮件联系作者。

---

**SCAssistant - 让下载管理更简单**