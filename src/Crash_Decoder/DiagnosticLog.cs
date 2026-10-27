// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Diagnostics;
using System.Text;
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal static class DiagnosticLog
{
    private const long MaxBytes = 1_000_000;
    private static readonly object Gate = new();
    private static readonly string DirectoryPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MSFS-Input-Blackbox");
    private static readonly string LogPath = Path.Combine(DirectoryPath, "viewer-diagnostics.log");
    private static readonly string PreviousPath = Path.Combine(DirectoryPath, "viewer-diagnostics.previous.log");

    public static void Write(string eventCode, Exception? exception = null)
    {
        // Only controlled event codes, exception types, HRESULTs, and method names go into this log.
        // Exception messages and paths may contain private user data, so they are excluded.
        var safeCode = new string(eventCode.Take(64).Select(ch => char.IsAsciiLetterOrDigit(ch) || ch == '_' ? ch : '_').ToArray());
        var line = new StringBuilder($"{DateTimeOffset.UtcNow:O} | {safeCode}");
        if (exception is not null)
        {
            var method = new StackTrace(exception, false).GetFrames()?.FirstOrDefault()?.GetMethod();
            line.Append($" | {exception.GetType().FullName} | HRESULT 0x{exception.HResult:X8}");
            if (method is not null) line.Append($" | {method.DeclaringType?.FullName}.{method.Name}");
        }
        AppendLine(line.ToString());
    }

    public static void WritePerformance(MonitorStatus status, double viewerCpuCorePercent, long viewerWorkingSetBytes)
    {
        var body = JsonSerializer.Serialize(new
        {
            schema = "msfs-input-blackbox/performance-log/v1",
            capturedAtUtc = DateTimeOffset.UtcNow,
            viewerCpuCorePercent = Math.Round(viewerCpuCorePercent, 2),
            viewerWorkingSetBytes,
            recorderState = status.State,
            status.SessionId,
            recorder = status.Performance,
            note = "EstimatedSamplingGaps uses timestamp intervals and is not confirmed packet loss."
        });
        AppendLine(body);
    }

    private static void AppendLine(string line)
    {
        lock (Gate)
        {
            try
            {
                Directory.CreateDirectory(DirectoryPath);
                if (File.Exists(LogPath) && new FileInfo(LogPath).Length >= MaxBytes)
                    File.Move(LogPath, PreviousPath, true);
                File.AppendAllText(LogPath, line + Environment.NewLine, Encoding.UTF8);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { /* Diagnostics must never interrupt the viewer or a crash handler. */ }
        }
    }

    public static string ReadForExport()
    {
        lock (Gate)
        {
            try { return File.Exists(LogPath) ? File.ReadAllText(LogPath) : "No viewer diagnostics have been recorded."; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            { return "Viewer diagnostics were not readable."; }
        }
    }
}
