# Windows MSFS 2024 live-integration test plan

## Prerequisites

- MSFS 2024 and its SDK installed on the same Windows PC.
- A confirmed supported method for a gauge/WASM module to use Client Data APIs.
- The official SimConnect managed interop DLL supplied to the recorder build; do not redistribute it.
- Developer Mode enabled and the WASM package installed only in the test Community folder.

## 1. Transport smoke test — UNKNOWN until run

1. Build/package the hook with `MSFS_WASM_SIMCONNECT_SUPPORTED=1` after verifying the SDK supports that configuration.
2. Start the simulator in Developer Mode and load a default aircraft at a quiet airport.
3. Start the recorder and verify it reports changing timestamps while moving aileron, elevator, rudder, and throttle controls.
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

The managed interop delivers callbacks through a Windows message loop. Use a minimal host window, pass its handle and custom message ID to `SimConnectMonitor`, and call `ReceiveMessage` for that message. Verify that a Client Data callback enters the ring buffer before testing timeout logic.

## Reporting rules

Use **VERIFIED** only with command output or saved artifacts from the exact test run. Use **BLOCKED** for missing SDK/simulator prerequisites and **UNKNOWN** for unexecuted behavior. Preserve no personal flight data in public issue attachments.

