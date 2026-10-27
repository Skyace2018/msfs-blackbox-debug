// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
#if MSFS_SIMCONNECT
using System.Diagnostics;
using System.Windows.Forms;

namespace MsfsInputBlackbox;

internal static class LiveRecorderHost
{
    private const int SimConnectMessage = 0x0400 + 0x4B;
    private static readonly string DataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSFS-Input-Blackbox");

    public static int Run()
    {
        try
        {
            Application.Run(new RecorderContext());
            return 0;
        }
        catch (Exception ex)
        {
            MonitorStatusStore.Report("disconnected", "Recorder could not open a SimConnect session: " + ex.Message);
            Console.Error.WriteLine(ex);
            return 1;
        }
    }

    private sealed class RecorderContext : ApplicationContext
    {
        private readonly MessageWindow _window;
        private readonly SimConnectMonitor _monitor;
        private readonly EventCaptureService _capture;
        private readonly RecorderPreferences _preferences;
        private readonly RecorderPerformance _performance;
        private readonly LivePreviewStore _preview;
        private readonly RecorderIoWorkers _ioWorkers;
        private readonly System.Windows.Forms.Timer _timer;
        private DateTimeOffset _lastFrameUtc;
        private DateTimeOffset _lastReportUtc;
        private bool _hasReceivedFrame;
        private bool _ending;
        private uint? _lastAcceptedTimestamp;
        private AircraftState? _aircraftState;

        public RecorderContext()
        {
            Directory.CreateDirectory(DataDirectory);
            _preferences = RecorderPreferences.Load(DataDirectory);
            _performance = new RecorderPerformance(_preferences.SampleRateHz);
            _capture = new EventCaptureService(_preferences.SampleRateHz * _preferences.HistorySeconds,
                DataDirectory, _performance.Snapshot);
            _preview = new LivePreviewStore(DataDirectory);
            _window = new MessageWindow(SimConnectMessage);
            _monitor = new SimConnectMonitor(_window.Handle, SimConnectMessage);
            _window.ReceiveMessage = _monitor.ReceiveMessage;
            _monitor.FrameReceived += OnFrame;
            _monitor.AircraftStateReceived += OnAircraftState;
            _monitor.Disconnected += OnDisconnected;
            _ioWorkers = new RecorderIoWorkers(_capture.ProcessRequests, () =>
            {
                if (_capture.Latest is { } frame) _preview.Publish(_capture.SessionId, frame);
            }, ex => MonitorStatusStore.Report("error", "Recorder background I/O failed: " + ex.GetType().Name));
            _timer = new System.Windows.Forms.Timer { Interval = 250 };
            _timer.Tick += (_, _) => Tick();
            _timer.Start();
            MonitorStatusStore.Report("connecting", "SimConnect opened; waiting for the first telemetry frame.",
                bufferCapacity: _capture.Capacity, sessionId: _capture.SessionId);
        }

        private void OnFrame(TelemetryFrame frame)
        {
            var startedAtTicks = Stopwatch.GetTimestamp();
            try
            {
                _performance.Received(frame.Timestamp);
                if (!_preferences.LoggingEnabled) return;
                if (_lastAcceptedTimestamp is { } previous &&
                    unchecked(frame.Timestamp - previous) < 1000u / (uint)_preferences.SampleRateHz)
                {
                    _performance.RateLimited();
                    return;
                }
                _lastAcceptedTimestamp = frame.Timestamp;
                _capture.AddFrame(frame);
                _performance.Accepted();
                _lastFrameUtc = DateTimeOffset.UtcNow;
                _hasReceivedFrame = true;
            }
            finally { _performance.HandlerFinished(startedAtTicks); }
        }

        private void OnAircraftState(AircraftState state)
        {
            _aircraftState = state;
            _capture.SetAircraftState(state);
        }

        private void Tick()
        {
            var now = DateTimeOffset.UtcNow;
            if (_hasReceivedFrame && now - _lastFrameUtc > TimeSpan.FromSeconds(5))
            {
                EndSession("telemetry-timeout");
                return;
            }
            if (now - _lastReportUtc < TimeSpan.FromSeconds(2)) return;
            _lastReportUtc = now;
            var latest = _capture.Latest;
            var aircraft = _aircraftState;
            var hasReceivedFrame = _hasReceivedFrame;
            var lastFrameUtc = _lastFrameUtc;
            var count = _capture.Count;
            var capacity = _capture.Capacity;
            var sessionId = _capture.SessionId;
            var performance = _performance.Snapshot();
            _ioWorkers.PostStatus(() => MonitorStatusStore.Report(hasReceivedFrame ? "connected" : "connecting",
                hasReceivedFrame ? "Receiving live telemetry frames." :
                    _preferences.LoggingEnabled ? "Waiting for the first telemetry frame." : "Input logging is disabled in viewer settings.",
                latest?.Timestamp, latest, count, capacity, sessionId,
                aircraftType: aircraft?.Title, lastFrameReceivedAtUtc: hasReceivedFrame ? lastFrameUtc : null,
                indicatedAirspeedKnots: aircraft?.IndicatedAirspeedKnots, altitudeFeet: aircraft?.AltitudeFeet,
                performance: performance));
        }

        private void OnDisconnected(string reason) => EndSession(reason);

        private void EndSession(string reason)
        {
            if (_ending) return;
            _ending = true;
            _timer.Stop();
            _ioWorkers.Dispose();
            var frames = _capture.Snapshot();
            var finalDetail = reason;
            if (frames.Length > 0 && _preferences.KeepCrashDumps)
            {
                var name = "crash_telemetry_dump_" + DateTimeOffset.UtcNow.ToString("yyyyMMdd_HHmmss") + ".json";
                try { Program.Dump(Path.Combine(DataDirectory, name), reason, frames); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                { finalDetail += "; dump could not be saved: " + ex.Message; }
            }
            MonitorStatusStore.Report("disconnected", finalDetail, bufferedFrames: _capture.Count,
                bufferCapacity: _capture.Capacity, sessionId: _capture.SessionId,
                performance: _performance.Snapshot());
            ExitThread();
        }

        protected override void ExitThreadCore()
        {
            _timer.Dispose();
            _ioWorkers.Dispose();
            _monitor.Dispose();
            _window.DestroyHandle();
            base.ExitThreadCore();
        }
    }

    private sealed class MessageWindow : NativeWindow
    {
        private readonly int _messageId;
        public Action? ReceiveMessage { get; set; }

        public MessageWindow(int messageId)
        {
            _messageId = messageId;
            CreateHandle(new CreateParams { Caption = "MSFS Input Blackbox SimConnect host" });
        }

        protected override void WndProc(ref Message message)
        {
            if (message.Msg == _messageId) ReceiveMessage?.Invoke();
            base.WndProc(ref message);
        }
    }
}
#endif
