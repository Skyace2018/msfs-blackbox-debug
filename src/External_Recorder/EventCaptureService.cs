// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.IO.Compression;
using System.Text.Json;

namespace MsfsInputBlackbox;

internal sealed class EventCaptureRequest
{
    public Guid Id { get; init; }
    public Guid SessionId { get; init; }
    public DateTimeOffset MarkedAtUtc { get; init; }
    public string? OutputPath { get; init; }
    public string? AircraftProfile { get; init; }
    public string? AircraftType { get; init; }
    public string? AircraftSource { get; init; }
}

internal sealed class EventCaptureResult
{
    public Guid Id { get; init; }
    public bool Success { get; init; }
    public string? OutputPath { get; init; }
    public string? Error { get; init; }
}

internal sealed class EventCaptureService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, IncludeFields = true };
    private readonly TelemetryRingBuffer _ring;
    private readonly string _requestDirectory;
    private readonly string _resultDirectory;
    private readonly Guid _sessionId = Guid.NewGuid();
    private TelemetryFrame? _latest;

    public EventCaptureService(int capacity, string dataDirectory)
    {
        _ring = new TelemetryRingBuffer(capacity);
        _requestDirectory = Path.Combine(dataDirectory, "event-requests");
        _resultDirectory = Path.Combine(dataDirectory, "event-results");
        Directory.CreateDirectory(_requestDirectory);
        Directory.CreateDirectory(_resultDirectory);
    }

    public int Count => _ring.Count;
    public int Capacity => _ring.Capacity;
    public TelemetryFrame? Latest => _latest;
    public Guid SessionId => _sessionId;
    public TelemetryFrame[] Snapshot() => _ring.SnapshotOldestFirst();

    public void AddFrame(TelemetryFrame frame)
    {
        _ring.Add(frame);
        _latest = frame;
    }

    public void ProcessRequests()
    {
        foreach (var readyPath in Directory.EnumerateFiles(_requestDirectory, "*.ready"))
        {
            var path = Path.ChangeExtension(readyPath, ".json");
            EventCaptureRequest? request = null;
            try
            {
                request = JsonSerializer.Deserialize<EventCaptureRequest>(File.ReadAllText(path));
                if (request is null || request.Id == Guid.Empty || string.IsNullOrWhiteSpace(request.OutputPath))
                    throw new InvalidDataException("The event request is incomplete.");
                if (request.SessionId != _sessionId)
                    throw new InvalidDataException("The marked session has ended; no event export was created.");
                var frames = _ring.SnapshotOldestFirst();
                if (frames.Length == 0) throw new InvalidDataException("No telemetry frames have arrived yet.");
                WriteBundle(request, frames);
                WriteResult(new EventCaptureResult { Id = request.Id, Success = true, OutputPath = request.OutputPath });
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidDataException or JsonException)
            {
                if (request?.Id is { } id && id != Guid.Empty)
                    WriteResult(new EventCaptureResult { Id = id, Success = false, Error = ex.Message });
            }
            finally
            {
                try { File.Delete(path); File.Delete(readyPath); }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException) { }
            }
        }
    }

    private void WriteBundle(EventCaptureRequest request, TelemetryFrame[] frames)
    {
        var destination = Path.GetFullPath(request.OutputPath!);
        Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
        var stage = "open output archive";
        var created = false;
        try
        {
            using (var stream = new FileStream(destination, FileMode.CreateNew, FileAccess.Write, FileShare.None))
            using (var zip = new ZipArchive(stream, ZipArchiveMode.Create))
            {
                created = true;
                stage = "write archive entries";
                var last = frames[^1];
                var capturedAtUtc = DateTimeOffset.UtcNow;
                WriteJson(zip, "telemetry.json", new
                {
                    schema = "msfs-input-blackbox/v1", reason = "user-marked-event",
                    capturedAtUtc, frames
                });
                WriteJson(zip, "event.json", new
                {
                    schema = "msfs-input-blackbox/event/v1", eventId = request.Id,
                    markedAtUtc = request.MarkedAtUtc, capturedAtUtc,
                    sessionId = _sessionId, lastBufferedFrameTimestampMs = last.Timestamp,
                    lastInputs = last, aircraftType = request.AircraftType,
                    aircraftIdentityStatus = string.IsNullOrWhiteSpace(request.AircraftType) ?
                        "Unavailable: the v1 telemetry transport does not include aircraft identity." : request.AircraftSource switch
                    {
                        "recorder-reported" => "Reported by the external recorder; live identity verification remains pending.",
                        "user-entered" => "User-entered aircraft title; not verified by the live telemetry transport.",
                        _ => "Aircraft title supplied without a verified source."
                    },
                    aircraftProfile = request.AircraftProfile,
                    variablesCaptured = new[] { "YokeX", "YokeY", "Rudder", "Throttle", "Switches0", "Switches1", "Switches2", "Switches3" },
                    additionalSimulatorVariablesStatus = "Not captured by the v1 transport; airspeed, altitude, and engine state are unavailable."
                });
                WriteJson(zip, "manifest.json", new
                {
                    schema = "msfs-input-blackbox/developer-export/v2", eventId = request.Id,
                    markedAtUtc = request.MarkedAtUtc, capturedAtUtc, frameCount = frames.Length,
                    firstTimestampMs = frames[0].Timestamp, lastTimestampMs = last.Timestamp,
                    note = "Frame timestamps are simulator monotonic milliseconds. markedAtUtc is the user click time; the buffer snapshot is taken when the recorder processes the request."
                });
                using var notes = new StreamWriter(zip.CreateEntry("README.txt").Open());
                notes.WriteLine("MSFS Input Blackbox event export");
                notes.WriteLine("telemetry.json contains the rolling inputs captured when the event request was processed.");
                notes.WriteLine("event.json contains the user timestamp, latest inputs, and available aircraft metadata.");
                notes.WriteLine("Aircraft title is user-entered when present; the v1 telemetry transport does not verify it.");
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            if (created) { try { File.Delete(destination); } catch (IOException) { } }
            throw new IOException("Could not " + stage + ": " + ex.Message, ex);
        }
    }

    private void WriteResult(EventCaptureResult result)
    {
        var path = Path.Combine(_resultDirectory, result.Id.ToString("N") + ".json");
        File.WriteAllText(path, JsonSerializer.Serialize(result, JsonOptions));
        File.WriteAllText(Path.ChangeExtension(path, ".ready"), "");
    }

    private static void WriteJson(ZipArchive zip, string name, object body)
    {
        using var writer = new StreamWriter(zip.CreateEntry(name).Open());
        writer.Write(JsonSerializer.Serialize(body, JsonOptions));
    }
}
