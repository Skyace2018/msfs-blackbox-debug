# BlueBird 757 five-minute test card

Status: planned, not yet run. This is a first smoke test, not a performance certification.

## First: establish which test is possible

The current Google Drive ZIP contains the WPF viewer only. It cannot receive live MSFS data by itself. On the test PC, run the SDK preflight from a copy of this repository (`scripts/Test-LivePrerequisites.ps1`) against the official managed SimConnect DLL. A successful preflight proves that the conditional C# recorder compiles; it does **not** prove that an in-simulator WASM sender has been built or installed. Follow `LIVE_TEST_PLAN.md` for that separate transport check. If the sender is absent, stop the live checklist and use the viewer-only checklist below. Record **BLOCKED: sender unavailable**, rather than reporting an empty Live View as a failed aircraft test.

The [BlueBird 757 public Simvars list](https://support.bluebirdsim.com/docs/simvars) publishes aircraft-specific L-Vars for third-party integrations. The present Blackbox v1 frame does **not** capture those L-Vars. It captures standard yoke X/Y, rudder, throttle, and four switch words; the recorder separately requests the user-aircraft title, indicated airspeed, and altitude. Test the basic input transport first. Do not interpret absent BlueBird-specific switch values as a 757 failure.

## If the SDK-enabled recorder and sender are working

Before loading the aircraft, record the MSFS build, BlueBird 757 version, test airport, major add-ons, and controller type. In Blackbox **Live recording**, enable input logging, select **300 seconds** of history and **30 Hz**, then restart the recorder so it loads the setting. Choose a folder under **Settings → Developer handoff**. Confirm the UI says **Connected** and the latest input timestamp advances; otherwise stop and record the preflight result.

| Time | Action | Expected evidence |
| --- | --- | --- |
| 0:00–1:00 | Load the BlueBird 757 and leave controls mostly still. | Recorder session ID and advancing timestamps; note baseline frame time. |
| 1:00–2:00 | Move yoke left/right and forward/back in smooth, distinct sweeps. | Yoke X/Y traces change in Live View. |
| 2:00–3:00 | Move rudder and throttle through distinct positions. | Rudder and throttle traces change without obvious UI stalls. |
| 3:00–4:00 | Keep flying normally; note any stutter, connection warning, or performance spike. | Recorder counters continue increasing; note approximate frame time. |
| 4:00–5:00 | While moving one control, select **Mark live event + export**. Wait for the saved-file confirmation. | One new event ZIP in the chosen folder; no input interruption during export. |

Open the event ZIP after the run. Check `telemetry.json`, `event.json`, `manifest.json`, `performance.json`, and `README.txt`; compare the marker time, final input, aircraft title source, received/accepted/rate-limited counts, and maximum input-handler time with what you observed. `EstimatedSamplingGaps` is based on timestamp intervals and is **not** confirmed packet loss. Do not change or write BlueBird L-Vars for this read-only smoke test.

If the event button is disabled or the ZIP is missing, save the recorder preflight output, a screenshot of the Blackbox status, and any local diagnostic export. Do not upload private flight paths or settings publicly without reviewing them.

## If only the current viewer ZIP is available

Open `Crash_Decoder.exe` beside MSFS, load the included `SAMPLE-synthetic-telemetry.json`, check chart/table responsiveness, then select **Export developer bundle**. That ZIP proves the viewer's saved-dump/export workflow only. The sample is synthetic, and **Recorder not running** is expected. It does not test BlueBird inputs or live SimConnect transport.

## After this smoke test

Record the result as VERIFIED, UNKNOWN, or BLOCKED in `TEST_STATUS.md` with the exact evidence. For frame-time impact and coexistence with other add-ons, use the repeated 30-minute comparison in `PERFORMANCE_AND_ISOLATION_PLAN.md`. Build the in-app Help steps after this test so the setup, logging, and especially developer-export instructions match the behavior users actually encounter.
