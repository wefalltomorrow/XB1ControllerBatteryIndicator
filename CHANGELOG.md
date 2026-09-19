# Changelog

All notable changes to this maintained fork are documented here.

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
