# MSFS Input Blackbox

An experimental, low-overhead flight-input recorder for Microsoft Flight Simulator 2024. It is intended to preserve the final 60 seconds of control and selected panel-switch state when an external monitor detects a SimConnect disconnect or telemetry timeout.

> **Prototype status.** The .NET ring buffer and synthetic test are runnable without the simulator. The WASM-to-SimConnect Client Data transport has **not** been validated in a live MSFS 2024 installation. See [test status](TEST_STATUS.md) before relying on it for incident evidence.

## Repository layout

- `src/WASM_Hook` — MSFS gauge/WASM sender boundary.
- `src/External_Recorder` — .NET 8 monitor, ring buffer, JSON dumper, and synthetic self-test.
- `src/Crash_Decoder` — Windows 11–styled WPF dashboard for live status, local simulator settings, and saved JSON dumps.
- `docs/PROTOCOL.md` — binary contract and ownership assumptions.
- `docs/LIVE_TEST_PLAN.md` — Windows/MSFS validation procedure.
- `docs/PORTABLE_TEST_PACKAGE.md` — what the Windows development ZIP includes and how to test it on another PC.
- `docs/PERFORMANCE_AND_ISOLATION_PLAN.md` — multi-simulator adapter boundaries, known receive-thread risks, and live performance gates.

## Quick start: synthetic recorder test

```powershell
dotnet run --project src/External_Recorder -- --self-test
```

The command proves frame layout validation, wraparound behavior, ordered snapshots, and JSON dump creation. It does **not** connect to MSFS.

## Decode a saved crash dump

```powershell
dotnet run --project src/Crash_Decoder -- C:\path\to\crash_telemetry_dump.json
```

The Windows 11–styled viewer dashboard has a branded debugger-style layout: resizable navigation/table rails, telemetry tables, aircraft profiles, logging preferences, developer export/snapshot tools, and Community-folder discovery. It validates the dump schema before display. See [decoder details](docs/DECODER_UI.md).

The dashboard now has a collapsible navigation rail, an expandable input chart, a **Live View** toggle, high-contrast connection state with a polling indicator, and **Copy diagnostics** for a small JSON handoff. Live View reads a bounded local preview published by the external recorder; it does not start a SimConnect session itself.

For another Windows x64 computer, use the self-contained development ZIP described in [portable test package](docs/PORTABLE_TEST_PACKAGE.md). Extract the whole ZIP, then double-click `Crash_Decoder.exe`; no PowerShell or separate .NET installation is needed. The development build also records bounded local viewer diagnostics and can export them without a telemetry dump.

Choose **Settings → Appearance** to follow Windows, force Light mode, or force Dark mode.
Minimizing keeps the viewer running in the Windows notification area. Click its blue Blackbox tray icon for a compact status panel with **Open full screen**, **Minimize to tray**, and **Exit Blackbox**. Double-click the icon to restore the full dashboard. The executable and tray icon use the same blue Blackbox mark.

When a recorder is actively receiving frames, the tray panel shows a fresh live-session heartbeat, the latest yoke/rudder/throttle and switch values, and rolling-buffer use. **Mark event + export** timestamps the event and asks the recorder to save its current buffer as a developer ZIP in the folder chosen under **Settings → Developer handoff**. The first mark prompts for a folder if none is configured. See [event capture](docs/EVENT_CAPTURE.md) for the exact fields and limits.

## Compiling the live SimConnect boundary

Install the official `Microsoft.FlightSimulator.SimConnect.dll` from the MSFS SDK, then pass its path explicitly:

```powershell
dotnet build src/External_Recorder -p:SimConnectInteropPath="C:\path\to\Microsoft.FlightSimulator.SimConnect.dll" -p:EnableSimConnect=true
```

`SimConnectMonitor` registers the packed frame and handles client-data / quit / exception callbacks. A Windows message-loop host now dispatches callbacks, fills the ring buffer, publishes live status, and processes event markers. This code path has **not** been compiled against the target SDK or tested in MSFS 2024. The specified SDK DLL is copied only to local build output for execution; it is ignored by Git and is not committed.

On the Windows PC with the simulator and SDK, run `powershell -NoProfile -File scripts\Test-LivePrerequisites.ps1 -SimConnectDll "C:\path\to\Microsoft.FlightSimulator.SimConnect.dll"` before the [live test plan](docs/LIVE_TEST_PLAN.md). The preflight script only checks prerequisites and compiles the recorder; it does not install a package or launch MSFS.

## Building the WASM hook

Use the MSFS 2024 SDK toolchain and package layout configured on the target Windows machine. `src/WASM_Hook/Makefile` intentionally requires `MSFS_SDK` and `MSFS_WASM_SIMCONNECT_SUPPORTED=1`; it fails closed rather than pretending that the desktop SimConnect import library is usable by a gauge WASM module.

## Safety and privacy

The program records only telemetry defined in `TelemetryFrame`. Do not add account tokens, paths containing personal information, aircraft DRM assets, SDK binaries, or crash dumps with sensitive data to commits. Review generated JSON before sharing it publicly.

## Contributing

Keep the packed binary frame synchronized in C++ and C#. Any layout change requires updates to `docs/PROTOCOL.md`, the synthetic test, and a live-integration revalidation.
