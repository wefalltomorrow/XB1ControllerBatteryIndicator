# Contributing

Contributions that keep XB1ControllerBatteryIndicator small, dependable and easy to run are welcome.

## Build environment

The project targets .NET Framework 4.8 and is built as a WPF application.

```powershell
nuget restore .\XB1ControllerBatteryIndicator\XB1ControllerBatteryIndicator.sln
msbuild .\XB1ControllerBatteryIndicator\XB1ControllerBatteryIndicator.sln /m /p:Configuration=Release /p:Platform="Any CPU"
```

Pull requests are built automatically on Windows through GitHub Actions.

## Project scope

Please prefer changes that:

- Improve XInput controller detection, battery-state handling or notifications
- Fix crashes, CPU usage, resource leaks or Windows compatibility
- Keep dependencies small and justified
- Preserve support for multiple XInput controller slots
- Preserve the lightweight tray-application design
- Improve translations or documentation

Avoid presenting battery percentages unless a reliable API actually provides a percentage. Standard XInput battery information exposes only Empty, Low, Medium and Full.

## Pull requests

Keep changes focused and explain:

- What problem is being solved
- What behaviour changes
- How it was tested
- Any Windows or controller-specific assumptions

For controller-specific bugs, include the controller model and connection method.
