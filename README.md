# Desktop Gadgets (C#/WPF)

Native Windows desktop gadgets in the spirit of the Windows 7 Sidebar gadgets,
written in **C# / WPF** (`net8.0-windows`). Four independent apps share a common
base library (`src/GadgetCore`): borderless translucent window, drag-to-move with
snapping to screen edges and other gadgets, and a common right-click menu.

> The project was originally written in Rust (eframe/egui); that implementation
> has been fully replaced by this C#/WPF one and removed from the repo.

| Gadget | App | Description | README |
|--------|-----|-------------|--------|
| **cpu-meter** | `CpuMeter.exe` | Total CPU %, clock speed, RAM/swap (used/free/total + bars), per-core usage bars | [src/CpuMeter/README.md](src/CpuMeter/README.md) |
| **drives-meter** | `DrivesMeter.exe` | Per-drive used/free/total + read/write rates, aggregated activity graph | [src/DrivesMeter/README.md](src/DrivesMeter/README.md) |
| **network-meter** | `NetworkMeter.exe` | Per-NIC internal IP, TX/RX rates with auto-scaled dual-line graph, machine-wide external IP | [src/NetworkMeter/README.md](src/NetworkMeter/README.md) |
| **digital-clock** | `DigitalClock.exe` | Big clock with seconds, date, day-of-year/week/quarter lines, system uptime | [src/DigitalClock/README.md](src/DigitalClock/README.md) |

## Common right-click menu (every gadget)

- **Always on top** — toggle window topmost
- **Scale** — 100% / 125% / 150% / 200%
- **Opacity** — 20% / 40% / 60% / 80% / 100%
- **Options…** — opens the gadget options as a separate window
- **Exit**
- A flashing **"New version available"** line appears when a newer GitHub Release
  exists for that gadget; clicking it downloads, swaps and relaunches.

## Layout

```text
src/
  GadgetCore/     shared base library (window, snap, menu, config, graph, updater)
  CpuMeter/       -> CpuMeter.exe
  DrivesMeter/    -> DrivesMeter.exe
  NetworkMeter/   -> NetworkMeter.exe
  DigitalClock/   -> DigitalClock.exe
```

Each gadget keeps its settings in `%APPDATA%\<Gadget>\config.json` (settings only —
no runtime data is ever persisted). Gadgets discover each other and snap via
small rect files under `%TEMP%\RustGadgets\`.

## Building

Requires the **.NET 8 SDK**. Each app targets `net8.0-windows` with WPF.

```powershell
dotnet build src\CpuMeter\CpuMeter.csproj -c Release
# or publish a framework-dependent bundle:
dotnet publish src\CpuMeter\CpuMeter.csproj -c Release -o src\CpuMeter\bin\publish
```

The published apps are **framework-dependent**: the target machine needs the
**.NET 8 Desktop Runtime** installed. For a no-prerequisite single-file exe, use
the self-contained mode of the release script (see below).

## Releasing

`release-csharp.ps1` at the repo root builds, packages and publishes all four
gadgets to GitHub Releases with tags `<gadget>/<yyyyMMdd_HHmm>` (which the running
apps' update checker looks for):

```powershell
.\release-csharp.ps1                 # framework-dependent zips (small; needs .NET 8 Desktop Runtime)
.\release-csharp.ps1 -SelfContained  # one big self-contained single-file exe per gadget (no install needed)
```

Each release's zip is picked up automatically by all running copies of that gadget,
which then offer a one-click update.

