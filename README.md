# Wi-Fi Emulator Accept

Pure .NET program for Windows only. When Wi-Fi reconnects, it clicks Allow or Accept on the Windows emulator prompt (QEMU, BlueStacks, LDPlayer, Nox, MEmu, MuMu, Google Play Games).

The window title and the line under the introduction name the runtime for that copy. Closing the window leaves the program running in the tray. Exit is on the tray menu. Help and F1 open Help.html.

## Packages

Release 1.1.0 publishes two Windows packages. Install the one that matches the Windows Desktop Runtime on the PC.

| Runtime | Setup wizard | Zip |
| --- | --- | --- |
| .NET 8 Windows Desktop | `WifiEmulatorAccept-Setup-net8.exe` | `WifiEmulatorAccept-1.1.0-net8.0-win-x64.zip` |
| .NET 10 Windows Desktop | `WifiEmulatorAccept-Setup-net10.exe` | `WifiEmulatorAccept-1.1.0-net10.0-win-x64.zip` |

Each setup wizard installs for the current user to `%LOCALAPPDATA%\Programs\WifiEmulatorAccept`, shows the MIT license, and creates shortcuts that use the Wi-Fi icon. The installed name includes the runtime, for example Wi-Fi Emulator Accept (.NET 10). The zip for the same runtime contains `Install.cmd`.

Uninstall from Windows Settings, under Apps. Each zip also includes `Uninstall.ps1`.

Settings and the log are in `%APPDATA%\WifiEmulatorAccept`.

## License

MIT. See [LICENSE](LICENSE). Copyright (c) 2026 nwlterry.
