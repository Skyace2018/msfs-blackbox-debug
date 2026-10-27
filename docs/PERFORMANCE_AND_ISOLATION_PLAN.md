# Performance and simulator isolation plan

Status: design targets, not live-verified results. The current portable ZIP contains the viewer, not a validated live recorder. No other simulator adapter has been implemented or tested.

## Boundaries for additional simulators

Use one opt-in adapter per simulator, built against that simulator's supported SDK or read-only data interface. Keep the core ring buffer, file format, and viewer independent of the adapter. Put simulator identity, adapter version, session ID, and time-base details in a session envelope rather than silently changing the existing 36-byte MSFS frame. Activate only the selected simulator adapter. Do not install global keyboard hooks, inject into unrelated processes, expose a network listener, or write simulator controls. Keep local status and exports under the current user's profile and require an explicit action to share them.

The simulator callback should only sample the selected inputs and enqueue a compact frame. No disk writes, ZIP compression, full-buffer copies, waiting for the viewer, or synchronous network calls belong on that callback. Each adapter should have a measured callback budget and an explicit unsubscribe/cleanup path.

## Current implementation risks to address before live multi-simulator testing

1. `LiveRecorderHost.Tick` runs on a Windows Forms timer attached to the SimConnect message-loop thread. It calls `EventCaptureService.ProcessRequests`, which can copy the entire ring and write a JSON ZIP. A slow disk or a one-hour export can delay receiving frames. Move export work to a bounded background worker. Take a short, deliberate snapshot boundary and report the marker time separately from the snapshot time.
2. The same timer calls `LivePreviewStore.Publish`, which allocates a frame array, serializes up to 240 frames, and rewrites `live-preview.json` every 250 ms while frames change. Move preview production off the receive thread and rate-limit it. The viewer should skip file reads and redraws when hidden or on another page.
3. `TelemetryRingBuffer.SnapshotOldestFirst` holds a lock while copying all retained frames. At the maximum configured 3600 seconds × 60 Hz, the raw 36-byte array is about 7.4 MiB, before JSON/export allocations. Measure the lock duration and use a snapshot strategy that keeps the receive path within budget.
4. The v1 frame has a millisecond timestamp but no sequence number. Timestamp gaps can suggest missed samples but cannot distinguish all losses. A future version should add an adapter-side sequence number in the session envelope or protocol, count gaps, and show them in exports instead of implying continuous coverage.

## Proposed capture behavior

- Keep the fixed-capacity rolling buffer and a default 30 Hz accepted sample rate; let users increase to 60 Hz only after target-PC testing. High-frequency reception may occur, but accepted frames are rate-limited.
- Separate priorities: receive and ring append first; status and live chart second; compression and disk export last. Bounded worker queues must never stall the simulator callback. If a display update is dropped, count it. If an input frame is missed, record the gap in session diagnostics.
- Keep the viewer's plotted sample count bounded and redraw at a human-readable rate. Do not render every simulator frame or keep redrawing a hidden chart.
- Expose health counters: received/accepted frames, estimated missing samples, queue depth, callback duration, export duration, disk-write failures, and connection state. Avoid per-frame text logging.

MSFS SimConnect supports `CHANGED` client-data requests and an interval measured in period events; tune these against observed behavior instead of assuming visual-frame delivery is free. An X-Plane adapter would use its documented flight-loop callbacks and should avoid heavy work in those callbacks. See [MSFS request documentation](https://docs.flightsimulator.com/msfs2024/retail/programming-apis/simconnect/api-reference/events-and-data/simconnect_requestclientdata/) and [X-Plane flight-loop API](https://developer.x-plane.com/sdk/XPLMProcessing/).

## Release gate: measure on the target PC

Run paired 30-minute sessions of each simulator alone, with the adapter, with the viewer, and with a representative set of other add-ons. Repeat in a busy airport and in cruise, with identical graphics settings and an aircraft suitable for the target adapter. Record at least median, 95th, and 99th percentile frame time; recorder CPU and memory; .NET allocation/GC rate; callback duration; dropped or missing samples; export duration; and disk throughput. Use the simulator's frame-time tools and [.NET counters](https://learn.microsoft.com/en-us/dotnet/core/diagnostics/dotnet-counters) for process-level data.

Provisional targets to validate and adjust with measurements: callback 99th percentile under 0.25 ms, no callback over 1 ms during a normal 30-minute run, and no reproducible simulator 95th-percentile frame-time regression beyond 2% relative to repeated baseline runs. A marker export must not interrupt incoming frames. On an overloaded PC, the product should degrade the live chart before evidence capture, show an explicit gap counter if capture still misses frames, and never claim a gap-free recording without evidence.

Do not mark any live performance gate VERIFIED until a target-PC run produces the measurements. The simulator-specific transport and interoperability tests in [LIVE_TEST_PLAN.md](LIVE_TEST_PLAN.md) remain required.
