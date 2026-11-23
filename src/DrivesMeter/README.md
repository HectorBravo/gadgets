# Drives Meter gadget (DrivesMeter)

Shows one block per configured drive, top to bottom:

- **Drive letter** (+ volume label) and a usage **%** badge (green/yellow/red)
- **Used / Free** space
- **Total** space plus live **▲ read** / **▼ write** rates (MB/s)
- A thin usage bar
- At the bottom, an aggregated disk-activity graph across all shown drives

Right-click anywhere for the common menu: **Always on top**, **Scale**
(100–200%), **Opacity** (20–100%), **Options…**, **Exit**. The window drags by
its header and snaps to screen edges and other gadgets. Settings persist in
`%APPDATA%\DrivesMeter\config.json`.

Options → **Drives**: set Drive 1..5 to a letter (e.g. `C:`) or `off` to end the list.
Options → **General**: toggle the aggregated activity graph.

## Build

Requires the .NET 8 SDK.

```powershell
dotnet build src\DrivesMeter\DrivesMeter.csproj -c Release
```

Output: `src\DrivesMeter\bin\Release\net8.0-windows\DrivesMeter.exe` (framework-dependent;
needs the .NET 8 Desktop Runtime to run). Publish with `.\release-csharp.ps1`.
