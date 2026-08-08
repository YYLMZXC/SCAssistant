

# SC Assistant

SCAssistant is a Windows Forms application designed to integrate with the Chromium browser and manage download tasks. The application allows users to browse the web through an embedded browser interface, and track and manage downloaded files using a graphical user interface.

## Key Features

- Browse web pages using the Chromium browser.
- Customizable download handling logic, supporting download confirmation before starting and progress updates.
- Download history management interface, supporting viewing, opening folder locations, and deleting download records.
- Integrated installation program, allowing users to install the application via SetupForm.

## Project Structure

- `MainForm.cs` and `MainForm.Designer.cs`: Main interface logic and UI design.
- `DownloadHandler.cs`: Download handling logic, implementing download confirmation and progress updates.
- `DownloadListForm.cs` and `DownloadListForm.Designer.cs`: Interface for managing download history.
- `Setup/SetupForm.cs` and `SetupForm.Designer.cs`: Installation interface and logic for the application.
- `Program.cs`: Application entry point.
- `SCAssistant.csproj` and `Setup/Setup.csproj`: Project configuration files.
- `res/` and `Setup/res/`: Contain application icons and other resource files.

## Dependencies

This project depends on [CefSharp](https://github.com/cefsharp/CefSharp), used to embed the Chromium browser into the Windows Forms application.

## How to Run

1. Ensure you have installed .NET Framework 4.7 or later.
2. Download and install the CefSharp dependencies.
3. Open the project directory and run `SCAssistant.sln`.
4. Build and run the `SCAssistant` project.

## How to Install

1. Run `SetupForm` located in the Setup folder.
2. Select the installation path and click "Next" to begin installation.
3. After installation completes, launch the application from the desktop shortcut or the installation directory.

## Events and Interactions

- When a user clicks a download link, the `DownloadHandler` triggers the `OnDownloadCreated` event.
- Download records are automatically saved in the `DownloadListForm`, supporting double-click to open files or right-click menu operations.
- During installation, the `SetupForm` provides a progress bar and status updates to guide the user through the installation process.

## License

This project uses the MIT License. Please refer to the `LICENSE` file in the project root directory for more details.

## Contributions

Contributions and improvements are welcome! If you find any issues or have suggestions for enhancements, please submit a PR or open an issue. Before contributing, please read and understand the project's contribution guidelines.

## Contact

If you have any questions or need assistance, please visit the [Gitee Project Page](https://gitee.com/projects) or contact the author via email.

---

**SCAssistant - Simplified Download Management**