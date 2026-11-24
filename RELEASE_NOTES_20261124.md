## Review round 2 — all gadgets (24-11-2026)

### Global changes
1. Gadgets no longer start "always on top" (opt-in via right-click menu).
2. Opacity 100% is now truly opaque (panel alpha fixed; translucency only via Window opacity).
3. All menus/submenus/options windows restyled: white background, black text everywhere.
4. Magnet snapping with a small separation gap — to screen edges and other gadgets, while dragging.
5. Gadgets are no longer resizable; they consume exactly the space their content needs.
6. Graphs plot data at the correct time position on the horizontal scale (no more full-width fill when data is short).
7. Sliders now show the current value under the thumb, tick marks for every selectable value, and min/max + intermediate numbers under the bar.
8. Settings are saved to JSON immediately on every change — nothing gets lost.
9. Options windows show all content of every tab at once (no scrollbars; all tabs share the largest size).
10. Every gadget with graphs has a graph-height slider in options.
11. Changing the scale resizes the gadget window accordingly.
12. Submenus near the right screen edge now flip to the other side and stay selectable.
13. Main gadget titles ("Drives Meter", "Network Meter", …) are white and bold.
14. Fixed settings corruption when a second instance / options interaction wiped toggles (single shared config store).
15. Right-click context menu opens immediately.
16. "Update available" now appears as an extra line at the bottom of the gadget, flashing light blue with the new version; clicking it downloads, replaces the exe and relaunches automatically.
17. 100%-scale width is the Drives Meter reference width (~236 DIP); graphs/meters fit inside minus a small margin.
18. Graph history is bounded to the configured time — excess data is discarded (memory stays flat).
19. Fixed frozen graphs/values (network meter) — sampler self-heals and UI updates every tick.
20. Labels (Used:/Free:/Total:/RAM/Swap/Ext IP…) are white; their values light blue (like the clock HH:MM). Percentage-of-load values keep the green/yellow/red ramp. Digital Clock keeps its own colors.
21. Lower impact on measured data + lower memory/CPU usage: visual tree reused between ticks, ring buffers, cheap Win32 sampling only.

### Drives Meter
- Drive letters accepted without colon (C, D, V…).
- Optional per-drive read/write graph under each drive (default ON), reads green / writes yellow (text, icons and lines).
- Aggregate graph now plots total reads+writes of all drives with "Aggr Reads"/"Aggr Writes" rows.
- Read/write rates fixed (were always 0) via IOCTL_DISK_PERFORMANCE on the volume.
- New unit setting for reads+writes: Auto / GB/s / MB/s / KB/s (Auto shows 4 digits + dot, e.g. 43.45MB/s).
- Usage bar moved right under the "Free:" line, aligned from "Free:" start to the % position.
- Volume labels ("Datos", …) same font size as the drive letter.

### Digital Clock
- New "Sync time" row (icon + text): queries an NTP server and steps the system clock (applying needs admin). Server configurable in options, default pool.ntp.org.
- Seconds now share the light blue color of hours/minutes.

### Network Meter
- Link speed printed on its own line under the interface name, light blue.
- Unit setting for upload+download: Auto / GB/s / MB/s / KB/s (4 digits + dot).
- Upload green, download yellow (text, icons and graph lines).
- Options: Select all / Select none buttons, per-interface checkboxes, sorting by name/link speed.
- External IP line moved just before the first interface.

### CPU Usage
- Core numbering starts at 1; wrong "Core n" labels removed.
- Each row shows its effective clock speed (MHz) where "Core n" used to be.
- Real cores / E-cores / threads detected: cores white, efficient cores green, threads light blue.
