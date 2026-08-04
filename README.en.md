# SC Assistant

SCAssistant is a **cross-platform** application built with [Uno Platform](https://platform.uno/), providing convenient access to Survivalcraft game community sites and download management. A single codebase targets Windows desktop, Android, and iOS.

## Key Features

- **Quick Navigation**: Built-in shortcuts to the homepage, [SCKey](https://www.sckey.net), and [SCWZ](https://scwz.top/) for quick access to Survivalcraft community resources.
- **Built-in Browser**: Based on Uno Platform's cross-platform WebView2, automatically mapped to each platform's native browser:
  - Windows: Edge WebView2
  - Android: Android WebView
  - iOS: WKWebView
  - Desktop Skia: falls back to the system default browser
- **Download History Management**: Automatically records downloaded files, supports viewing records, opening folders, and deleting records.
- **Cross-Platform Support**: A single project (Uno Single Project) compiled into Windows desktop apps, Android APKs, and iOS apps.

## Technical Architecture

- **UI Framework**: Uno Platform (Uno.Sdk, SkiaRenderer + WinUI style)
- **Runtime**: .NET 10
- **Architecture Pattern**: MVVM (CommunityToolkit.Mvvm with IOC)
- **Dependency Injection**: Microsoft.Extensions.DependencyInjection
- **Serialization**: Newtonsoft.Json

## Project Structure

```
src/SCAssistant.UnoApp/
├── SCAssistant.UnoApp.slnx           # Solution file
├── global.json                       # .NET SDK version pinning
├── nuget.config                      # NuGet source configuration
└── SCAssistant.UnoApp/               # Single project (Uno Single Project)
    ├── App.xaml / App.xaml.cs        # Application entry point & DI configuration
    ├── ViewModels/                   # MVVM ViewModel layer
    │   ├── ViewModelBase.cs          # Base class
    │   ├── MainViewModel.cs          # Main page logic (navigation, browser state)
    │   └── DownloadListViewModel.cs  # Download list logic
    ├── Views/                        # View layer
    │   ├── MainPage.xaml             # Main page (toolbar + browser host + download list overlay)
    │   └── DownloadListPanel.xaml    # Download list popup panel
    ├── Models/                       # Data models
    │   └── DownloadRecord.cs         # Download record
    ├── Services/                     # Service layer
    │   ├── IBrowserProvider.cs       # Browser provider interface
    │   ├── BrowserProvider.cs        # Cross-platform WebView2 implementation
    │   ├── SystemBrowserProvider.cs  # System browser fallback
    │   ├── IDownloadHistoryService.cs # Download history interface
    │   ├── DownloadHistoryService.cs  # Download history implementation
    │   └── ServiceLocator.cs          # Service locator (static access)
    ├── Converters/                   # XAML value converters
    ├── Platforms/                    # Platform-specific entry points & config
    │   ├── Desktop/Program.cs        # Desktop entry point
    │   ├── Android/                  # Android entry point & manifest
    │   └── iOS/                      # iOS entry point & config
    └── Assets/                       # App resources
```

## Dependencies

| Package | Version | Description |
|---------|---------|-------------|
| Uno.Sdk | - | Uno Platform SDK (single project build, version pinned by global.json) |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM toolkit |
| Newtonsoft.Json | 13.0.3 | JSON serialization |

## How to Run

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows desktop**: No additional browser runtime required (uses the system Edge WebView2)
- **Android**: Requires Android SDK and related build tools
- **iOS**: Requires macOS with Xcode for building

### Desktop (Windows)

```bash
dotnet run --project src/SCAssistant.UnoApp/SCAssistant.UnoApp -f net10.0-desktop
```

Or open `src/SCAssistant.UnoApp/SCAssistant.UnoApp.slnx` directly in Visual Studio / Rider and run.

### Android

```bash
dotnet build src/SCAssistant.UnoApp/SCAssistant.UnoApp -f net10.0-android -c Release
```

The generated APK will be located at `src/SCAssistant.UnoApp/SCAssistant.UnoApp/bin/Release/net10.0-android/`.

### iOS

On macOS, open the solution in Visual Studio for Mac or Rider and build & run with the iOS target.

## License

This project is licensed under the MIT License.

## Contributing

Issues and Pull Requests are welcome! If you have any questions or suggestions, please submit them via GitHub Issues.

---

**SCAssistant - Simplify your Survivalcraft community access**
