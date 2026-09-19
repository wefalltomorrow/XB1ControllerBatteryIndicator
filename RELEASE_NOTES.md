# XB1 Controller Battery Indicator 1.3.2.2

This release reduces background polling and makes the battery display more useful without pretending XInput provides an exact percentage.

## Changes

- Reduced controller polling from **every 1 second** to **every 5 seconds**
- Wireless battery tooltips now show approximate percentage ranges:
  - Empty → **0–10%**
  - Low → **10–40%**
  - Medium → **40–70%**
  - Full → **70–100%**
- Multi-controller tray rotation remains every five seconds
- Low-battery notifications and warning sounds are otherwise unchanged

## Important note about percentages

XInput does not provide an exact 0–100 battery percentage. It exposes four coarse battery states. The percentages shown by this release are the documented approximate charge ranges for those states, so the app does not invent false precision.

## Download choice

The **portable ZIP** is recommended. It contains the executable and configuration file.

A standalone EXE and SHA-256 checksum file are also attached.

## Build verification

Release builds are restored and compiled on a current Windows GitHub Actions runner before publishing.
