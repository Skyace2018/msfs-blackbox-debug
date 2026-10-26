# Portable Windows development test package

The Windows x64 development ZIP contains the self-contained WPF decoder, a synthetic telemetry dump, and a START-HERE guide. Extract the entire ZIP in File Explorer, then double-click `Crash_Decoder.exe`. Do not run it from the ZIP preview. A separate .NET installation or PowerShell command is not needed.

Open `SAMPLE-synthetic-telemetry.json` with **Open telemetry dump**. The table and chart should populate. Try the navigation rail, timeline expansion, Light/Dark/System theme, tray panel, Copy diagnostics, and Export developer bundle. With no saved dump open, the export is diagnostics-only; with a dump open, it also contains the selected telemetry. The viewer keeps a bounded local diagnostic log under `%LocalAppData%\MSFS-Input-Blackbox` and does not send it automatically.

This package is for viewer and saved-dump testing. It does not include the MSFS SDK, official SimConnect interop DLL, live recorder, or in-simulator WASM hook. **Recorder not running** and an empty Live View are expected without those pieces. The sample dump is generated data and is not simulator evidence.

On the other PC, record the Windows version, whether the EXE opens, whether the sample chart populates, and the exact steps for any issue. If you share an export, review its contents first. See [test status](../TEST_STATUS.md) for the current evidence and the [live plan](LIVE_TEST_PLAN.md) for simulator integration tests.
