# XB1 Controller Battery Indicator 1.3.2.0

This is the first maintained release from the `wefalltomorrow` fork.

It keeps the original lightweight tray application and its familiar behaviour, while incorporating the useful maintenance fixes found across newer forks and correcting regressions introduced by some of those changes.

## Highlights

- Direct native XInput calls; SharpDX is no longer required
- All four controller slots are checked every second
- Multiple connected controllers still rotate in the tray every five seconds
- Better handling of XInput failures and controller initialization
- No false low-battery alerts for wired controllers
- Per-controller warning-sound state
- Faster theme handling with reliable theme-change watching
- Old Windows API Code Pack dependencies removed
- .NET Framework 4.8 target for current Windows 10/11 compatibility
- HTTPS fork-specific update checking
- Reproducible Windows CI build

## Download choice

The **portable ZIP** is recommended. It includes the executable and its configuration file.

A standalone EXE is also attached for convenience.

## Compatibility note

Battery reporting over Bluetooth remains limited by Windows/XInput and may be missing or inaccurate. The Xbox Wireless Adapter / proprietary Xbox wireless connection remains the recommended connection method.

XInput provides only four coarse battery levels: Empty, Low, Medium and Full. The application does not fabricate percentage values.

## Build verification

Release builds are restored and compiled on a current Windows GitHub Actions runner before publishing.
