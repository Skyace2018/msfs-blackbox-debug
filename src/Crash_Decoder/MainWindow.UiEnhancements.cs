// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;

namespace MsfsInputBlackbox.Decoder;

public partial class MainWindow
{
    private readonly DispatcherTimer _previewTimer = new() { Interval = TimeSpan.FromMilliseconds(300) };
    private TelemetrySample[] _timelineFrames = [];
    private bool _sidebarCollapsed;
    private bool _timelineFocused;
    private bool _isLiveTimeline;
    private double _expandedSidebarWidth = 260;
    private DateTimeOffset _actionFeedbackUntilUtc;

    private void InitializeUiEnhancements()
    {
        _previewTimer.Tick += (_, _) => UpdateLiveTimeline();
        ShowDumpTimeline();
    }

    private void ToggleSidebar_Click(object sender, RoutedEventArgs e)
    {
        if (!_sidebarCollapsed && SidebarColumn.ActualWidth > 180) _expandedSidebarWidth = SidebarColumn.ActualWidth;
        _sidebarCollapsed = !_sidebarCollapsed;
        ApplySidebarLayout();
    }

    private void ApplySidebarLayout()
    {
        if (_timelineFocused)
        {
            SidebarPanel.Visibility = Visibility.Collapsed;
            SidebarSplitter.Visibility = Visibility.Collapsed;
            SidebarColumn.Width = new GridLength(0);
            SidebarSplitColumn.Width = new GridLength(0);
            return;
        }
        SidebarPanel.Visibility = Visibility.Visible;
        SidebarColumn.Width = new GridLength(_sidebarCollapsed ? 64 : Math.Max(220, _expandedSidebarWidth));
        SidebarSplitColumn.Width = new GridLength(_sidebarCollapsed ? 0 : 6);
        SidebarSplitter.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        BrandIcon.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        BrandText.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarStatusBorder.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarVersion.Visibility = _sidebarCollapsed ? Visibility.Collapsed : Visibility.Visible;
        SidebarTop.Margin = _sidebarCollapsed ? new Thickness(10, 18, 10, 8) : new Thickness(12, 20, 12, 10);
        ToggleSidebarButton.ToolTip = _sidebarCollapsed ? "Expand navigation" : "Collapse navigation to icons";
        SetNavContent(DashboardNav, "⌂", "⌂   Dashboard");
        SetNavContent(LiveNav, "●", "●   Live recording");
        SetNavContent(AircraftNav, "✈", "✈   Aircraft profiles");
        SetNavContent(TablesNav, "▤", "▤   Telemetry tables");
        SetNavContent(SettingsNav, "⚙", "⚙   Settings");
    }

    private void SetNavContent(System.Windows.Controls.Button button, string icon, string expanded)
    {
        button.Content = _sidebarCollapsed ? icon : expanded;
        button.HorizontalContentAlignment = _sidebarCollapsed ? System.Windows.HorizontalAlignment.Center : System.Windows.HorizontalAlignment.Left;
        button.FontSize = _sidebarCollapsed ? 20 : 14;
        button.Padding = _sidebarCollapsed ? new Thickness(8) : new Thickness(14, 11, 14, 11);
        button.Margin = _sidebarCollapsed ? new Thickness(6, 3, 6, 3) : new Thickness(8, 2, 8, 2);
    }

    private void FocusTimeline_Click(object sender, RoutedEventArgs e) => SetTimelineFocus(!_timelineFocused);

    private void SetTimelineFocus(bool focused)
    {
        _timelineFocused = focused;
        DashboardCards.Visibility = focused ? Visibility.Collapsed : Visibility.Visible;
        QuickActionsCard.Visibility = focused ? Visibility.Collapsed : Visibility.Visible;
        TimelineCard.Height = focused ? Math.Max(420, ActualHeight - 200) : 380;
        FocusTimelineButton.Content = focused ? "Exit full view" : "Expand";
        FocusTimelineButton.ToolTip = focused ? "Restore dashboard cards and navigation" : "Give the input timeline most of the window";
        PageTitle.Text = focused ? "Input timeline" : "Dashboard";
        PageSubtitle.Text = focused ? "Focused input view • use Exit full view to return" : "Review recent inputs and recorder health.";
        ApplySidebarLayout();
        Dispatcher.BeginInvoke(() => DrawTimeline(), DispatcherPriority.Loaded);
    }

    private void LiveViewToggle_Click(object sender, RoutedEventArgs e)
    {
        _isLiveTimeline = !_isLiveTimeline;
        LiveViewToggle.Content = _isLiveTimeline ? "Live View: On" : "Live View: Off";
        TimelineTitle.Text = _isLiveTimeline ? "Input timeline • LIVE" : "Input timeline";
        if (_isLiveTimeline)
        {
            _previewTimer.Start();
            UpdateLiveTimeline();
        }
        else
        {
            _previewTimer.Stop();
            ShowDumpTimeline();
        }
    }

    private void UpdateLiveTimeline()
    {
        if (!_isLiveTimeline) return;
        var status = MonitorStatusReader.Read();
        var preview = status.State == "connected" ? LivePreviewReader.Read(status.SessionId) : null;
        if (preview is null)
        {
            SetTimelineFrames([], "Waiting for current live telemetry. The external recorder must be receiving frames.",
                "Live View • no confirmed current session");
            return;
        }
        SetTimelineFrames(preview.Frames, "Collecting live input samples…",
            $"Live View • {preview.Frames.Length} display samples • updated {preview.UpdatedAtUtc.ToLocalTime():HH:mm:ss}");
    }

    private void ShowDumpTimeline()
    {
        if (_isLiveTimeline) return;
        if (_loadedDump is null)
        {
            SetTimelineFrames([], "Open a telemetry dump to plot the final 60 seconds of inputs.", "Offline dump view • final 60 seconds");
            return;
        }
        var frames = _loadedDump.Frames;
        if (frames.Count == 0)
        {
            SetTimelineFrames([], "This dump has no input frames.", "Saved dump • 0 frames");
            return;
        }
        var lastTimestamp = frames[^1].Timestamp;
        var finalMinute = frames.Where(frame => unchecked(lastTimestamp - frame.Timestamp) <= 60_000u).ToArray();
        SetTimelineFrames(finalMinute, "The dump contains too few samples to draw a timeline.",
            $"Saved dump • {finalMinute.Length:N0} frames from the final 60 seconds");
    }

    private void SetTimelineFrames(IReadOnlyList<TelemetrySample> frames, string emptyMessage, string footnote)
    {
        _timelineFrames = frames.Count <= 600 ? frames.ToArray() :
            frames.Where((_, index) => index % (int)Math.Ceiling(frames.Count / 599.0) == 0 || index == frames.Count - 1).ToArray();
        TimelineEmptyOverlay.Text = emptyMessage;
        TimelineEmptyOverlay.Visibility = _timelineFrames.Length < 2 ? Visibility.Visible : Visibility.Collapsed;
        TimelineFootnote.Text = footnote;
        DrawTimeline();
    }

    private void TimelineCanvas_SizeChanged(object sender, SizeChangedEventArgs e) => DrawTimeline();

    private void DrawTimeline()
    {
        if (TimelineCanvas is null || YokeXLine is null) return;
        var width = TimelineCanvas.ActualWidth;
        var height = TimelineCanvas.ActualHeight;
        if (width < 8 || height < 8 || _timelineFrames.Length < 2)
        {
            YokeXLine.Points = []; YokeYLine.Points = []; RudderLine.Points = []; ThrottleLine.Points = [];
            return;
        }
        var first = _timelineFrames[0].Timestamp;
        var total = unchecked(_timelineFrames[^1].Timestamp - first);
        PointCollection Build(Func<TelemetrySample, double> value, bool throttle = false)
        {
            var points = new PointCollection(_timelineFrames.Length);
            for (var i = 0; i < _timelineFrames.Length; i++)
            {
                var sample = _timelineFrames[i];
                var x = total == 0 ? i * width / (_timelineFrames.Length - 1) :
                    unchecked(sample.Timestamp - first) * width / total;
                var scaled = throttle ? Math.Clamp(value(sample), 0, 1) :
                    (Math.Clamp(value(sample), -1, 1) + 1) / 2;
                points.Add(new System.Windows.Point(Math.Clamp(x, 0, width), 6 + (1 - scaled) * (height - 12)));
            }
            return points;
        }
        YokeXLine.Points = Build(frame => frame.YokeX);
        YokeYLine.Points = Build(frame => frame.YokeY);
        RudderLine.Points = Build(frame => frame.Rudder);
        ThrottleLine.Points = Build(frame => frame.Throttle, true);
    }

    private void CopyDiagnostics_Click(object sender, RoutedEventArgs e)
    {
        var status = MonitorStatusReader.Read();
        var selected = TelemetryGrid.SelectedItem as TelemetrySample ?? _loadedDump?.Frames.LastOrDefault() ?? status.LatestInput;
        var snippet = new
        {
            schema = "msfs-input-blackbox/diagnostics-clipboard/v1",
            copiedAtUtc = DateTimeOffset.UtcNow,
            recorder = new
            {
                status.State, status.UpdatedAtUtc, status.SessionId, status.LastFrameTimestamp,
                status.BufferedFrames, status.BufferCapacity, status.AircraftType,
                status.IndicatedAirspeedKnots, status.AltitudeFeet
            },
            dump = _loadedDump is null ? null : new
            {
                _loadedDump.Schema, _loadedDump.Reason, _loadedDump.CapturedAtUtc,
                frameCount = _loadedDump.Frames.Count
            },
            lastInput = selected
        };
        try
        {
            System.Windows.Clipboard.SetText(JsonSerializer.Serialize(snippet, new JsonSerializerOptions { WriteIndented = true }));
            ActionFeedback.Text = "Diagnostics copied as JSON";
        }
        catch (ExternalException)
        {
            ActionFeedback.Text = "Clipboard is busy; try again";
        }
        _actionFeedbackUntilUtc = DateTimeOffset.UtcNow.AddSeconds(6);
    }
}
