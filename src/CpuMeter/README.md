# CPU Usage gadget (CpuMeter)

Shows, top to bottom:

- **CPU Usage** — total utilization % (green/yellow/red by load)
- **Clock** — current CPU clock speed (MHz)
- **Memory** — used / free / total for RAM and swap, numbers + usage bars
- **Per-core rows** — `[n]` label, colored bar and % for every logical processor
- Mini history graph of total CPU load

Right-click anywhere for the common menu: **Always on top**, **Scale**
(100–200%), **Opacity** (20–100%), **Options…**, **Exit**. The window drags by
its header and snaps to screen edges and other gadgets. Settings persist in
`%APPDATA%\CpuMeter\config.json`.

## Build

Requires the .NET 8 SDK.

```powershell
dotnet build src\CpuMeter\CpuMeter.csproj -c Release
```

Output: `src\CpuMeter\bin\Release\net8.0-windows\CpuMeter.exe` (framework-dependent;
needs the .NET 8 Desktop Runtime to run). Publish a new version with
`.\release-csharp.ps1` from the repo root — all running copies then offer a one-click update.
