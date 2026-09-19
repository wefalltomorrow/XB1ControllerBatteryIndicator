# XB1 Controller Battery Indicator 1.3.2.1

A small follow-up release that simplifies the low-battery warning sound.

## Changes

- The low-battery warning now uses Windows' built-in **Exclamation** system sound
- Enabling the warning no longer opens a custom WAV file picker
- The sound follows the user's configured Windows sound scheme
- **Repeat on loop** continues to replay the warning every five seconds while the battery remains empty
- Removed the obsolete custom-WAV path, `wavFile` setting and SoundPlayer instance

This keeps the warning feature zero-config and avoids depending on a specific file under `C:\\Windows\\Media`.

## Compatibility

The controller and battery handling is otherwise unchanged from 1.3.2.0. Battery reporting over Bluetooth remains subject to Windows/XInput limitations.

## Download choice

The **portable ZIP** is recommended. It contains the executable and configuration file.

A standalone EXE and SHA-256 checksum file are also attached.

## Build verification

Release builds are restored and compiled on a current Windows GitHub Actions runner before publishing.
