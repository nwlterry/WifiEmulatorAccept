# Wi-Fi Emulator Accept

Windows tray program. When Wi-Fi reconnects, it clicks Allow or Accept on the emulator prompt (QEMU, Android Emulator, BlueStacks, LDPlayer, Nox, MEmu, MuMu, Google Play Games).

It is a Windows-only .NET program. It needs the .NET 10 Windows Desktop Runtime.

The window and tray show whether Wi-Fi is connected and the network name. Closing the window leaves the program running in the tray. Exit is on the tray menu. Help and F1 open Help.html.

## Install

Release 1.0.0 includes two packages:

- `WifiEmulatorAccept-Setup.exe` is the Windows setup wizard. It installs for the current user to `%LOCALAPPDATA%\Programs\WifiEmulatorAccept`, creates a Start menu shortcut, and can create a desktop shortcut. Both shortcuts use the Wi-Fi icon. The setup page shows the MIT license.
- `WifiEmulatorAccept-1.0.0-win-x64.zip` is the same program plus `Install.cmd`. Unpack the zip and run `Install.cmd`.

Uninstall from Windows Settings, under Apps. The zip also includes `Uninstall.ps1`.

Settings and the log are in `%APPDATA%\WifiEmulatorAccept`.

## License

MIT. See [LICENSE](LICENSE). Copyright (c) 2026 nwlterry.
