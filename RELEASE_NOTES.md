# XB1 Controller Battery Indicator 1.3.3.0

This release adds the best non-Bluetooth quality-of-life ideas from the broader controller-battery ecosystem while keeping the app focused on Xbox/XInput and lightweight portable use.

## New features

- **Configurable warning threshold** — warn at Low (10–40%) or only at Empty (0–10%)
- **Auto-hide** — optionally hide the tray icon while no controller is connected
- **Theme override** — Auto, Light or Dark; Auto still follows Windows theme changes live
- **Diagnostics log** — state transitions and errors are written to a rotating 1 MB log under `%LOCALAPPDATA%\XB1ControllerBatteryIndicator`
- **Check for updates now** — manual update check from the tray menu

## Improvements

- Startup update checks are now asynchronous and have a 5-second timeout
- Dismissing a release suppresses repeated prompts for that exact version
- Start-with-Windows paths are quoted correctly
- Tray setting handlers no longer rely on the old inverted-value workaround
- Low-state warnings use a more appropriate low-battery toast message
- Default English toast dismiss button now says **Dismiss**
- Existing 5-second XInput polling, four-controller tracking, percentage ranges and Windows Exclamation warning sound remain unchanged

## Download choice

The **portable ZIP** remains the recommended download. A standalone EXE and SHA-256 checksum file are also attached.

## Build verification

The release is restored and compiled on a Windows GitHub Actions runner before publishing.
