# SC Assistant

SCAssistant is a **cross-platform** application built with [Avalonia UI](https://www.avaloniaui.net/), providing convenient access to Survivalcraft game community sites and download management. A single codebase targets Windows desktop, Android, and iOS.

## Key Features

- **Quick Navigation**: Built-in shortcuts to the homepage, [SCKey](https://www.sckey.net), and [SCWZ](https://scwz.top/) for quick access to Survivalcraft community resources.
- **Built-in Browser**: Based on Avalonia's cross-platform WebView, automatically mapped to each platform's native browser:
  - Windows: Edge WebView2
  - Android: Android WebView
  - iOS: WKWebView
- **Download Management**: Multi-task concurrent downloads, progress display, pause/cancel support.
- **Download History Management**: Automatically records downloaded files, supports viewing records, opening folders, and deleting records.
- **Settings Management**: Configurable homepage URL, search engine, download directory, max concurrent downloads, etc.
- **Cross-Platform Support**: Compiled into Windows desktop apps, Android APKs, and iOS apps.

## Technical Architecture

- **UI Framework**: Avalonia UI (Avalonia 12.x, Fluent Theme)
- **Runtime**: .NET 10
- **Architecture Pattern**: MVVM (CommunityToolkit.Mvvm with IOC)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Serialization**: Newtonsoft.Json

## Project Structure

```
src/SCAssistant.AvaloniaApp/
├── SCAssistant.AvaloniaApp.slnx           # Solution file
├── Directory.Packages.props               # Central package version management
├── SCAssistant.AvaloniaApp/               # Shared project
│   ├── App.axaml / App.axaml.cs           # Application entry point & DI configuration
│   ├── ViewModels/                        # MVVM ViewModel layer
│   │   ├── ViewModelBase.cs               # Base class
│   │   ├── MainViewModel.cs               # Main page logic (navigation, browser state, download)
│   │   ├── DownloadListViewModel.cs        # Download list logic
│   │   └── SettingsViewModel.cs           # Settings panel logic
│   ├── Views/                             # View layer
│   │   ├── MainWindow.axaml               # Desktop main window (toolbar + WebView + overlays)
│   │   ├── MainView.axaml                 # Mobile main view
│   │   ├── HomeView.axaml                 # Home/welcome page
│   │   └── SettingsView.axaml             # Settings panel
│   ├── Models/                            # Data models
│   │   ├── DownloadRecord.cs              # Download record
│   │   └── AppSettings.cs                 # Application settings
│   ├── Services/                          # Service layer
│   │   ├── IBrowserProvider.cs            # Browser provider interface
│   │   ├── BrowserProvider.cs             # Cross-platform WebView implementation
│   │   ├── SystemBrowserProvider.cs        # System browser fallback
│   │   ├── IDownloadService.cs            # Download service interface
│   │   ├── DownloadService.cs             # Download service implementation
│   │   ├── IDownloadHistoryService.cs      # Download history interface
│   │   ├── DownloadHistoryService.cs       # Download history implementation
│   │   ├── ISettingsService.cs            # Settings service interface
│   │   ├── SettingsService.cs             # Settings service implementation
│   │   ├── LogHelper.cs                   # Logging helper
│   │   └── ServiceLocator.cs              # Service locator (static access)
│   └── Converters/                        # Value converters
│       └── Converters.cs                  # XAML binding converters
├── SCAssistant.AvaloniaApp.Desktop/        # Desktop platform project
├── SCAssistant.AvaloniaApp.Android/        # Android platform project
│   ├── MainActivity.cs
│   └── Properties/AndroidManifest.xml
└── SCAssistant.AvaloniaApp.iOS/            # iOS platform project
    ├── AppDelegate.cs
    ├── Main.cs
    └── Info.plist
```

## Dependencies

| Package | Version | Description |
|---------|---------|-------------|
| Avalonia | 12.1.0 | Avalonia UI framework |
| Avalonia.Themes.Fluent | 12.1.0 | Fluent design theme |
| Avalonia.Fonts.Inter | 12.1.0 | Inter font |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM toolkit |
| Microsoft.Extensions.DependencyInjection | 10.0.3 | DI container |
| Newtonsoft.Json | 13.0.4 | JSON serialization |

## How to Run

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows desktop**: No additional browser runtime required
- **Android**: Requires Android SDK and related build tools
- **iOS**: Requires macOS with Xcode for building

### Desktop (Windows)

```bash
dotnet run --project src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Desktop
```

Or open `src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.slnx` directly in Visual Studio / Rider and run.

### Android

```bash
dotnet build src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Android -c Release
```

The generated APK will be located at `src/SCAssistant.AvaloniaApp/SCAssistant.AvaloniaApp.Android/bin/Release/net10.0-android/`.

### iOS

On macOS, open the solution in Visual Studio for Mac or Rider and build & run with the iOS target.

## License

This project is licensed under the MIT License.

## Contributing

Issues and Pull Requests are welcome! If you have any questions or suggestions, please submit them via GitHub Issues.

---

**SCAssistant - Simplify your Survivalcraft community access**
