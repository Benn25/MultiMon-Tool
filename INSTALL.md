# Installing Dual Monitor Tools 3

## Install

1. Run `DualMonitorTools-Setup.exe` (built into the `dist` folder, see below).
2. Choose your options and press **Install**. No administrator rights are needed: it installs just for you.
   - Program: `%LOCALAPPDATA%\Programs\Dual Monitor Tools`
   - Settings: `%APPDATA%\Dual Monitor Tools`
3. Coming from an older (portable) copy of DMT? Use **Bring your old settings** and pick its `DmtSettings.xml`.
   Your settings, magic words and wallpaper sources are copied across.

Running the setup again updates DMT and keeps your settings.

Numpad zones (Ctrl / Win / Alt + numeric keypad) are off on a fresh install:
turn them on in **Settings › Windows › Numpad zones**.

## Uninstall

**Windows Settings › Apps › Installed apps › Dual Monitor Tools › Uninstall**.
You can choose whether to keep your settings.

## Portable use

Rename `DualMonitorTools-Setup.exe` to `DMT.exe` and run it from any folder.
Its settings are then kept next to it.

## Building the installer

Needs the .NET 10 SDK. From the repository folder:

```
powershell -ExecutionPolicy Bypass -File .\build-release.ps1
```

This runs the unit tests and creates `dist\DualMonitorTools-Setup.exe`, a single self-contained file
(the PC it runs on doesn't need .NET installed).
