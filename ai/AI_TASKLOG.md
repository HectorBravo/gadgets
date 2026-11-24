# AI Tasks Log

## Summary

| Created | Task | Status | Type | Subtasks | Time Spent | Blockers |
|---------|------|--------|------|----------|------------|----------|
| 24-11-2026 04:21:45 | [T3: Review round 2 — 21 global + 17 gadget-specific fixes](#task-t3-review-round-2--21-global--17-gadget-specific-fixes) | <span style="background-color:#0969da;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">in_progress</span> | <span style="background-color:#0969da;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">refactor</span> | 0/14 | 0m | none |
| 23-11-2026 01:21:00 | [T1: Rust desktop gadgets (cpu/drives/network/clock)](#task-t1-rust-desktop-gadgets-cpudrivesnetworkclock) | <span style="background-color:#da3633;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">blocked</span> | <span style="background-color:#22863a;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">feat</span> | 12/14 | 7h 30m | superseded — Rust removed, see [T2](#task-t2-migrate-all-4-gadgets-rust--csharpwpf) (done) |

> ✅ **1 completed task(s)** — [View completed tasks](#completed-tasks)

---

## Task T3: Review round 2 — 21 global + 17 gadget-specific fixes

- **Status**: <span style="background-color:#0969da;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">in_progress</span>
- **Type**: <span style="background-color:#0969da;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">refactor</span>
- **Created**: 24-11-2026 04:21:45
- **Last Updated**: 24-11-2026 04:30:00
- **Time Spent**: 8m
- **Branch**: [`feat/ai-gadget-review-round-2`](https://github.com/HectorBravo/gadgets/tree/feat/ai-gadget-review-round-2)
- **Commit(s)**: pending
- **Blockers**: none
- **Findings & Notes**:
  - User reviewed the 4 C#/WPF gadgets and reported 21 global issues + gadget-specific issues (Drives 9, Clock 2, Network 5, CPU 3). All implemented in one pass; user asleep → best-judgement choices recorded below for confirmation tomorrow.
  - **User clarification (during task)**: percentage values currently color-coded by load (CPU total %, per-core %, drive used %, RAM/Swap %) KEEP their current green/yellow/red colors — NOT switched to light blue.

### User Confirmations

**Pending (awaiting user response):**

- [ ] (24-11-2026 04:21:45) #13/#20: gadget title → white+bold; labels (Used:/Free:/Total:/Int. IP/Ext. IP/RAM/Swap…) → white; values → light blue (#87cefa), EXCEPT percentage-of-load values which keep the green/yellow/red ramp. Digital Clock keeps all its current colors. Confirm?
- [ ] (24-11-2026 04:21:45) #17 fixed width: ~236 DIP content width (≈ Drives Meter at 100%, ≈250 window px incl. border); height auto-fits; Scale multiplies everything. Confirm exact width?
- [ ] (24-11-2026 04:21:45) #16 auto-update: flashing light-blue line at the BOTTOM of the gadget body; click → download zip → PS helper swaps + relaunches. No elevation; if exe folder isn't writable the swap fails and the line stays for retry. OK?
- [ ] (24-11-2026 04:21:45) Drives #9 volume labels ("Datos", "Multimedia"): same font size as drive letter (13), white. Confirm color?
- [ ] (24-11-2026 04:21:45) CPU #3 core classes via GetLogicalProcessorInformationEx: physical cores = white, E-cores = green, threads = light blue; on non-hybrid CPUs everything shows as normal (white). OK?
- [ ] (24-11-2026 04:21:45) Network #4: checkboxes + All/None buttons + Up/Down ordering saved in config; Auto mode kept. OK?
- [ ] (24-11-2026 04:21:45) #7 slider marks: ticks on the track + min/max/evenly-spaced intermediate numbers under the bar + current value under the thumb. OK?

**Confirmed (user provided):**

- (24-11-2026 04:30:00) Percentage-of-load colors (CPU load, drive usage %, RAM/Swap %) → "keep the colors as they are now" (green/yellow/red ramp), not light blue.

### Subtasks / Plan

- [ ] S1 — GadgetCore: Theme (white bold title, white labels, light-blue values, keep Usage() ramp) + light menu/window styling (#3, #13, #20)
- [ ] S2 — GadgetCore: unify ConfigStore<T:BaseSettings> (fix double-store overwrite), defaults AlwaysOnTop=false / Opacity=1.0 / opaque panel, ResizeMode=NoResize + SizeToContent, fixed width, LayoutTransform scale → window resizes (#1, #2, #5, #8, #11)
- [ ] S3 — GadgetCore: live "magnet" snap with small separation gap during drag (#4)
- [ ] S4 — GadgetCore: LineGraph time-proportional x-axis + ring buffers + graph-height option (#6, #10, #18), freeze fixes (#19)
- [ ] S5 — GadgetCore: shared OptionsWindow base (light theme, no scrollbars, uniform tab size) + LabeledSlider control (#7, #9)
- [ ] S6 — GadgetCore: update-available flashing light-blue bottom line + apply/relaunch (#16), immediate menu open (#15)
- [ ] S7 — DrivesMeter: "C" without colon; per-drive graphs (default on); aggregate reads+writes graph plotting + Aggr Reads/Writes rows; fix 0 rates (#D1–#D4)
- [ ] S8 — DrivesMeter: rate unit Auto/GB/s/MB/s/KB/s (4 sig digits w/ dot); bar under Free line aligned F→%; read green / write yellow (text+icon+lines); label size (#D5–#D9)
- [ ] S9 — DigitalClock: SNTP "Sync time" icon+text, NTP server option (pool.ntp.org); seconds light blue like HH:MM (#C1, #C2)
- [ ] S10 — NetworkMeter: link speed own line in light blue; rate unit setting; upload green / download yellow; All/None + ordering; Ext IP before first interface (#N1–#N5)
- [ ] S11 — CpuMeter: cores numbered from 1, "Core n" text replaced by per-core effective MHz; core/thread/E-core label colors (#P1–#P3)
- [ ] S12 — Efficiency pass: reuse visual tree instead of rebuilding each tick, ring buffers, cheap sampling (#18, #19, #21)
- [ ] S13 — Build all 4 Release clean + smoke run
- [ ] S14 — Docs/CHANGELOG, commit+push, publish releases (self-contained), merge to main, delete AI branches

### Full Context Notes for AI Agents

**Repo/toolchain:** `D:\Repos\gadgets`, remote `https://github.com/HectorBravo/gadgets`. .NET SDK 8.0.428, gh 2.105.0. No solution; each gadget csproj → `..\GadgetCore\GadgetCore.csproj` (net8.0-windows, UseWPF, code-built WPF, no XAML). Build: `dotnet build src\<Proj>\<Proj>.csproj -c Release`. Releases: `.\release-csharp.ps1 [-SelfContained]`, tag `<gadget>/<yyyyMMdd_HHmm>[-sc]`; UpdateService compares release PublishedAt vs exe LastWriteTime.

**Root-cause map:**
- #2 opacity: PanelBg had alpha 235 → panel translucent even at Window.Opacity=1. Fix: opaque panel, translucency only via Window.Opacity.
- #8/#14 settings lost / toggles stop working: base ctor got a SECOND `ConfigStore<BaseSettings>("Name")` while the gadget held `ConfigStore<TDerived>("Name")` — same file, last writer wins → fields wiped (e.g. ShowSeconds). Fix: `GadgetWindow<T> where T : BaseSettings` with ONE store shared by gadget + base.
- #3 menus unreadable: dark ContextMenu bg + default Aero item templates → white selection bar / white-on-white submenus. Fix: explicit light styles (white-ish bg, black text) for ContextMenu/MenuItem (all roles)/Separator applied app-wide + light Options windows.
- #6 graph fills width early: `x = w*i/(count-1)`. Fix: map over configured time capacity so N samples occupy N/cap of the width.
- #5/#11/#17: ResizeMode=NoResize, SizeToContent=WidthAndHeight, fixed content width (236 DIP), LayoutTransform instead of RenderTransform → window follows scaled size automatically.
- #4/#12/#15: live snap during drag (mouse move + SetWindowPos) with ~6px separation; menu opened immediately on MouseRightButtonUp with `IsOpen=true`; submenu flip near right edge fixed once window rect matches visual size (LayoutTransform).
- #19 frozen network graph: exceptions during enumeration + full UI rebuild each tick; fix robust sampling + in-place updates.
- #21 efficiency: keep TextBlock references and update Text only; Ring<T> buffers instead of List.RemoveAt(0); cache volume→disk map + adapter list; one NtQuerySystemInformation call/s.

**Colors decided:** Title = white bold (except Digital Clock unchanged). Labels = Theme.White. Values = Theme.Blue (#87cefa) EXCEPT percent-of-load which keeps Theme.Usage() ramp. Drives read=green, write=yellow; Network upload=green, download=yellow (+graph lines). Menus: bg #FFFFFF, text #000000, hover #E8F2FE, submenu identical.

**Files:** src/GadgetCore/{Theme,Config,GadgetWindow,Snap,LineGraph,UsageBar,Fmt,UpdateNotifier}.cs + new {MenuStyle.cs, OptionsBase.cs, LabeledSlider.cs, Ring.cs, SntpClient.cs}; gadget MainWindow/Sampler/Settings/OptionsWindow ×4; READMEs.

**State:** branch `feat/ai-gadget-review-round-2` from main @ 45a94ba. Next: rewrite GadgetCore (S1–S6), then gadgets (S7–S12), build, docs, releases, merge.

---

## Task T2: Migrate all 4 gadgets Rust → C#/WPF

- **Status**: <span style="background-color:#22863a;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">done</span>
- **Type**: <span style="background-color:#0969da;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">refactor</span>
- **Created**: 23-11-2026 12:00:57
- **Last Updated**: 23-11-2026 12:36:24
- **Time Spent**: 18m
- **Branch**: [`feat/ai-rust-desktop-gadgets`](https://github.com/HectorBravo/gadgets/tree/feat/ai-rust-desktop-gadgets) — reused the existing AI branch (C# work continues here; ahead of origin by 3 commits: `47ee25c` base+CpuMeter, `30c107f` gitignore bin/obj, `9bbd38a` wip checkpoint DrivesMeter partial)
- **Commit(s)**: [cce6324](https://github.com/HectorBravo/gadgets/commit/cce6324) (log resume), [107a4c9](https://github.com/HectorBravo/gadgets/commit/107a4c9) (DrivesMeter UI + NetworkMeter + DigitalClock ports), [a142d27](https://github.com/HectorBravo/gadgets/commit/a142d27) (log), [10cefe9](https://github.com/HectorBravo/gadgets/commit/10cefe9) (release script), [a80759a](https://github.com/HectorBravo/gadgets/commit/a80759a) (-SelfContained mode), [5e7e2f3](https://github.com/HectorBravo/gadgets/commit/5e7e2f3) (remove Rust + docs C# canonical)
- **Blockers**: none
- **Findings & Notes**:
  - **Git history is INTACT** — the "corrupted history" was only that `ai/AI_TASKLOG.md` was never updated for the C# migration; commits (`47ee25c`, `30c107f`, `9bbd38a`) are all present and recoverable via `git log`. Resumed from those + reading every source file.
  - **Why C#**: Rust/egui gadgets rejected by user ("right click doesn't work, graphs don't work, texts overlapped, dragging broken" + visuals far from reference). Migrated to C#/WPF (net8.0-windows), same proven pattern as `D:\Repos\nvidia_app`.
  - **Status at resume**: `src/GadgetCore` ✅; `src/CpuMeter` ✅ (reference pattern); `src/DrivesMeter` ⚠ partial (Sampler+Settings+csproj done; MISSING Program/MainWindow/OptionsWindow); `src/NetworkMeter` ❌ empty; `src/DigitalClock` ❌ empty.
  - Rust sources kept at repo root as visual/behavior reference until C# parity confirmed.

### User Confirmations

**Pending (awaiting user response):**

- None yet.

**Confirmed (user provided):**

- (23-11-2026 11:07:00) Migrate all 4 gadgets to C#/WPF, mirroring `D:\Repos\nvidia_app`; keep Rust sources until parity confirmed. → "migrate to C# as you don't seem to be a good rust developer"

### Subtasks / Plan

- [x] S1 — `GadgetCore` (Config, Theme, GadgetWindow base w/ drag+snap+menu+scale+opacity, LineGraph, Snap IPC, Fmt, UpdateService, UpdateNotifier) ✅ prior session
- [x] S2 — `CpuMeter` full C# port ✅ prior session (reference pattern)
- [x] S3 — `DrivesMeter` Sampler + Settings + csproj ✅ prior session
- [x] S4 — `DrivesMeter`: add Program.cs, MainWindow.cs (per-drive Used/Free/Total + ▲read/▼write MB/s + usage bar + aggregated activity graph), OptionsWindow.cs (drive slots C:..off, ShowGraph) ✅ (+ moved UsageBar to GadgetCore so all gadgets share it; fixed missing `using System.Text` in Sampler)
- [x] S5 — `NetworkMeter` full port (csproj, Settings, Sampler via System.Net.NetworkInformation, MainWindow per-NIC blocks+graphs+external IP, OptionsWindow) ✅ (Sampler uses NetworkInterface IPv4-stat deltas instead of fragile MIB_IF_ROW2; ExternalIp.cs = hidden powershell api.ipify.org fetcher)
- [x] S6 — `DigitalClock` full port (csproj, Settings, Sampler uptime+ISO week, MainWindow big time/date/info lines, OptionsWindow Format/Lines/General) ✅ (uses System.Globalization.ISOWeek + GetTickCount64)
- [x] S7 — `dotnet build` all 4; fix compile errors until clean ✅ all Release OK (0 errors); fixed NetworkMeter ni.Speed long→ulong cast + removed unused _openOptions field
- [x] S8 — Push to `feat/ai-rust-desktop-gadgets` (upstream already set) ✅ pushed; remote tip = local HEAD `107a4c9`, branch synced with origin
- [x] S9 — GitHub Releases for the C# builds: added `release-csharp.ps1` (framework-dependent + `-SelfContained` single-file modes); published framework-dependent releases tag `<gadget>/20261123_1220`; later self-contained `_1226-sc` removed per user, plus old Rust-era releases (`_1054`, `_0903`) deleted → only the 4 C# `_1220` releases remain
- [x] S10 — Docs + cleanup: deleted all Rust source folders (cpu-meter/, drives-meter/, network-meter/, digital-clock/, gadget-core/) + build_env.bat via `git rm`; rewrote root README to C#/WPF canonical; added per-gadget READMEs under src/; cleaned `.gitignore` of Rust patterns. Commit `5e7e2f3`.

### Full Context Notes for AI Agents

**Build/toolchain:** .NET SDK 8.0.428 working. Build: `dotnet build D:\Repos\gadgets\src\<Proj>\<Proj>.csproj -c Release`. No solution file; each gadget csproj references `..\GadgetCore\GadgetCore.csproj`. All net8.0-windows, UseWPF=true, code-built WPF (no XAML).

**Established C# pattern (mirror GadgetCore + CpuMeter exactly):**
- Namespace root `HectorBravo.Gadgets.<Proj>`; core `HectorBravo.Gadgets.Core`.
- csproj: WinExe, AssemblyName=`<Proj>`, ProjectReference GadgetCore.
- Program.cs: `[STAThread] static Main()` → `new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown }; app.Run(new MainWindow());`
- `MainWindow : GadgetWindow`. Base ctor `(string snapName, ConfigStore<BaseSettings>)`. CpuMeter passes a *separate* `ConfigStore<BaseSettings>("CpuMeter")` to base AND its own `ConfigStore<CpuSettings>("CpuMeter")`; follow this. Build UI into `ContentHost.Children` (Grid inside scaled Border). Header = Grid [icon | title(Theme.TitleFg bold) | right pct]. `DispatcherTimer { Interval=1s }`→Refresh(); also Refresh on Loaded. `OpenOptions = () => { var w=new OptionsWindow(_cfg){Owner=this}; w.Show(); };`. End ctor with `new UpdateNotifier(this, new UpdateService("<slug>","<Exe>.exe"))`.
- Slugs/exes: cpu-meter/CpuMeter.exe, drives-meter/DrivesMeter.exe, network-meter/NetworkMeter.exe, digital-clock/DigitalClock.exe.
- OptionsWindow : Window — dark bg `#161a23`, TabControl/TabItems, CheckBox bound to cfg.Data (Checked/Unchecked → set + Save).

**GadgetCore API:** ConfigStore<T>(.Data,.Save(),.Path); BaseSettings(X?,Y?,Width=250,Height=430,AlwaysOnTop,Scale,Opacity). GadgetWindow: ContentHost(Grid), OpenOptions(Action?), UpdateLineProvider, OnUpdateClick, SaveGeometry(). Theme: PanelBg,Border,Fg,Muted,TitleFg(#ffd24a),Green(#bfffb2),DeepGreen(#158000),Yellow(#ffe14d),Red,Blue(#87cefa),GraphBg,GridPen,BorderPen; Usage(frac),CoreColors[],Hex(str),fonts Ui/Mono. LineGraph:SetSeries(List<Series>),FixedMax?,LegendFormat(Func<double,string>); Series(Name,IReadOnlyList<double>,Color). Fmt.Bytes/Rate/BitRate. Snap, UpdateService(slug,exe), UpdateNotifier(window,svc).

**UsageBar decision:** currently defined in `HectorBravo.Gadgets.CpuMeter` (MainWindow.cs) as public class with Fraction+Color. MOVE it to GadgetCore (new file `src/GadgetCore/UsageBar.cs`, namespace HectorBravo.Gadgets.Core) so Drives/Network reuse it; delete from CpuMeter and add `using HectorBravo.Gadgets.Core;` there (already imported).

**DrivesMeter (Sampler done):** DriveSample(Letter,Label,Total,Free,ReadBps,WriteBps); Sampler.Sample(IReadOnlyList<string>)→List<DriveSample>; Sampler.ActivityHist(List<double> newest-last, GraphLen=160). Settings DrivesSettings{List<string> Slots={"C:"..}; bool ShowGraph}. Active slots = take until first "off", drop empty. MainWindow: header [🗄 "Drives"]; per-drive block `C:`+right %badge, `Used: x  Free: y`, `Total: z`, ▲read MB/s ▼write MB/s(Yellow), thin UsageBar; bottom aggregated activity LineGraph(LegendFormat=Fmt.Rate) from ActivityHist. OptionsWindow: Drives tab (5 slot TextBoxes → uppercase letter or "off"), General tab (ShowGraph).

**NetworkMeter (port from network-meter/src):** Settings NetworkSettings{List<string> InterfaceNames=new(); double GraphHeight=70; int HistorySeconds=120; int ExternalRefreshMinutes=5}. Empty names = auto(all up non-loopback). Sampler via System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces(): Name/Description, OperationalStatus==Up, Speed(bps), Type!=Loopback, GetIPv4Statistics().BytesReceived/Sent for rate deltas, UnicastAddresses where AddressFamily==InterNetwork for internal IPv4. Rates = delta/elapsed. External IP: background hidden powershell `Invoke-RestMethod https://api.ipify.org`, cache, ⟳ button + auto every N min. MainWindow: ScrollArea(StackPanel) per-NIC blocks (name header +(speed), Int.IP value +(down)red, row ▲Mbit/s Yellow ▼MB/s Green, LineGraph h=GraphHeight two Series TX#ffe14d/RX#bfffb2); bottom Ext.IP + ⟳. OptionsWindow: Interfaces tab (Auto radio + checkbox list w/status), General tab (graph height/history/refresh sliders).

**DigitalClock (port from digital-clock/src):** Settings ClockSettings{bool Use12h,ShowSeconds=true,ShowDate=true; WeekdayPos Weekday=None; bool ShowDayOfYear=true,ShowWeek=true,ShowQuarter=true,ShowUptime=true}. enum WeekdayPos{None,Before,After}. Sampler: DateTime.Now + GetTickCount64(kernel32)/1000 uptime. DayOfYear prop; ISO week via System.Globalization.ISOWeek.GetWeekOfYear(date); quarter=(month-1)/3+1. MainWindow: big `{hh:02}:{mm:02}` size40 Blue, smaller `:{ss:02}` size20 Fg (or AM/PM 12h-no-sec). Date line Yellow "23 November, 2026" weekday None/Before("Wednesday ...")/After("... (Wednesday)"). Info line Green joined 2 spaces "Day: N  Week: N  Quarter: N". Uptime Fg "UT Current: 0d 5h 30m 45s". Timer ~250ms. OptionsWindow tabs Format/Lines/General.

**Git identity (every git cmd):** `$env:GIT_AUTHOR_NAME='AI_bot'; $env:GIT_COMMITTER_NAME='AI_bot'` — never set email/config. Msgs `<type>(<scope>): [ai] ...`. Branch exists & tracks origin → plain `git push`.

**Current state:** GadgetCore+CpuMeter complete. DrivesMeter needs Program/MainWindow/OptionsWindow (+move UsageBar to core). NetworkMeter+DigitalClock need everything. Next: S4, then S5/S6, build S7, push S8.

---


## Task T1: Rust desktop gadgets (cpu/drives/network/clock)

- **Status**: <span style="background-color:#0969da;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">in_progress</span>
- **Type**: <span style="background-color:#22863a;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">feat</span>
- **Created**: 23-11-2026 01:21:00
- **Last Updated**: 24-11-2026 07:55:00
- **Time Spent**: 6h 10m
- **Branch**: [`feat/ai-rust-desktop-gadgets`](https://github.com/HectorBravo/gadgets/tree/feat/ai-rust-desktop-gadgets) — pushed with -u (local tracks remote)
- **Commit(s)**: [48f8a08](https://github.com/HectorBravo/gadgets/commit/48f8a08) (branch+log), [9be0c66](https://github.com/HectorBravo/gadgets/commit/9be0c66) (core+cpu-meter), [325632e](https://github.com/HectorBravo/gadgets/commit/325632e) (drives-meter+snap)
- **Blockers**: none
- **Findings & Notes**:
  - Repo: https://github.com/HectorBravo/gadgets — currently only a stub README.md on `main`.
  - Reference app (update-notifier pattern): `D:\Repos\nvidia_app` → `src/NvidiaMonitor/UpdateService.cs`: GitHub Releases tags `<type>/<yyyyMMdd_HHmm>`, hourly re-check, flashing "New version available (click to update)" link, PowerShell swap-helper relaunch. We mirror this per gadget with tag prefix = gadget name.
  - Windows 7 gadgets being replaced live on the user's desktop: Drives Meter and CPU Usage visible top-right; original .gadget zips in `D:\Programas\Gadgets` (extracted by user to `D:\Programas\Gadgets\extract`; reference screenshot stored at `docs/reference/original_gadgets.png`, git-ignored).
  - **All 4 gadgets implemented, compiled (GNU toolchain) and running**: CpuMeter, DrivesMeter, NetworkMeter, DigitalClock. Common base `gadget-core` (window/snap/menu/config/graph/updater). First GitHub release published for cpu-meter (`cpu-meter/20261123_0903`).
  - **Push (RESOLVED)**: plain `git push` works now from this shell (earlier hangs were stale credential/GPG state; the trick was to let it run without piping tricks and verify via `git ls-remote`). Branch `feat/ai-rust-desktop-gadgets` fully synced with origin (tip e225a65).
  - **Screenshot blocker**: `Graphics.CopyFromScreen` fails with "bad controller" on all 3 monitors since the machine crash (~07:40 24-11). Reboot expected to fix; then capture each gadget → `<gadget>/screenshot.png`.

### User Confirmations

**Pending (awaiting user response):**

- None yet.

**Confirmed (user provided):**

- (23-11-2026 11:07:00) Rust gadgets rejected on the user's machine ("right click doesn't work, graphs don't work, texts overlapped, dragging broken" + visuals far from the reference screenshots) → **"migrate to C# as you don't seem to be a good rust developer"** — full migration of all 4 gadgets to C#/WPF (net8.0-windows), same proven pattern as `D:\Repos\nvidia_app` (code-built WPF, dark Theme.cs, GitHub Releases UpdateService). Rust sources kept for reference until parity is confirmed.

- (23-11-2026 00:45:00) Which toolchain? → "Install Windows SDK 10 first so MSVC builds work" — later amended below.
- (23-11-2026 01:21:00) After repeated SDK install failures (MSVC linker OK but no Windows SDK libs; registry claims SDK 10.1.18362 installed yet `C:\Program Files (x86)\Windows Kits\10` missing — user deleted it manually) → "proceed with the coding with the GNU toolchain, the nice exes can wait".
- (23-11-2026 00:50:00) Network meter → external IP is machine-wide (plain-text HTTP service + manual refresh icon + auto-refresh every 5 min); per-NIC graph plots TX and RX with height auto-scaling; NO session/total transfer figures.
- (23-11-2026 00:50:00) Config storage → JSON file per gadget (`%APPDATA%\<Gadget>\config.json`); no runtime/historical data persisted.
- (23-11-2026 00:40:00) Repo layout → each gadget independent: own folder, own binary, own GitHub Releases namespace, own README with screenshot.

### Subtasks / Plan

- [x] S1 — Create `feat/ai-rust-desktop-gadgets` branch + push (with -u) ✅ pushed, upstream set (`git branch -vv` verified)
- [x] S2 — `gadget-core`: config JSON (%APPDATA%), borderless window (drag, snap to screen edges + other gadgets), right-click menu (Always on top / Scale 100–200% / Opacity 20–100% / Options… / Exit), options-window scaffolding, update notifier (GitHub Releases per gadget, one-click download+swap) ✅
- [x] S3 — `gadget-core` build + smoke test with a dummy gadget ✅ (cpu-meter doubles as smoke test)
- [x] S4 — `cpu-meter`: total CPU %, clock speed, RAM used/free/total + swap (numbers + bars), per-core bars `[n]` + % ✅ runs live, verified via screenshot crop
- [x] S5 — `drives-meter`: per-drive Used/Free/Total + read/write rates + usage bar; aggregated activity graph; options: pick drives (empty = off) ✅ compiles + runs
- [x] S6 — `network-meter` ✅ (agent-b): per-NIC name, internal IP, TX/RX rates, auto-scaled two-line graph; machine-wide external IP (⟳ + 5-min auto via api.ipify.org); options: Interfaces/General tabs
- [x] S7 — `digital-clock` ✅ (agent-b): big time w/ seconds, date, day-of-year/week/quarter, uptime; Format/Lines/General option tabs
- [x] S8b — network-meter + digital-clock live verification ✅ (processes alive & responding)
- [ ] S9 — Screenshots per gadget → README.md ⚠ blocked: screen capture broken system-wide since ~07:40 24-11 (`CopyFromScreen` → "bad controller" on all monitors; likely GPU driver state after the crash). READMEs reference `screenshot.png`; add files when capture works (reboot should fix).
- [x] S10 — `release.ps1` per gadget ✅ + first real release published: tag `cpu-meter/20261123_0903` on GitHub Releases (gh CLI)
- [x] S11 — Root README linking the 4 gadgets ✅
- [x] S12 — Final clean-state build of all 4 ✅ (release profile: CpuMeter 5.9MB, DrivesMeter 5.7MB, NetworkMeter 5.8MB, DigitalClock 5.6MB)
- [x] S13 — Push AI branch ✅ synced with origin (tip e225a65; verified via `git ls-remote`)
- [ ] S14 — Post-merge: fast-forward AI branch onto final log, re-push

### Full Context Notes (cont.) — Environment & verified deps

**Toolchain (verified 23-11-2026):**
- Windows, PowerShell 5.x, cargo 1.99.1; default toolchain `stable-x86_64-pc-windows-msvc`, also installed `stable-x86_64-pc-windows-gnu`.
- **MSVC target broken**: VS2022 BuildTools OK (`link.exe` at `...\VC\Tools\MSVC\14.44.35207\bin\Hostx86\x64`) but NO Windows SDK → `LNK1181 kernel32.lib`. Registry `HKLM:\SOFTWARE\WOW6432Node\Microsoft\Windows Kits\Installed Roots` claims SDK 10.1.18362 (KitsRoot10=`C:\Program Files (x86)\Windows Kits\10\`) but folder missing (user deleted manually). Do NOT retry installs; user chose GNU.
- **GNU works** — build command:
  `cmd /c 'set PATH=D:\SDK_ARM\msys64\mingw64\bin;%PATH% && cargo +stable-x86_64-pc-windows-gnu build'`
- Offline SDK installer kept at `D:\dlc\Windows Kits\10\WindowsSDK` for a future MSVC fix (select "Windows SDK for Desktop C++ amd64 Apps", run elevated).
- `build_env.bat` at repo root = vcvars helper, only useful after SDK libs exist.

**Verified dependency set (probe crate compiled OK):**
- `eframe = { version = "0.37", default-features = false, features = ["glow","persistence","default_fonts"] }` (feature name is `default_fonts`, NOT `winit_default_fonts`).
- `windows-sys = "0.61"` features: Win32_Foundation, Win32_UI_WindowsAndMessaging, Win32_UI_Shell, Win32_System_LibraryLoader, Win32_Graphics_Gdi, Win32_Storage_FileSystem, Win32_NetworkManagement_IpHelper, Win32_NetworkManagement_Ndis, Win32_Networking_WinSock, Win32_System_ProcessStatus, Win32_System_Threading, Win32_Globalization.
- `serde` (+derive), `serde_json`, `dirs-lite`.
- Updater HTTP: no Rust HTTP crate — spawn hidden PowerShell (`Invoke-RestMethod` GitHub API / `Invoke-WebRequest` download) mirroring nvidia_app's helper-script approach.

**Repo layout (agreed):**
```
gadget-core/            shared base crate (path dep for all gadgets)
cpu-meter/  drives-meter/  network-meter/  digital-clock/   each: Cargo.toml, src/, release.ps1, README.md (+screenshot.png)
ai/AI_TASKLOG.md
```
Binaries: `CpuMeter.exe`, `DrivesMeter.exe`, `NetworkMeter.exe`, `DigitalClock.exe`. Config: `%APPDATA%\<exe stem>\config.json` (serde_json; only user settings + window pos/size — never runtime samples).

**Visual target (user screenshots + live capture):** dark translucent rounded panel, thin border. Header: icon + name + overall % badge.
- CPU: `Clock: 3600MHz`; Used/Free/Total columns; RAM line % + bar; swap line; per-core rows `[0]`…`[15]` colored bar + %.
- Drives: `C:` + % badge, `Used: x  Free: y`, `Total: z`, ▲read/▼write MB/s, thin usage bar; bottom aggregated activity graph.
- Network (per NIC): name, internal IP; machine-wide external IP; ▲/▼ rates; two-line TX/RX graph auto-scaled in height.
- Clock: big time w/ seconds, date, uptime.
- Right-click menu everywhere: Always on top ✓ / Scale ▶ 100–200% / Opacity ▶ 20–40–60–80–100% / Options… (separate tabbed window) / Exit.

**Update notifier:** tag `<gadget>/<yyyyMMdd_HHmm>` on https://github.com/HectorBravo/gadgets releases; check at startup + hourly; flashing blue "New version available: <tag> (click to update)" line in menu/header; click → download zip → PS helper waits for exit, backs up *.exe, extracts, relaunches.

**Snapping:** while dragging, snap within ~12px to monitor edges and other gadget windows. File-based IPC: each gadget writes its rect to `%TEMP%\RustGadgets\<name>.rect` on move; others read it (avoids Win32 messaging).

**Git identity:** `$env:GIT_AUTHOR_NAME='AI_bot'; $env:GIT_COMMITTER_NAME='AI_bot'` on every git cmd; never set email/config. Messages `<type>(<scope>): [ai] ...`.

**Parallel agents:** A = cpu-meter + digital-clock, B = drives-meter + network-meter; both depend on gadget-core (S2/S3) which the main agent owns first.

**Current state:** nothing committed yet; workspace has `build_env.bat`, stub `README.md`, `ai/AI_TASKLOG.md`.
**Next step:** S1 (branch), then S2 (gadget-core).


---

## Completed Tasks

| Created | Task | Type | Subtasks | Time Spent |
|---------|------|------|----------|------------|
| 23-11-2026 12:00:57 | [T2: Migrate all 4 gadgets Rust → C#/WPF](#task-t2-migrate-all-4-gadgets-rust--csharpwpf) | <span style="background-color:#0969da;color:#fff;padding:2px 8px;border-radius:12px;font-size:12px;">refactor</span> | 10/10 | 18m |
