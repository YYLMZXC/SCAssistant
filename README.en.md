# SC Assistant

SCAssistant (Survivalcraft Assistant) provides convenient access to Survivalcraft game community sites and download management. The project offers two cross-platform implementations that share the same core business logic (MVVM + DI):

| Implementation | UI Framework | Renderer | Supported Platforms |
|---------------|-------------|----------|---------------------|
| `SCAssistant.AvaloniaApp` | [Avalonia UI](https://www.avaloniaui.net/) | Native Controls | Windows, Android, iOS |
| `SCAssistant.UnoApp` | [Uno Platform](https://platform.uno/) | Skia | Windows, macOS, Linux, Android, iOS |

## Key Features

- **Quick Navigation**: Built-in shortcuts to the homepage, [SCKey](https://www.sckey.net), and [SCWZ](https://scwz.top/) for quick access to Survivalcraft community resources.
- **Built-in Browser**: Cross-platform WebView, automatically mapped to each platform's native browser:
  - Windows: Edge WebView2
  - Android: Android WebView
  - iOS / macOS: WKWebView
- **Download Management**: Multi-task concurrent downloads, progress display, pause/cancel support.
- **Download History Management**: Automatically records downloaded files, supports viewing records, opening folders, and deleting records.
- **Settings Management**: Configurable homepage URL, search engine, download directory, max concurrent downloads, etc.
- **Cross-Platform Support**:
  - Avalonia: Windows desktop, Android APK, iOS app
  - Uno Platform: Windows, macOS, Linux desktop, Android APK, iOS app

## Technical Architecture

- **Architecture Pattern**: MVVM (CommunityToolkit.Mvvm with DI)
- **Runtime**: .NET 10
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
