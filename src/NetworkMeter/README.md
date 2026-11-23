# Network Meter gadget (NetworkMeter)

Shows one block per selected network adapter, top to bottom:

- **Adapter name** (+ negotiated link speed, e.g. "1Gbps link")
- **Int. IP** — the adapter's internal IPv4 (and "(down)" when offline)
- **▲ TX / ▼ RX** rates (Mbit/s and MB/s)
- An auto-scaled two-line graph of TX (yellow) and RX (green) history

At the bottom, a machine-wide **Ext. IP** line with a **⟳** refresh button; the
external IP is fetched from api.ipify.org and refreshed automatically at the
interval set in Options.

Right-click anywhere for the common menu: **Always on top**, **Scale**
(100–200%), **Opacity** (20–100%), **Options…**, **Exit**. Settings persist in
`%APPDATA%\NetworkMeter\config.json`.

Options → **Interfaces**: pick Auto (all connected adapters) or specific ones.
Options → **General**: graph height, history length, external-IP refresh interval.

## Build

Requires the .NET 8 SDK.

```powershell
dotnet build src\NetworkMeter\NetworkMeter.csproj -c Release
```

Output: `src\NetworkMeter\bin\Release\net8.0-windows\NetworkMeter.exe` (framework-dependent;
needs the .NET 8 Desktop Runtime to run). Publish with `.\release-csharp.ps1`.
