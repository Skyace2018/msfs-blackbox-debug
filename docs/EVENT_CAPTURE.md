# Live event markers and developer exports

The desktop viewer and external recorder run as separate processes. The recorder publishes a local heartbeat with session ID, buffer count, latest frame, and update time. The tray shows **LIVE TELEMETRY CONNECTED** only when that heartbeat is fresh and includes a frame. A stale heartbeat never counts as a confirmed live session.

## Mark an event

1. Start the SDK-enabled external recorder and confirm that it is receiving frames.
2. Open the Blackbox tray panel and select **Mark event + export**. The same action is available on the main dashboard.
3. Select an export folder on first use. Later marks use the folder saved under **Settings → Developer handoff**.
4. The viewer writes a request for the current session. The recorder polls requests every 250 ms, snapshots its rolling buffer, and writes a uniquely named developer ZIP. The tray confirms the saved path or reports an error.

The event's `markedAtUtc` is recorded when the request is queued. `capturedAtUtc` is when the recorder snapshots the buffer. Frame `Timestamp` values are monotonic milliseconds from the telemetry transport; they are **not** UTC. The live host loads the viewer's history and sample-rate preferences when the recorder starts, with a maximum of one hour at 60 Hz. Restart the recorder after changing those settings. The ZIP includes the buffer available when the request is processed, not an exact reconstruction of an earlier click if the recorder was delayed.

The request includes the live session ID, so a later recorder session cannot turn an old request into a misleading new-session export. The UI keeps the event button disabled while it is waiting for confirmation. A session without incoming frames cannot be marked.

## ZIP contents

| File | Contents |
| --- | --- |
| `telemetry.json` | Ordered buffered frames with yoke X/Y, rudder, throttle, four switch words, and millisecond timestamps. |
| `event.json` | Event ID, UTC marker and capture times, session ID, last buffered inputs, captured-variable names, optional aircraft title, indicated airspeed/altitude when received, and identity-source note. |
| `manifest.json` | Export schema, frame count, time range, and timestamp interpretation. |
| `performance.json` | Aggregate received, accepted, and rate-limited frame counts; estimated sampling gaps; and maximum recorder input-handler time. Present in exports from the SDK-enabled host. |
| `README.txt` | Short explanation for the receiving developer. |

`EstimatedSamplingGaps` is inferred from timestamp intervals against the configured accepted sample rate. It is not a confirmed packet-loss count; a slow simulator frame rate can also create an interval. The counters are updated in memory per frame, with no per-frame diagnostic file writes.

The 36-byte input frame does not contain aircraft identity, airspeed, altitude, or engine state. The SDK-enabled recorder now has a separate user-aircraft SimConnect request for title, indicated airspeed, and plane altitude. When those values arrive, `event.json` identifies them as recorder-reported and includes the observation time. This request has only passed a local compile probe using a test double; it has **not** been built against the official SDK or tested in MSFS. An aircraft title entered under **Aircraft profiles** remains a fallback and is labeled **user-entered**. Engine state is not captured. The [MSFS 2024 SimConnect request documentation](https://docs.flightsimulator.com/msfs2024/flighting/programming-apis/simconnect/api-reference/events-and-data/simconnect_requestdataonsimobject/) describes querying the user aircraft.

Exports include selected control telemetry and the optional aircraft label. They do not include Community-folder paths, account data, SDK binaries, or simulator packages. Review telemetry before sending a ZIP to a developer.

## Verification status

The simulator-free self-test verifies a request produces a ZIP with the marker, last input, captured-variable list, aircraft title, indicated airspeed, altitude, and performance counters from synthetic input. Live SimConnect capture, automatic aircraft identification, timeout behavior, and tray confirmation require the Windows/MSFS procedure in [LIVE_TEST_PLAN.md](LIVE_TEST_PLAN.md). Do not treat a synthetic event export as proof of a live flight recording.
