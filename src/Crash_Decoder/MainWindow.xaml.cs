// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using Microsoft.Win32;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Threading;

namespace MsfsInputBlackbox.Decoder;

public partial class MainWindow : Window
{
    private readonly AppSettings _settings = SettingsStore.Load();
    private readonly DispatcherTimer _statusTimer = new() { Interval = TimeSpan.FromSeconds(2) };
    private TelemetryDump? _loadedDump;
    private bool _isApplyingSettings;

    public MainWindow(string? initialDumpPath)
    {
        InitializeComponent();
        SourceInitialized += (_, _) => ApplyWindows11Chrome();
        _statusTimer.Tick += (_, _) => UpdateStatus();
        _statusTimer.Start();
        Closed += (_, _) => _statusTimer.Stop();
        _isApplyingSettings = true;
        ApplySettings();
        _isApplyingSettings = false;
        RefreshCandidates();
        UpdateStatus();
        if (!string.IsNullOrWhiteSpace(initialDumpPath)) LoadDump(initialDumpPath);
    }

    private void ApplySettings()
    {
        Msfs2020Path.Text = _settings.Msfs2020CommunityPath ?? string.Empty;
        Msfs2024Path.Text = _settings.Msfs2024CommunityPath ?? string.Empty;
        ExportFolderPath.Text = _settings.ExportFolder ?? string.Empty;
        SessionLogPath.Text = _settings.SessionLogPath ?? string.Empty;
        LoggingEnabled.IsChecked = _settings.LoggingEnabled;
        KeepDumps.IsChecked = _settings.KeepCrashDumps;
        HistorySeconds.SelectedIndex = _settings.HistorySeconds switch { 300 => 1, 900 => 2, 1800 => 3, 3600 => 4, _ => 0 };
        SampleRate.SelectedIndex = _settings.SampleRateHz switch { 15 => 0, 60 => 2, _ => 1 };
        ProfileName.Text = _settings.AircraftProfileName;
        ProfileCard.Text = _settings.AircraftProfileName;
        UpdateLoggingCard();
        UpdateCapacity(null);
    }

    private void Navigate_Click(object sender, RoutedEventArgs e)
    {
        var page = (sender as FrameworkElement)?.Tag?.ToString() ?? "Dashboard";
        DashboardView.Visibility = page == "Dashboard" ? Visibility.Visible : Visibility.Collapsed;
        LiveView.Visibility = page == "Live" ? Visibility.Visible : Visibility.Collapsed;
        AircraftView.Visibility = page == "Aircraft" ? Visibility.Visible : Visibility.Collapsed;
        TablesView.Visibility = page == "Tables" ? Visibility.Visible : Visibility.Collapsed;
        SettingsView.Visibility = page == "Settings" ? Visibility.Visible : Visibility.Collapsed;
        PageTitle.Text = page switch { "Live" => "Live recording", "Aircraft" => "Aircraft profiles", "Tables" => "Telemetry tables", "Settings" => "Settings", _ => "Dashboard" };
        PageSubtitle.Text = page switch
        {
            "Live" => "Choose what is recorded when the external monitor is connected.",
            "Aircraft" => "Keep per-aircraft logging preferences without touching simulator files.",
            "Tables" => "Classic debugger-style inspection of every recorded frame.",
            "Settings" => "Discover or configure local Community folder locations.",
            _ => "Review recent inputs and recorder health."
        };
    }

    private void OpenDump_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Filter = "Crash telemetry dump (*.json)|*.json|All files (*.*)|*.*", Title = "Open MSFS Input Blackbox dump" };
        if (dialog.ShowDialog(this) == true) LoadDump(dialog.FileName);
    }

    private void ExportBundle_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetDump(out var dump)) return;
        var dialog = CreateSaveDialog("Developer export (*.zip)|*.zip", "msfs-blackbox-developer-export.zip");
        if (dialog.ShowDialog(this) != true) return;
        try { DeveloperExport.CreateBundle(dialog.FileName, dump, dump.Frames); }
        catch (Exception ex) when (ex is IOException or InvalidDataException) { ShowExportError(ex); }
    }

    private void Snapshot_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetDump(out var dump)) return;
        var frame = TelemetryGrid.SelectedItem as TelemetrySample ?? dump.Frames[^1];
        var dialog = CreateSaveDialog("Telemetry snapshot (*.json)|*.json", "msfs-blackbox-snapshot.json");
        if (dialog.ShowDialog(this) != true) return;
        try { DeveloperExport.WriteSnapshot(dialog.FileName, dump, frame); }
        catch (IOException ex) { ShowExportError(ex); }
    }

    private void ExportLastTenSeconds_Click(object sender, RoutedEventArgs e)
    {
        if (!TryGetDump(out var dump)) return;
        var lastTimestamp = dump.Frames[^1].Timestamp;
        var frames = dump.Frames.Where(frame => lastTimestamp - frame.Timestamp <= 10_000).ToArray();
        var dialog = CreateSaveDialog("Developer export (*.zip)|*.zip", "msfs-blackbox-last-10-seconds.zip");
        if (dialog.ShowDialog(this) != true) return;
        try { DeveloperExport.CreateBundle(dialog.FileName, dump, frames); }
        catch (Exception ex) when (ex is IOException or InvalidDataException) { ShowExportError(ex); }
    }

    private bool TryGetDump(out TelemetryDump dump)
    {
        if (_loadedDump is not null) { dump = _loadedDump; return true; }
        System.Windows.MessageBox.Show("Open a telemetry dump first.", "No telemetry loaded", MessageBoxButton.OK, MessageBoxImage.Information);
        dump = null!;
        return false;
    }

    private Microsoft.Win32.SaveFileDialog CreateSaveDialog(string filter, string name) => new()
    {
        Filter = filter,
        FileName = name,
        InitialDirectory = Directory.Exists(_settings.ExportFolder) ? _settings.ExportFolder : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        OverwritePrompt = true
    };

    private void ShowExportError(Exception ex) => System.Windows.MessageBox.Show(ex.Message, "Cannot create export", MessageBoxButton.OK, MessageBoxImage.Error);

    private void LoadDump(string path)
    {
        try
        {
            _loadedDump = TelemetryDumpReader.Read(path);
            TelemetryGrid.ItemsSource = _loadedDump.Frames;
            var duration = (_loadedDump.Frames[^1].Timestamp - _loadedDump.Frames[0].Timestamp) / 1000.0;
            TableHint.Text = $"{_loadedDump.Frames.Count:N0} frames • {duration:F1}s • {_loadedDump.Reason}";
            TableTitle.Text = "Input frames";
            PageTitle.Text = "Telemetry tables";
            PageSubtitle.Text = Path.GetFileName(path);
            DashboardView.Visibility = Visibility.Collapsed; TablesView.Visibility = Visibility.Visible;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or System.Text.Json.JsonException or KeyNotFoundException)
        {
            System.Windows.MessageBox.Show(ex.Message, "Cannot decode telemetry dump", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void Refresh_Click(object sender, RoutedEventArgs e) { UpdateStatus(); RefreshCandidates(); }

    private void FindFolders_Click(object sender, RoutedEventArgs e) => RefreshCandidates();

    private void RefreshCandidates()
    {
        CandidatesList.ItemsSource = CommunityFolderDiscovery.FindCandidates();
        CandidatesList.DisplayMemberPath = nameof(CommunityFolderCandidate.Display);
    }

    private void CandidatesList_DoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
    {
        if (CandidatesList.SelectedItem is not CommunityFolderCandidate candidate) return;
        if (candidate.Simulator == SimulatorVersion.Msfs2024) Msfs2024Path.Text = candidate.Path; else Msfs2020Path.Text = candidate.Path;
        SavePaths();
    }

    private void Browse2024_Click(object sender, RoutedEventArgs e) => BrowseForPath(Msfs2024Path);
    private void Browse2020_Click(object sender, RoutedEventArgs e) => BrowseForPath(Msfs2020Path);
    private void BrowseForPath(System.Windows.Controls.TextBox box)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Select a Community folder" };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) box.Text = dialog.SelectedPath;
    }
    private void Open2024_Click(object sender, RoutedEventArgs e) => OpenFolder(Msfs2024Path.Text);
    private void Open2020_Click(object sender, RoutedEventArgs e) => OpenFolder(Msfs2020Path.Text);
    private void OpenFolder(string path)
    {
        if (!Directory.Exists(path)) { System.Windows.MessageBox.Show("This folder does not exist.", "Cannot open folder", MessageBoxButton.OK, MessageBoxImage.Information); return; }
        Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void Path_TextChanged(object sender, TextChangedEventArgs e) { if (!_isApplyingSettings) SavePaths(); }
    private void SavePaths()
    {
        _settings.Msfs2020CommunityPath = Msfs2020Path?.Text.Trim();
        _settings.Msfs2024CommunityPath = Msfs2024Path?.Text.Trim();
        TrySaveSettings();
    }
    private void ChooseExportFolder_Click(object sender, RoutedEventArgs e)
    {
        using var dialog = new System.Windows.Forms.FolderBrowserDialog { Description = "Choose the default developer-export folder" };
        if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK) ExportFolderPath.Text = dialog.SelectedPath;
    }
    private void ExportFolder_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isApplyingSettings || ExportFolderPath is null) return;
        _settings.ExportFolder = ExportFolderPath.Text.Trim();
        TrySaveSettings();
    }
    private void SessionLogPath_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isApplyingSettings || SessionLogPath is null) return;
        _settings.SessionLogPath = SessionLogPath.Text.Trim();
        TrySaveSettings();
    }
    private void LoggingOption_Changed(object sender, RoutedEventArgs e)
    {
        if (_isApplyingSettings) return;
        if (LoggingEnabled is null) return;
        _settings.LoggingEnabled = LoggingEnabled.IsChecked == true;
        _settings.KeepCrashDumps = KeepDumps.IsChecked == true;
        _settings.HistorySeconds = int.TryParse((HistorySeconds.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString(), out var seconds) ? seconds : 60;
        _settings.SampleRateHz = int.TryParse((SampleRate.SelectedItem as System.Windows.Controls.ComboBoxItem)?.Content?.ToString(), out var hertz) ? hertz : 30;
        UpdateLoggingCard(); TrySaveSettings();
        UpdateCapacity(null);
    }
    private void ProfileName_TextChanged(object sender, TextChangedEventArgs e)
    {
        if (_isApplyingSettings) return;
        if (ProfileName is null) return;
        _settings.AircraftProfileName = string.IsNullOrWhiteSpace(ProfileName.Text) ? "Default / all aircraft" : ProfileName.Text.Trim();
        ProfileCard.Text = _settings.AircraftProfileName; TrySaveSettings();
    }
    private void UpdateLoggingCard() => LoggingCard.Text = _settings.LoggingEnabled ? $"Enabled • {_settings.HistorySeconds / 60.0:F0} min" : "Disabled";
    private void TrySaveSettings() { try { SettingsStore.Save(_settings); } catch (IOException) { } catch (UnauthorizedAccessException) { } }

    private void TablesList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        // The selected item in XAML can raise this while InitializeComponent is
        // still constructing the table labels.
        if (TableTitle is null || TableHint is null) return;
        if (TablesList.SelectedItem is not System.Windows.Controls.ListBoxItem item) return;
        TableTitle.Text = item.Content.ToString() ?? "Telemetry table";
        if (item.Content?.ToString() != "Input frames") TableHint.Text = "This table will populate when the live monitor emits the corresponding data.";
    }

    private void UpdateStatus()
    {
        var status = MonitorStatusReader.Read();
        var state = status.State.ToLowerInvariant();
        SidebarStatus.Text = state switch
        {
            "connected" => "● MSFS CONNECTED\n" + status.Detail,
            "connecting" => "● CONNECTING\n" + status.Detail,
            "disconnected" => "● DISCONNECTED\n" + status.Detail,
            _ => "● RECORDER NOT RUNNING\n" + status.Detail
        };
        ConnectionCard.Text = state switch { "connected" => "Connected", "connecting" => "Connecting", "disconnected" => "Disconnected", _ => "Recorder not running" };
        ConnectionDetail.Text = status.Detail;
        UpdateCapacity(status);
    }

    private void UpdateCapacity(MonitorStatus? status)
    {
        if (CapacityBar is null) return;
        if (status?.BufferCapacity is > 0 && status.BufferedFrames is >= 0)
        {
            var percentage = Math.Clamp(status.BufferedFrames.Value * 100.0 / status.BufferCapacity.Value, 0, 100);
            CapacityBar.Value = percentage;
            CapacityText.Text = $"Rolling buffer: {status.BufferedFrames:N0} / {status.BufferCapacity:N0} frames ({percentage:F0}%)";
            PurgeWarning.Text = percentage >= 100 || status.IsPurging
                ? "Rolling purge is active: the oldest frames are being overwritten."
                : percentage >= 90 ? "Warning: buffer is at 90% capacity; purge begins at 100%."
                : "At 90% capacity the monitor will warn; at 100% it overwrites the oldest frames in the rolling buffer.";
            return;
        }
        var plannedFrames = _settings.HistorySeconds * _settings.SampleRateHz;
        CapacityBar.Value = 0;
        CapacityText.Text = $"Planned capacity: {plannedFrames:N0} frames ({_settings.HistorySeconds / 60.0:F0} minutes at {_settings.SampleRateHz} Hz)";
        PurgeWarning.Text = "No live session. The monitor will warn at 90%; at 100% the rolling buffer overwrites the oldest frames.";
    }

    private void ApplyWindows11Chrome()
    {
        var handle = new WindowInteropHelper(this).Handle;
        var dark = 1; DwmSetWindowAttribute(handle, 20, ref dark, sizeof(int));
        var rounded = 2; DwmSetWindowAttribute(handle, 33, ref rounded, sizeof(int));
    }

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attribute, ref int value, int size);
}

