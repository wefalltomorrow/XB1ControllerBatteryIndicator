# Changelog

All notable changes to this maintained fork are documented here.

## [1.3.3.0] - 2026-09-27

### Added

- Configurable battery warning threshold: Low (10–40%) or Empty (0–10%)
- Optional tray auto-hide when no controller is connected
- Auto / Light / Dark tray theme modes
- Manual "Check for updates now" command
- Lightweight rotating diagnostics log under `%LOCALAPPDATA%\XB1ControllerBatteryIndicator`
- Diagnostics menu command to open the log folder

### Changed

- Update checks are now asynchronous and no longer block application startup
- Update network requests now time out after 5 seconds
- Dismissing an available version suppresses repeated prompts for that same version
- Start-with-Windows entries now quote executable paths correctly
- Tray checkbox handlers now use their actual checked state instead of the inherited inverted-setting workaround
- Low-battery toasts use a dedicated message when the warning begins at the Low state
- Default English dismiss button text changed from "Shut up!" to "Dismiss"

### Optimized

- Diagnostics log records only controller state transitions and errors rather than every 5-second poll
- Log rotation is capped at 1 MB with one previous log retained

## [1.3.2.2] - 2026-09-20

### Changed

- Reduced XInput polling from every 1 second to every 5 seconds
- Wireless battery tooltips now show approximate percentage ranges instead of Empty / Low / Medium / Full
- XInput Empty is shown as 0–10%
- XInput Low is shown as 10–40%
- XInput Medium is shown as 40–70%
- XInput Full is shown as 70–100%

### Notes

- These are ranges, not exact percentages; XInput only exposes four coarse battery states

## [1.3.2.1] - 2026-09-20

### Changed

- Low-battery warning audio now uses the built-in Windows Exclamation system sound
- Enabling the warning no longer opens a WAV file picker
- The selected Windows sound scheme is respected automatically
- Existing "Repeat on loop" behaviour is unchanged

### Removed

- Custom WAV file selection
- The obsolete `wavFile` user setting and SoundPlayer state

## [1.3.2.0] - 2026-09-20

### Added

- Native XInput wrapper using `xinput1_4.dll`
- One-second polling of all four XInput controller slots
- Five-second visible-controller rotation independent of polling frequency
- Per-controller low-battery sound state
- Windows GitHub Actions build verification
- Automated GitHub Release workflow
- Fork-specific HTTPS update feed
- Maintained-fork documentation and contribution guidance

### Changed

- Retargeted the application from .NET Framework 4.5.2 to .NET Framework 4.8
- Theme state is cached and refreshed only when Windows reports a theme change
- Theme watcher lifetime is retained explicitly
- Translation resources are no longer rescanned on every language switch
- Update checks now use HTTPS/TLS 1.2 and this repository
- Release/version metadata now identifies version 1.3.2.0

### Fixed

- Persistent XInput failures can no longer spin the polling loop without delay
- Multi-controller refresh no longer scales to 5 seconds per connected controller
- Faster polling no longer causes only the last controller to be meaningfully visible
- Connected controllers whose battery data is not ready still show the waiting state
- Wired and initializing controllers no longer trigger false empty-battery alerts
- Low-battery notification state is tracked cleanly per controller

### Removed

- SharpDX
- SharpDX.XInput
- Microsoft.WindowsAPICodePack-Core
- Microsoft.WindowsAPICodePack-Shell
- Obsolete SharpDX binding redirect

## Upstream history

Versions through 1.3.1.2 originate from [NiyaShy/XB1ControllerBatteryIndicator](https://github.com/NiyaShy/XB1ControllerBatteryIndicator). See the upstream repository and release history for changes before this maintained fork's 1.3.2.0 release.
