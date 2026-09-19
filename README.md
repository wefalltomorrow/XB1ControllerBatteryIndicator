# Xbox One Controller Battery Indicator

[![Windows build](https://github.com/wefalltomorrow/XB1ControllerBatteryIndicator/actions/workflows/windows-build.yml/badge.svg)](https://github.com/wefalltomorrow/XB1ControllerBatteryIndicator/actions/workflows/windows-build.yml)
[![Latest release](https://img.shields.io/github/v/release/wefalltomorrow/XB1ControllerBatteryIndicator)](https://github.com/wefalltomorrow/XB1ControllerBatteryIndicator/releases/latest)
[![License: GPL v2](https://img.shields.io/badge/license-GPL%20v2-blue.svg)](LICENSE)

A lightweight Windows tray application that shows the battery state of XInput-compatible Xbox controllers and warns when a battery is nearly empty.

This repository is a maintained fork of [NiyaShy/XB1ControllerBatteryIndicator](https://github.com/NiyaShy/XB1ControllerBatteryIndicator), focused on keeping the original small tray utility reliable on current Windows versions without turning it into a large background application.

## Download

**[Download the latest release](https://github.com/wefalltomorrow/XB1ControllerBatteryIndicator/releases/latest).**

The portable ZIP is the recommended download. Extract it anywhere and run `XB1ControllerBatteryIndicator.exe`.

### Requirements

- Windows 10 or Windows 11
- .NET Framework 4.8
- An XInput-compatible controller
- For reliable Xbox controller battery reporting, an Xbox Wireless Adapter or another dongle using Microsoft's proprietary Xbox wireless protocol

No .NET 10 runtime is required.

## Features

- Lightweight system-tray battery indicator
- Supports up to four XInput controllers
- Polls all controller slots every second
- Rotates the visible controller every five seconds when multiple controllers are connected
- Empty, low, medium and full battery states
- Wired and waiting-for-data states
- Low-battery Windows toast notifications
- Optional built-in Windows low-battery warning sound and looping warning sound
- Automatic light/dark tray icon handling
- Optional startup with Windows
- Built-in update checking against this fork

XInput exposes coarse battery levels rather than a precise percentage, so this application intentionally reports **Empty / Low / Medium / Full** rather than inventing a percentage.

## What's improved in this fork

Version 1.3.2.1 consolidates the useful maintenance work from newer forks while preserving the original behaviour:

- Removed the abandoned SharpDX and SharpDX.XInput dependencies
- Calls Windows `xinput1_4.dll` directly through P/Invoke
- Polls controller state every second without breaking five-second multi-controller display rotation
- Prevents persistent polling errors from creating a high-CPU retry loop
- Preserves the connected-but-waiting-for-battery-data state
- Prevents false low-battery alerts for wired or still-initializing controllers
- Tracks low-battery sound state independently for each controller
- Uses the Windows Exclamation system sound for battery warnings instead of requiring a custom WAV file
- Caches the Windows theme value instead of reading the registry on every icon refresh
- Keeps the theme watcher alive for reliable theme changes
- Removes redundant translation-resource rescans
- Removes the old Microsoft Windows API Code Pack dependency by inlining the tiny shell interop pieces actually used
- Retargeted from obsolete .NET Framework 4.5.2 to .NET Framework 4.8
- Uses HTTPS/TLS 1.2 for update checks and points them to this fork
- Adds reproducible Windows CI builds and automated GitHub releases

See [CHANGELOG.md](CHANGELOG.md) for the release history.

## Controller compatibility

The original project is known to work with XInput controllers including:

- Xbox 360
- Xbox One model 1537 / 1697
- Xbox One Elite model 1698
- Xbox One S model 1708
- Xbox Elite Wireless Controller Series 2 model 1797
- Xbox Series X|S controller model 1914

Other XInput-compatible controllers may work as well.

## Bluetooth limitation

Xbox controllers connected through Bluetooth may report no battery information or an incorrect battery level through XInput. This is a Windows/XInput limitation inherited from the original project, not something the tray application can reliably correct.

For the most reliable battery reporting, use the official Xbox Wireless Adapter or another compatible Xbox wireless dongle.

The upstream discussion is available in [NiyaShy issue #49](https://github.com/NiyaShy/XB1ControllerBatteryIndicator/issues/49).

## Initial controller detection

A newly connected controller can briefly appear as **waiting for battery level data**. XInput may need several seconds and controller activity before battery information becomes available.

## Building from source

The project targets **.NET Framework 4.8**.

From a Visual Studio Developer PowerShell or another environment with MSBuild and NuGet available:

```powershell
nuget restore .\XB1ControllerBatteryIndicator\XB1ControllerBatteryIndicator.sln
msbuild .\XB1ControllerBatteryIndicator\XB1ControllerBatteryIndicator.sln /m /p:Configuration=Release /p:Platform="Any CPU"
```

The Release output is written to:

```text
XB1ControllerBatteryIndicator\XB1ControllerBatteryIndicator\bin\Release\
```

Every pull request and master build is also compiled on a Windows GitHub Actions runner.

## Reporting bugs

Please [open an issue](https://github.com/wefalltomorrow/XB1ControllerBatteryIndicator/issues/new/choose) and include:

- Windows version
- Application version
- Controller model
- Connection method
- What happened and what you expected
- Whether the problem is repeatable

For Bluetooth battery-reporting problems, read the Bluetooth limitation above first.

## Credits

Original application by [NiyaShy](https://github.com/NiyaShy).

This fork also incorporates selected maintenance ideas from [bergi9's fork](https://github.com/bergi9/XB1ControllerBatteryIndicator), while deliberately retaining the original lightweight application model and multi-controller behaviour. Translation contributions from the upstream project remain credited through the repository history.

Licensed under the [GNU General Public License v2](LICENSE).
