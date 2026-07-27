
# SC Assistant

SCAssistant is a **cross-platform** application built with [Avalonia UI](https://avaloniaui.net/), providing convenient access to Survivalcraft game community sites and download management. It supports Windows, Android, and iOS platforms.

## Key Features

- **Quick Navigation**: Built-in shortcuts to the homepage, [SCKey](https://www.sckey.net), and [SCWZ](https://scwz.top/) for quick access to Survivalcraft community resources.
- **Built-in Browser**: Uses native browser engines on each platform for a smooth browsing experience:
  - Windows: WebView2 (Edge Chromium)
  - Android: Native System WebView
  - iOS: WKWebView
- **Download History Management**: Automatically records downloaded files, supports viewing, opening folders, and deleting records.
- **Cross-Platform Support**: A single codebase compiled into Windows desktop apps, Android APKs, and iOS apps.

## Technical Architecture

- **UI Framework**: Avalonia UI 12 + Fluent Theme
- **Runtime**: .NET 10
- **Architecture Pattern**: MVVM (using CommunityToolkit.Mvvm)
- **Serialization**: Newtonsoft.Json

## Project Structure

```
src/SCAssistant.AvaloniaApp/
├── SCAssistant.AvaloniaApp/               # Shared core project (net10.0)
│   ├── App.axaml / App.axaml.cs           # Application entry point
│   ├── ViewModels/                        # MVVM ViewModel layer
│   │   ├── ViewModelBase.cs               # Base class
│   │   ├── MainViewModel.cs               # Main page logic
│   │   └── DownloadListViewModel.cs       # Download list logic
│   ├── Views/                             # MVVM View layer
│   │   ├── MainWindow.axaml               # Desktop main window
│   │   ├── MainView.axaml                 # Main user control
│   │   └── DownloadListWindow.axaml       # Download list flyout panel
│   ├── Models/                            # Data models
│   │   └── DownloadRecord.cs              # Download record
│   └── Services/                          # Service layer
│       ├── IBrowserProvider.cs            # Browser provider interface
│       ├── IDownloadHistoryService.cs     # Download history interface
│       ├── DownloadHistoryService.cs      # Download history implementation
│       ├── ServiceLocator.cs              # Service locator
│       ├── PlaceholderBrowserProvider.cs  # Placeholder browser (fallback)
│       └── SystemBrowserProvider.cs       # System browser fallback
├── SCAssistant.AvaloniaApp.Desktop/       # Desktop project (net10.0-windows)
│   ├── Program.cs                         # Desktop entry point
│   └── Services/WebView2BrowserProvider.cs # WebView2 browser implementation
├── SCAssistant.AvaloniaApp.Android/       # Android project (net10.0-android)
│   ├── MainActivity.cs                    # Android main activity
│   └── Services/AndroidBrowserProvider.cs  # Android native WebView implementation
├── SCAssistant.AvaloniaApp.iOS/           # iOS project (net10.0-ios)
│   ├── AppDelegate.cs                     # iOS app delegate
│   └── Services/iOSBrowserProvider.cs      # iOS WKWebView implementation
├── SCAssistant.AvaloniaApp.slnx           # Solution file
└── Directory.Packages.props               # Central package version management
```

## Dependencies

| Package | Version | Description |
|---------|---------|-------------|
| Avalonia | 12.1.0 | Avalonia UI core framework |
| Avalonia.Themes.Fluent | 12.1.0 | Fluent design theme |
| Avalonia.Fonts.Inter | 12.1.0 | Inter font family |
| CommunityToolkit.Mvvm | 8.4.2 | MVVM toolkit |
| Newtonsoft.Json | 13.0.3 | JSON serialization |
| Avalonia.Desktop | 12.1.0 | Desktop platform support |
| Avalonia.Controls.WebView | 12.0.1 | WebView2 browser control |
| Avalonia.Android | 12.1.0 | Android platform support |
| Avalonia.iOS | 12.1.0 | iOS platform support |

## How to Run

### Requirements

- [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0)
- **Windows**: Requires [WebView2 Runtime](https://developer.microsoft.com/microsoft-edge/webview2/) (pre-installed on Windows 11)
- **Android**: Requires Android SDK and related build tools
- **iOS**: Requires macOS with Xcode for building

### Desktop (Windows)

```bash
cd src/SCAssistant.AvaloniaApp
dotnet run --project SCAssistant.AvaloniaApp.Desktop
```

Or open `SCAssistant.AvaloniaApp.slnx` directly in Visual Studio / Rider and run the Desktop project.

### Android

```bash
cd src/SCAssistant.AvaloniaApp
dotnet build SCAssistant.AvaloniaApp.Android -c Release
```

The generated APK will be located at `SCAssistant.AvaloniaApp.Android/bin/Release/net10.0-android/`.

### iOS

On macOS, open the solution in Visual Studio for Mac or Rider, select the iOS project, and build & run.

## License

This project is licensed under the MIT License.

## Contributing

Issues and Pull Requests are welcome! If you have any questions or suggestions, please submit them via GitHub Issues.

---

**SCAssistant - Simplify your Survivalcraft community access**
