# SC Assistant

SCAssistant (Survivalcraft Assistant) provides convenient access to Survivalcraft game community sites and download management. The project offers three implementations: Avalonia and Uno Platform are cross-platform with MVVM architecture, while WindowsForms is a traditional desktop implementation:

| Implementation | UI Framework | Browser Engine | Runtime | Supported Platforms |
|---------------|-------------|---------------|---------|---------------------|
| `SCAssistant.AvaloniaApp` | [Avalonia UI](https://www.avaloniaui.net/) | Native WebView | .NET 10 | Windows, Android, iOS, macOS, Linux |
| `SCAssistant.UnoApp` | [Uno Platform](https://platform.uno/) | Uno WebView2 | .NET 10 | Windows, macOS, Linux, Android, iOS |
| `SCAssistant.WindowsForms` | Windows Forms | [CefSharp](https://cefsharp.github.io/) | .NET Framework 4.7.2 | Windows |

## Key Features

- **Quick Navigation**: Built-in shortcuts to the homepage, [SCKey](https://www.sckey.net), and [SCWZ](https://scwz.top/) for quick access to Survivalcraft community resources.
- **Built-in Browser**: WebView-based with different engines per implementation:
  - Avalonia: Edge WebView2 (Windows), Android WebView, WKWebView (iOS/macOS)
  - Uno Platform: Uno WebView2 (maps to platform-native WebView)
  - WindowsForms: CefSharp (Chromium Embedded Framework)
- **Download Management**: Multi-task concurrent downloads, progress display, pause/cancel support.
- **Download History Management**: Automatically records downloaded files, supports viewing records, opening folders, and deleting records.
- **Settings Management**: Configurable homepage URL, search engine, download directory, max concurrent downloads, etc.
- **Cross-Platform Support**:
  - Avalonia: Windows, Android, iOS, macOS, Linux
  - Uno Platform: Windows, macOS, Linux, Android, iOS
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
        ├── Converters/                               # Value converters
        ├── Assets/                                   # Icons & splash screen
        └── Platforms/                                # Platform entry points
            ├── Android/                              # Android (MainActivity, Manifest)
            ├── Desktop/                              # Desktop (Win32, X11, macOS)
            └── iOS/                                  # iOS (Info.plist, Entitlements)
│
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

## License

This project is licensed under the MIT License.

## Contributing

Issues and Pull Requests are welcome! If you have any questions or suggestions, please submit them via GitHub Issues.

---

**SCAssistant - Simplify your Survivalcraft community access**
