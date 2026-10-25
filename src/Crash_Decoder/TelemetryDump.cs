// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

public sealed class TelemetryDump
{
    public required string Schema { get; init; }
    public required string Reason { get; init; }
    public DateTimeOffset CapturedAtUtc { get; init; }
    public required IReadOnlyList<TelemetrySample> Frames { get; init; }
}

public sealed class TelemetrySample
{
    public uint Timestamp { get; init; }
    public float YokeX { get; init; }
    public float YokeY { get; init; }
    public float Rudder { get; init; }
    public float Throttle { get; init; }
    public uint Switches0 { get; init; }
    public uint Switches1 { get; init; }
    public uint Switches2 { get; init; }
    public uint Switches3 { get; init; }
}

public static class TelemetryDumpReader
{
    public const string SchemaV1 = "msfs-input-blackbox/v1";

    public static TelemetryDump Read(string path)
    {
        using var document = JsonDocument.Parse(File.ReadAllText(path));
        var root = document.RootElement;
        var schema = RequiredString(root, "schema");
        if (!string.Equals(schema, SchemaV1, StringComparison.Ordinal))
            throw new InvalidDataException($"Unsupported dump schema '{schema}'. Expected '{SchemaV1}'.");
        var frames = root.GetProperty("frames").EnumerateArray().Select(ReadFrame).ToArray();
        if (frames.Length == 0) throw new InvalidDataException("The dump contains no telemetry frames.");
        return new TelemetryDump
        {
            Schema = schema,
            Reason = RequiredString(root, "reason"),
            CapturedAtUtc = root.GetProperty("capturedAtUtc").GetDateTimeOffset(),
            Frames = frames
        };
    }

    private static TelemetrySample ReadFrame(JsonElement frame) => new()
    {
        Timestamp = RequiredUInt(frame, "Timestamp"),
        YokeX = RequiredSingle(frame, "YokeX"),
        YokeY = RequiredSingle(frame, "YokeY"),
        Rudder = RequiredSingle(frame, "Rudder"),
        Throttle = RequiredSingle(frame, "Throttle"),
        Switches0 = RequiredUInt(frame, "Switches0"),
        Switches1 = RequiredUInt(frame, "Switches1"),
        Switches2 = RequiredUInt(frame, "Switches2"),
        Switches3 = RequiredUInt(frame, "Switches3")
    };

    private static string RequiredString(JsonElement item, string name) => item.GetProperty(name).GetString()
        ?? throw new InvalidDataException($"'{name}' cannot be null.");
    private static uint RequiredUInt(JsonElement item, string name) => item.GetProperty(name).GetUInt32();
    private static float RequiredSingle(JsonElement item, string name)
    {
        var value = item.GetProperty(name).GetSingle();
        if (!float.IsFinite(value)) throw new InvalidDataException($"'{name}' must be finite.");
        return value;
    }
}

