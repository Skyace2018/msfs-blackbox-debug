# Windows MSFS 2024 live-integration test plan

## Prerequisites

- MSFS 2024 and its SDK installed on the same Windows PC.
- A confirmed supported method for a gauge/WASM module to use Client Data APIs.
- The official SimConnect managed interop DLL supplied to the recorder build; do not redistribute it.
- Developer Mode enabled and the WASM package installed only in the test Community folder.

First run `powershell -NoProfile -File scripts\Test-LivePrerequisites.ps1 -SimConnectDll "C:\path\to\Microsoft.FlightSimulator.SimConnect.dll"` from the repository root. Record its output. This checks the official managed DLL and compiles the conditional recorder; it does not launch MSFS or deploy the WASM hook.

## 1. Transport smoke test — UNKNOWN until run

1. Build/package the hook with `MSFS_WASM_SIMCONNECT_SUPPORTED=1` after verifying the SDK supports that configuration.
2. Start the simulator in Developer Mode and load a default aircraft at a quiet airport.
3. Start the recorder and verify it reports changing timestamps while moving aileron, elevator, rudder, and throttle controls. Confirm the aircraft title, indicated airspeed (knots), and plane altitude (feet) arrive through the separate user-aircraft SimConnect request.
4. Confirm the recorder receives 36-byte frames and values remain finite and within expected normalized ranges.

Pass criterion: 60 seconds of continuously advancing frames, with observed control movement. Record simulator build, SDK version, aircraft, package path, and any console output in `TEST_STATUS.md`.

## 2. Timeout behavior — UNKNOWN until run

1. While the recorder is receiving frames, disable the hook package or pause its sender path by a reversible developer test method.
2. Wait longer than the recorder timeout.
3. Verify exactly one `crash_telemetry_dump.json` is written and it contains ordered historical samples plus a timeout reason.

Pass criterion: JSON parses, has no more than the configured capacity, and includes the final healthy frames before silence.

## 3. Disconnect / simulator-close behavior — UNKNOWN until run

1. Repeat the smoke test.
2. Close the simulator normally, then repeat with a controlled simulator termination only on a disposable test session.
3. Verify the monitor captures its last received frames and labels the reason as disconnect, not a confirmed simulator crash.

Pass criterion: dump creation is deterministic, the process exits safely, and no claim is made that the module identified the root cause of a simulator crash.

## 4. Host message-loop check — UNKNOWN until run

The managed interop delivers callbacks through a Windows message loop. The conditional `LiveRecorderHost` now provides a hidden native window, dispatches its custom message ID to `ReceiveMessage`, and adds callbacks to the ring buffer. Build it with the target SDK and verify that a Client Data callback enters the ring buffer before testing timeout logic.

## 5. Tray dashboard and event marker — UNKNOWN until run

1. With the recorder receiving live frames, minimize the viewer to the tray and open the compact dashboard.
2. Verify the connection heartbeat remains fresh, the input values change with real controls, and the buffer count advances. Confirm a stopped recorder changes to an unconfirmed state after the heartbeat expires.
3. Choose a developer-export folder and select **Mark event + export** while changing a control. Only enter a manual aircraft title if the simulator-reported title is unavailable.
4. Confirm exactly one new ZIP appears, the tray reports its path, and the ZIP contains `telemetry.json`, `event.json`, `manifest.json`, and `README.txt`. Compare the UTC marker, frame range, last inputs, aircraft title, indicated airspeed, and altitude with the observed session. Confirm `event.json` labels whether the title was recorder-reported or user-entered.
5. Repeat after disconnect; the button should be disabled and an old request must not export frames from a new session.

## 6. Performance alongside other add-ons — UNKNOWN until run

Use the same aircraft, airport, weather, graphics settings, and background add-ons for repeated 30-minute runs: simulator alone; simulator plus recorder; then simulator plus recorder and viewer (dashboard visible and in the tray). Include a representative busy-airport segment and a cruise segment. During one recorder run, mark an event and export a full buffer while continuing normal control inputs. Repeat each condition enough to distinguish the tool's effect from run-to-run frame-time variation.

Record the simulator's median, 95th, and 99th percentile frame time; recorder and viewer CPU/memory; received/accepted/rate-limited frames; estimated timestamp gaps; maximum input-handler time; and any status heartbeat lapse or failed export. Check that an export does not stall reception. Compare to the provisional gates in [PERFORMANCE_AND_ISOLATION_PLAN.md](PERFORMANCE_AND_ISOLATION_PLAN.md). A timestamp gap is not proof of packet loss; preserve the actual recorder logs and mark interpretation separately.

The simulator, aircraft, airport, weather preset, typical other add-ons, and target PC specifications are useful inputs for choosing this case. They are not prerequisites for the simulator-free implementation tests.

## Reporting rules

Use **VERIFIED** only with command output or saved artifacts from the exact test run. Use **BLOCKED** for missing SDK/simulator prerequisites and **UNKNOWN** for unexecuted behavior. Preserve no personal flight data in public issue attachments.
