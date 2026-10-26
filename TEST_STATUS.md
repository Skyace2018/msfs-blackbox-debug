# Test status

Date: 2026-10-25

| Phase | Status | Evidence / limitation |
| --- | --- | --- |
| Repository static validation | VERIFIED | C++/C# frame contract is documented as 36 bytes; source contains matching packed layouts and assertions. Secret-pattern scan found no credential-bearing files. |
| .NET recorder build | VERIFIED | `dotnet build src/External_Recorder/External_Recorder.csproj --no-restore` succeeded with 0 warnings on .NET SDK 10.0.401, targeting .NET 8. |
| .NET recorder synthetic self-test | VERIFIED | `dotnet run --project src/External_Recorder --no-build -- --self-test` passed: 36-byte layout, three newest frames retained in order, and JSON dump emitted with telemetry fields. |
| Decoder UI build / parser self-test | VERIFIED | The decoder was migrated to a WPF Windows 11–styled dashboard and built successfully to a separate output folder because an older running executable held the default output lock. Its `--self-test` passed against synthetic telemetry and `UserCfg.opt` data. |
| Decoder UI launch smoke test | VERIFIED | The WPF shell launched without an exception and remained open during a 10-second local smoke test after startup-event and settings-write guards were added. |
| Developer export and rolling-capacity UI | VERIFIED | Recorder and WPF decoder both built with 0 warnings; decoder self-test and a subsequent 10-second launch smoke test passed. Export contents and live buffer percentage still require an actual telemetry session. |
| Maximized launch and in-app window controls | VERIFIED | The WPF decoder builds with 0 warnings after adding default maximized launch plus persistent minimize, restore/maximize, and close controls. |
| System / Light / Dark theme selector | VERIFIED | The decoder builds with 0 warnings and passed a 10-second launch smoke test after adding persistent theme preferences and theme-aware WPF checkbox/dropdown controls. |
| Tray UI, dropdown layout, and executable icon | VERIFIED | The updated decoder built with 0 warnings and errors. Its synthetic self-test exited successfully. A local window smoke test obtained a nonzero main-window handle; a Windows minimize command left the process running and hid the main window. The generated executable contained a 32×32 embedded icon. The tray popup and dropdowns still need hands-on visual confirmation on the target desktop. |
| Event marker and developer ZIP (synthetic) | VERIFIED | The recorder builds with 0 warnings and its self-test creates an event ZIP. The test checks the marker time, last input frame, captured-variable list, optional aircraft label, and one-hour recording preferences. The WPF viewer builds with 0 warnings and its decoder self-test passes. |
| Dashboard triage UI and live-preview contract | VERIFIED | The updated WPF decoder builds and opens locally. Its synthetic self-test checks the preview parser, session matching, and stale-preview rejection. The recorder self-test writes and checks ordered preview frames. The high-contrast status, collapsible sidebar, chart focus mode, Live View toggle, clipboard action, and quick-action styling were inspected in a local dashboard screenshot. Real simulator frames remain untested. |
| Portable Windows x64 viewer package | VERIFIED | `dotnet publish` produced a self-contained Release folder. Its published EXE passed `--self-test` with exit code 0, including a diagnostics-only ZIP check. A synthetic dump and START-HERE guide are included. The ZIP itself was integrity checked. Cross-computer launch is UNKNOWN until the user runs it on the target PC. |
| Viewer development diagnostics | VERIFIED | The published self-test created a path-free diagnostics ZIP with `viewer-diagnostics.log` and recorder status, and no telemetry file when no dump was open. Local viewer startup, recorder-state changes, and exception type/HRESULT are logged; retention is capped by a 1 MB active log with one previous log. It does not capture MSFS crash stacks. |
| Live tray values and event marking in MSFS | UNKNOWN | The SDK-enabled host, aircraft-state request, and tray request path are implemented but have not been compiled against the user's MSFS SDK or exercised with live simulator frames. User-entered aircraft title remains a fallback. |
| SDK-enabled C# recorder host compile | BLOCKED | The official managed SimConnect DLL is not available in this workspace. The simulator-free target builds; SDK-specific interop signatures and the Windows message loop still require a target-SDK build. |
| Managed-interoperability compile probe | VERIFIED | The SDK-only C# source compiled against a local test double shaped like the documented managed API. This checks conditional C# syntax and host wiring only; the test double is ignored by Git and is not the official SDK. |
| Live connection indicator | UNKNOWN | The local status contract is implemented, but a real SimConnect session has not been run in this environment. |
| Live chart in MSFS | UNKNOWN | The local preview writer/reader and chart are implemented, but no simulator or SDK is installed here to verify live frames or timing. |
| MSFS SDK WASM compile | BLOCKED | The user confirmed that this PC does not have the simulator or SDK. |
| MSFS 2024 live transport | UNKNOWN | Requires the supported gauge/WASM SimConnect pathway and an installed simulator. |
| Crash / disconnect behavior in MSFS | UNKNOWN | Must be exercised using the procedure in `docs/LIVE_TEST_PLAN.md`. |

The terms are intentional: **VERIFIED** means this workspace produced direct evidence; **BLOCKED** means a necessary local prerequisite is absent; **UNKNOWN** means no claim is made.
