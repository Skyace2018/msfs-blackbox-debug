# MSFS Input Blackbox

An experimental, low-overhead flight-input recorder for Microsoft Flight Simulator 2024. It is intended to preserve the final 60 seconds of control and selected panel-switch state when an external monitor detects a SimConnect disconnect or telemetry timeout.

> **Prototype status.** The .NET ring buffer and synthetic test are runnable without the simulator. The WASM-to-SimConnect Client Data transport has **not** been validated in a live MSFS 2024 installation. See [test status](TEST_STATUS.md) before relying on it for incident evidence.

## Repository layout

- `src/WASM_Hook` — MSFS gauge/WASM sender boundary.
- `src/External_Recorder` — .NET 8 monitor, ring buffer, JSON dumper, and synthetic self-test.
- `src/Crash_Decoder` — Windows 11–styled WPF dashboard for live status, local simulator settings, and saved JSON dumps.
- `docs/PROTOCOL.md` — binary contract and ownership assumptions.
- `docs/LIVE_TEST_PLAN.md` — Windows/MSFS validation procedure.

## Quick start: synthetic recorder test

```powershell
dotnet run --project src/External_Recorder -- --self-test
```

The command proves frame layout validation, wraparound behavior, ordered snapshots, and JSON dump creation. It does **not** connect to MSFS.

## Decode a saved crash dump

```powershell
dotnet run --project src/Crash_Decoder -- C:\path\to\crash_telemetry_dump.json
```

The Windows 11–styled viewer dashboard has a branded debugger-style layout: always-visible navigation and settings, telemetry tables, aircraft profiles, logging preferences, and Community-folder discovery. It validates the dump schema before display. See [decoder details](docs/DECODER_UI.md).

## Compiling the live SimConnect boundary

Install the official `Microsoft.FlightSimulator.SimConnect.dll` from the MSFS SDK, then pass its path explicitly:

```powershell
dotnet build src/External_Recorder -p:SimConnectInteropPath="C:\path\to\Microsoft.FlightSimulator.SimConnect.dll" -p:EnableSimConnect=true
dotnet build src/External_Recorder -p:SimConnectInteropPath="C:\path\to\Microsoft.FlightSimulator.SimConnect.dll" -p:EnableSimConnect=true
```

`SimConnectMonitor` registers the packed frame and handles client-data / quit / exception callbacks. A Windows message-loop host must dispatch its chosen SimConnect message ID to `ReceiveMessage`; this final host wiring remains a live-integration task until the target SDK is tested. No SDK DLL is committed to this repository.

## Building the WASM hook

Use the MSFS 2024 SDK toolchain and package layout configured on the target Windows machine. `src/WASM_Hook/Makefile` intentionally requires `MSFS_SDK` and `MSFS_WASM_SIMCONNECT_SUPPORTED=1`; it fails closed rather than pretending that the desktop SimConnect import library is usable by a gauge WASM module.

## Safety and privacy

The program records only telemetry defined in `TelemetryFrame`. Do not add account tokens, paths containing personal information, aircraft DRM assets, SDK binaries, or crash dumps with sensitive data to commits. Review generated JSON before sharing it publicly.

## Contributing

Keep the packed binary frame synchronized in C++ and C#. Any layout change requires updates to `docs/PROTOCOL.md`, the synthetic test, and a live-integration revalidation.

