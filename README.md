# BrightnessSwitch

[![Total downloads](https://img.shields.io/github/downloads/stephtr/BrightnessSwitch/total.svg)](https://github.com/stephtr/BrightnessSwitch/releases) _- hopefully increasing soon 😉_

_Do you have a notebook with Fn and function keys combined and have issues with combinations like <kbd>Alt</kbd>+<kbd>F4</kbd>? Make sure to check out [FixSurfaceKeyboard](https://github.com/stephtr/FixSurfaceKeyboard)._

---

This small app automatically switches between Light- and Dark-Theme, depending on the illuminance detected via your device's light sensor.

![Screenshot](screenshot.png)

In order to control the switching mechanism, it adds a tray icon where one can enable or disable automatic switching of themes, but also an option to manually switch the theme.

If automatic switching is enabled and one switches the theme, machine learning (support vector machine, to be precise) is being used for optimizing the automatic switching mechanism.

## Download

- .zip package: [Latest release](https://github.com/stephtr/BrightnessSwitch/releases)
- Installer: coming soon...
- Microsoft Store: under consideration

## Running the app

The app is written in C# (.NET 5.0) and therefore needs the [Desktop Runtime 5.0](https://dotnet.microsoft.com/download/dotnet/5.0#runtime-desktop-5.0.0-preview.2) to be installed.

For this app there is currently no installer available, therefore just download the [latest release](https://github.com/stephtr/BrightnessSwitch/releases) and extract it to a folder of your choice. Then run `BrightnessControl.exe`, autostart with Windows can be enabled via the context menu.

## Building from source

For building the app from scratch, you need to have the [.NET SDK 5.0 (preview)](https://dotnet.microsoft.com/download/dotnet/5.0) installed.

After cloning or downloading the source, running `dotnet run` is sufficient for automatically restoring, building and running the app.

## Changelog

For a list of recent changes, see the separate [Changelog](changelog.md).
