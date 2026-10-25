# Test status

Date: 2026-10-25

| Phase | Status | Evidence / limitation |
| --- | --- | --- |
| Repository static validation | VERIFIED | C++/C# frame contract is documented as 36 bytes; source contains matching packed layouts and assertions. Secret-pattern scan found no credential-bearing files. |
| .NET recorder build | VERIFIED | `dotnet build src/External_Recorder/External_Recorder.csproj --no-restore` succeeded with 0 warnings on .NET SDK 10.0.401, targeting .NET 8. |
| .NET recorder synthetic self-test | VERIFIED | `dotnet run --project src/External_Recorder --no-build -- --self-test` passed: 36-byte layout, three newest frames retained in order, and JSON dump emitted with telemetry fields. |
| Decoder UI build / parser self-test | VERIFIED | `dotnet build src/Crash_Decoder/Crash_Decoder.csproj --no-restore` compiled the Windows Forms viewer, and its `--self-test` parsed a synthetic v1 JSON dump containing two frames. |
| MSFS SDK WASM compile | BLOCKED | No MSFS SDK/toolchain was available in this environment. |
| MSFS 2024 live transport | UNKNOWN | Requires the supported gauge/WASM SimConnect pathway and an installed simulator. |
| Crash / disconnect behavior in MSFS | UNKNOWN | Must be exercised using the procedure in `docs/LIVE_TEST_PLAN.md`. |

The terms are intentional: **VERIFIED** means this workspace produced direct evidence; **BLOCKED** means a necessary local prerequisite is absent; **UNKNOWN** means no claim is made.

