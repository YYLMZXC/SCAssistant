# SC Assistant

SCAssistant (Survivalcraft Assistant) provides convenient access to Survivalcraft game community sites and download management. The project offers four implementations: Avalonia, Uno Platform and MAUI are cross-platform with MVVM architecture, while WindowsForms is a traditional desktop implementation:

| Implementation | UI Framework | Browser Engine | Runtime | Supported Platforms |
|---------------|-------------|---------------|---------|---------------------|
| `SCAssistant.AvaloniaApp` | [Avalonia UI](https://www.avaloniaui.net/) | Native WebView | .NET 10 | Windows, Android, iOS, macOS, Linux |
| `SCAssistant.UnoApp` | [Uno Platform](https://platform.uno/) | Uno WebView2 | .NET 10 | Windows, macOS, Linux, Android, iOS |
| `SCAssistant.MauiApp` | [.NET MAUI](https://dotnet.microsoft.com/apps/maui) + [Open-MAUI-Linux](https://github.com/open-maui/maui-linux) | Native WebView | .NET 10 | Windows, Android, iOS, macOS, Linux |
| `SCAssistant.WindowsForms` | Windows Forms | [CefSharp](https://cefsharp.github.io/) | .NET Framework 4.7.2 | Windows |

## Key Features

- **Quick Navigation**: Built-in shortcuts to the homepage, [SCKey](https://www.sckey.net), and [SCWZ](https://scwz.top/) for quick access to Survivalcraft community resources.
- **Built-in Browser**: WebView-based with different engines per implementation:
  - Avalonia: Edge WebView2 (Windows), Android WebView, WKWebView (iOS/macOS)
  - Uno Platform: Uno WebView2 (maps to platform-native WebView)
  - MAUI: Edge WebView2 (Windows), Android WebView, WKWebView (iOS/macOS), Linux WebView (Open-MAUI-Linux)
  - WindowsForms: CefSharp (Chromium Embedded Framework)
- **Download Management**: Multi-task concurrent downloads, progress display, pause/cancel support.
- **Download History Management**: Automatically records downloaded files, supports viewing records, opening folders, and deleting records.
- **Settings Management**: Configurable homepage URL, search engine, download directory, max concurrent downloads, etc.
- **Cross-Platform Support**:
  - Avalonia: Windows, Android, iOS, macOS, Linux
  - Uno Platform: Windows, macOS, Linux, Android, iOS
  - MAUI: Windows, Android, iOS, macOS, Linux
  - WindowsForms: Windows desktop only

## Technical Architecture

- **Architecture Pattern**: MVVM (CommunityToolkit.Mvvm with DI) for Avalonia & Uno; Code-behind for WindowsForms
- **Runtime**: .NET 10 (Avalonia, Uno); .NET Framework 4.7.2 (WindowsForms)
- **Serialization**: Newtonsoft.Json

### Avalonia Implementation

| Component | Version |
|-----------|---------|
| Avalonia UI | 12.1.0 |
| Fluent Theme | 12.1.0 |
| DI Container | Microsoft.Extensions.DependencyInjection 10.0.3 |

### Uno Platform Implementation (Single Project)

| Component | Version |
|-----------|---------|
| Uno Platform SDK | 6.6.42 |
| Uno Toolkit / ThemeService | Bundled with SDK |
| Renderer | Skia |
| DI Container | CommunityToolkit.Mvvm (built-in) |

### MAUI Implementation

| Component | Version |
|-----------|---------|
| .NET MAUI | 10.0 |
| Open-MAUI-Linux | 10.0.70.4 |
| DI Container | Microsoft.Extensions.DependencyInjection |

### WindowsForms Implementation

| Component | Version |
|-----------|---------|
| .NET Framework | 4.7.2 |
| CefSharp (Browser Engine) | 135.0.220 |
| Newtonsoft.Json | 13.0.3 |

## Project Structure

```
src/
├── SCAssistant.AvaloniaApp/                          # Avalonia UI implementation
│   ├── SCAssistant.AvaloniaApp.slnx                  # Solution file
│   ├── Directory.Packages.props                      # Central package version management
│   ├── SCAssistant.AvaloniaApp/                      # Shared project
│   │   ├── App.axaml / App.axaml.cs                  # App entry & DI configuration
│   │   ├── ViewModels/                               # MVVM ViewModel layer
│   │   │   ├── ViewModelBase.cs                      # Base class
│   │   │   ├── MainViewModel.cs                      # Main page logic
│   │   │   ├── DownloadListViewModel.cs              # Download list logic
│   │   │   └── SettingsViewModel.cs                  # Settings panel logic
│   │   ├── Views/                                    # View layer
│   │   │   ├── MainWindow.axaml                      # Desktop main window
│   │   │   ├── MainView.axaml                        # Mobile main view
│   │   │   ├── HomeView.axaml                        # Home/welcome page
│   │   │   └── SettingsView.axaml                    # Settings panel
│   │   ├── Models/                                   # Data models
│   │   ├── Services/                                 # Service layer (browser, download, settings, history)
│   │   └── Converters/                               # Value converters
│   ├── SCAssistant.AvaloniaApp.Desktop/              # Desktop platform project
│   ├── SCAssistant.AvaloniaApp.Android/              # Android platform project
│   └── SCAssistant.AvaloniaApp.iOS/                  # iOS platform project
│
└── SCAssistant.UnoApp/                               # Uno Platform implementation
    ├── SCAssistant.UnoApp.slnx                       # Solution file
    ├── global.json                                   # Uno SDK version declaration
    └── SCAssistant.UnoApp/                           # Single project (multi-platform)
        ├── SCAssistant.UnoApp.csproj                 # Project file (TargetFrameworks: Android/iOS/Desktop)
        ├── App.xaml / App.xaml.cs                    # App entry & DI configuration
        ├── ViewModels/                               # MVVM ViewModel layer
        ├── Views/                                    # View layer
        │   ├── MainPage.xaml                         # Main page
        │   ├── DownloadListPanel.xaml                # Download list panel
        │   └── SettingsPanel.xaml                    # Settings panel
        ├── Models/                                   # Data models
        ├── Services/                                 # Service layer
        │   ├── AppPaths.cs                           # Central data-directory management (config/Bugs/Downloads/...)
        │   ├── LogHelper.cs                          # Logging (Console/Debug/file output)
        │   ├── BrowserProvider.cs                    # Browser wrapper (download interception, UA settings)
        │   ├── DownloadService.cs                    # Multi-task concurrent download service
        │   ├── DownloadHistoryService.cs             # Download history persistence
        │   ├── SettingsService.cs                    # Settings persistence (config/settings.json)
        │   └── ServiceLocator.cs                     # Service locator
        ├── Converters/                               # Value converters
        ├── Assets/                                   # Icons & splash screen
        └── Platforms/                                # Platform entry points
            ├── Android/                              # Android (MainActivity, Manifest)
            ├── Desktop/                              # Desktop (Win32, X11, macOS)
            └── iOS/                                  # iOS (Info.plist, Entitlements)

├── SCAssistant.MauiApp/                               # .NET MAUI implementation
│   ├── SCAssistant.MauiApp.slnx                       # Solution file
│   ├── SCAssistant.MauiApp/                          # Shared project
│   │   ├── App.xaml / App.xaml.cs                    # App entry & DI configuration
│   │   ├── AppShell.xaml / AppShell.xaml.cs          # Shell navigation
│   │   ├── MainPage.xaml / MainPage.xaml.cs          # Main page
│   │   ├── ViewModels/                               # MVVM ViewModel layer
│   │   ├── Views/                                    # View layer
│   │   ├── Models/                                   # Data models
│   │   ├── Services/                                 # Service layer (browser, download, settings, history)
│   │   └── Converters/                               # Value converters
│   ├── SCAssistant.MauiApp.WinUI/                    # Windows platform project
│   ├── SCAssistant.MauiApp.Droid/                    # Android platform project
│   ├── SCAssistant.MauiApp.iOS/                      # iOS platform project
│   ├── SCAssistant.MauiApp.Mac/                      # macOS platform project
│   └── SCAssistant.MauiApp.Linux/                    # Linux platform project (Open-MAUI-Linux)

└── SCAssistant.WindowsForms/                         # Windows Forms + CefSharp implementation
    ├── SCAssistant.WindowsForms.sln                  # Solution file
    ├── SCAssistant.WindowsForms/                     # Project directory
    │   ├── SCAssistant.WindowsForms.csproj           # Project file (.NET Framework 4.7.2)
    │   ├── Program.cs                                # Application entry
    │   ├── MainForm.cs / MainForm.Designer.cs        # Main form (embedded CefSharp browser)
    │   ├── DownloadListForm.cs                       # Download list form
    │   ├── DownloadHandler.cs                        # CefSharp download handler
    │   ├── DownloadRecord.cs                         # Download record data model
    │   ├── ContextMenuHandler.cs                     # Custom context menu handler
    │   ├── CustomLifeSpanHandler.cs                  # Life span handler
    │   └── Properties/                               # Assembly info & resources
    └── packages/                                     # Local NuGet packages
```

## How to Run

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- Windows desktop: No additional browser runtime required
- Android: Requires Android SDK and related build tools
- iOS / macOS: Requires macOS with Xcode for building

### Avalonia

**Desktop (Windows)**

```bash
dotnet run --project src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Desktop
```

Or open `src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.slnx` directly in Visual Studio / Rider and run.

**Android**

```bash
dotnet build src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Android -c Release
```

The generated APK will be located at `src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Android/bin/Release/net10.0-android/`.

**iOS**

On macOS, open the solution in Visual Studio for Mac or Rider and build & run with the iOS target.

### Uno Platform

**Desktop (Windows / macOS / Linux)**

```bash
dotnet run --project src/SCAssistant.UnoApp/SCAssistant.UnoApp/SCAssistant.UnoApp.csproj
```

Or open `src/SCAssistant.UnoApp/SCAssistant.UnoApp.slnx` in Visual Studio / Rider and select the desired desktop target.

**Android**

```bash
dotnet build src/SCAssistant.UnoApp/SCAssistant.UnoApp/SCAssistant.UnoApp.csproj -c Release -f net10.0-android
```

**iOS**

On macOS, open the solution and build & run with the iOS target.

### MAUI

**Windows Desktop**

```bash
dotnet run --project src/SCAssistant.MauiApp/SCAssistant.MauiApp.WinUI
```

**Linux Desktop**

Install system dependencies first (Ubuntu/Debian):

```bash
sudo apt install libx11-dev libxrandr-dev libxcursor-dev libxi-dev libgl1-mesa-dev libfontconfig1-dev
```

Then run:

```bash
dotnet run --project src/SCAssistant.MauiApp/SCAssistant.MauiApp.Linux
```

**Android**

```bash
dotnet build src/SCAssistant.MauiApp/SCAssistant.MauiApp.Droid -c Release
```

**iOS / macOS**

On macOS, open `src/SCAssistant.MauiApp/SCAssistant.MauiApp.slnx` and build & run with the iOS or Mac target.

## Data Directory & Logs

The Uno Platform implementation (currently the primary maintained implementation) stores all application data under a "software directory", with each feature in its own folder:

```
Software directory/
├── config/            ← Configuration file (settings.json)
├── Bugs/              ← Log files (app_yyyy-MM-dd.log)
├── Downloads/         ← Downloaded files
├── DownloadHistory/   ← Download history (download_history.json)
└── WebView2/          ← Browser data (cookies, cache, etc.)
```

Software directory location:

| Platform | Location |
|----------|----------|
| Windows / macOS / Linux | Program directory (portable, data travels with the exe); falls back to `%LocalAppData%/SCAssistant` when the program directory is not writable |
| Android | App-specific external storage `Android/data/com.companyname.scassistant.yylmzxc001/files/` (accessible via file manager); falls back to internal storage |

> Note: After upgrading to the new directory layout, `settings.json` and `download_history.json` from the old location (`%LocalAppData%/SCAssistant/`) are migrated automatically.

### Logging

- Log files are stored at `Software directory/Bugs/app_yyyy-MM-dd.log`, rotated daily.
- Logs are written to: log file, console window, and IDE Debug output.
- At startup, the app logs: software directory, log directory, platform, and app version.
- Global unhandled exception logging is registered (`AppDomain.UnhandledException`, `TaskScheduler.UnobservedTaskException`), so any crash is recorded in the `Bugs` folder for easier diagnosis.
- When reporting a bug, please attach the current day's log file from the `Bugs` folder.

## FAQ

### Why does Android always show a "copied to clipboard" toast?

This is a **system-level privacy notification from Android 13+**, not a bug, and it cannot be disabled from the app. The trigger is the web page loaded inside the built-in browser: when page JS calls the clipboard API (e.g. clicking a "copy" button on the page, copying after long-press selection, or auto-copy), the system shows the toast. Opening the same page in the system Chrome and clicking the same button produces the same toast, confirming it's unrelated to this app.

### Rider reports "Unknown run configuration type XamarinAndroidProject"?

After the project migrated from Xamarin to Uno/.NET, Rider's local `.idea` configuration still contains stale run configurations. Delete the local `.idea` directory and reload the project (`File` → `Reload Project`); Rider will regenerate a `.NET Android` run configuration from the `net10.0-android` target. The `.idea` directory is already ignored by `.gitignore`, so deleting it does not affect the repository.

### Android build fails with `XAPRAS7009` (missing RuntimeIdentifier metadata)?

This was caused by `TreatAsLocalProperty` incorrectly ignoring the singular `RuntimeIdentifier` property. The current `SCAssistant.UnoApp.csproj` only ignores the plural `RuntimeIdentifiers` and keeps the singular `RuntimeIdentifier` to support Android multi-ABI (arm64/x64) builds. If it reappears, check whether the csproj accidentally added `TreatAsLocalProperty="RuntimeIdentifier;..."`.

## License

This project is licensed under the MIT License.

## Contributing

Issues and Pull Requests are welcome! If you have any questions or suggestions, please submit them via GitHub Issues.

---

**SCAssistant - Simplify your Survivalcraft community access**
