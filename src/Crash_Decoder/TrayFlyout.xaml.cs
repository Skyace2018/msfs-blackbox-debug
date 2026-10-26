// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Windows;

namespace MsfsInputBlackbox.Decoder;

public partial class TrayFlyout : Window
{
    private readonly Action _restore;
    private readonly Action _exit;
    private readonly Action _markEvent;

    public TrayFlyout(Action restore, Action exit, Action markEvent)
    {
        InitializeComponent();
        _restore = restore;
        _exit = exit;
        _markEvent = markEvent;
        Deactivated += (_, _) => Hide();
    }

    internal void UpdateDashboard(MonitorStatus status, string captureMessage, bool capturePending, string? configuredAircraftTitle)
    {
        var connected = status.State == "connected" && status.LatestInput is not null && status.SessionId is not null;
        StatusText.Text = connected ? "● LIVE TELEMETRY CONNECTED" : status.State switch
        {
            "connecting" => "● CONNECTING — waiting for input",
            "disconnected" => "● SESSION DISCONNECTED",
            _ => "● NO CONFIRMED LIVE SESSION"
        };
        SessionText.Text = status.Detail + (status.UpdatedAtUtc == default ? "" :
            "  •  Updated " + status.UpdatedAtUtc.ToLocalTime().ToString("HH:mm:ss"));
        AircraftText.Text = !string.IsNullOrWhiteSpace(status.AircraftType) ? "Aircraft: " + status.AircraftType
            : !string.IsNullOrWhiteSpace(configuredAircraftTitle) ? "Aircraft: " + configuredAircraftTitle + " (user-entered)"
            : "Aircraft: Not reported by current telemetry";
        if (status.IndicatedAirspeedKnots is { } airspeed && status.AltitudeFeet is { } altitude)
            AircraftText.Text += $"\nIAS {airspeed:F1} kt  •  Altitude {altitude:F0} ft";
        var frame = status.LatestInput;
        InputText.Text = frame is null ? "Last inputs: waiting for frames" :
            $"Last inputs  •  Yoke {frame.YokeX:F2}, {frame.YokeY:F2}  •  Rudder {frame.Rudder:F2}  •  Throttle {frame.Throttle:F2}\nSwitches: {frame.Switches0:X8} {frame.Switches1:X8} {frame.Switches2:X8} {frame.Switches3:X8}";
        BufferText.Text = status.BufferedFrames is { } count && status.BufferCapacity is { } capacity
            ? $"Rolling history: {count:N0} / {capacity:N0} frames  •  Frame time {status.LastFrameTimestamp} ms"
            : "Rolling history: not available";
        CaptureText.Text = string.IsNullOrWhiteSpace(captureMessage)
            ? "Mark event creates a timestamped developer ZIP from the recorder's current buffer."
            : captureMessage;
        MarkButton.IsEnabled = connected && !capturePending;
    }

    public void ShowNearTaskbar()
    {
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 14;
        Top = workArea.Bottom - Height - 14;
        Show();
        Activate();
    }

    private void MarkEvent_Click(object sender, RoutedEventArgs e) => _markEvent();
    private void Restore_Click(object sender, RoutedEventArgs e) { Hide(); _restore(); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => Hide();
    private void Exit_Click(object sender, RoutedEventArgs e) { Hide(); _exit(); }
}
