# Digital Clock gadget (DigitalClock)

Shows:

- **Big clock** — `HH:MM` with smaller `:SS` (or AM/PM in 12-hour mode)
- **Date line** — e.g. "23 November, 2026", optionally with the weekday before or after it
- **Info line** — Day-of-year, ISO week number and quarter (each optional)
- **Uptime** — system uptime since last boot ("UT Current: 0d 5h 30m 45s")

Right-click anywhere for the common menu: **Always on top**, **Scale**
(100–200%), **Opacity** (20–100%), **Options…**, **Exit**. Settings persist in
`%APPDATA%\DigitalClock\config.json`.

Options → **Format**: 12-hour clock, show seconds/date, weekday position.
Options → **Lines**: day-of-year / week / quarter / uptime toggles.
Options → **General**: info.

## Build

Requires the .NET 8 SDK.

```powershell
dotnet build src\DigitalClock\DigitalClock.csproj -c Release
```

Output: `src\DigitalClock\bin\Release\net8.0-windows\DigitalClock.exe` (framework-dependent;
needs the .NET 8 Desktop Runtime to run). Publish with `.\release-csharp.ps1`.
