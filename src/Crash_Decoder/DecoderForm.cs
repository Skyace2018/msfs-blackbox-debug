// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal sealed class DecoderForm : Form
{
    private readonly Label _summary = new() { AutoSize = true, ForeColor = Color.White, Padding = new Padding(10, 8, 10, 8) };
    private readonly Label _connectionStatus = new() { AutoSize = true, ForeColor = Color.Gainsboro, Padding = new Padding(10, 8, 10, 8) };
    private readonly DataGridView _grid = CreateGrid();
    private readonly TimelinePanel _timeline = new();
    private readonly TextBox _msfs2020Path = new() { Dock = DockStyle.Top, PlaceholderText = "Not configured" };
    private readonly TextBox _msfs2024Path = new() { Dock = DockStyle.Top, PlaceholderText = "Not configured" };
    private readonly ListBox _detectedPaths = new() { Dock = DockStyle.Fill, IntegralHeight = false };
    private readonly AppSettings _settings = SettingsStore.Load();
    private readonly System.Windows.Forms.Timer _statusTimer = new() { Interval = 2_000 };

    public DecoderForm(string? initialPath)
    {
        Text = "MSFS Input Blackbox — Crash Telemetry Decoder";
        MinimumSize = new Size(1120, 720);
        ClientSize = new Size(1440, 900);
        StartPosition = FormStartPosition.CenterScreen;
        BackColor = Color.FromArgb(13, 17, 23);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(8), BackColor = Color.FromArgb(22, 27, 34) };
        var open = new Button { Text = "Open telemetry dump…", AutoSize = true };
        open.Click += (_, _) => ChooseAndLoad();
        toolbar.Controls.Add(open);
        toolbar.Controls.Add(_connectionStatus);
        toolbar.Controls.Add(_summary);
        var workspace = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Vertical, SplitterDistance = 1060, BackColor = BackColor, FixedPanel = FixedPanel.Panel2 };
        var dataSplit = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 345, BackColor = BackColor };
        dataSplit.Panel1.Controls.Add(_timeline);
        dataSplit.Panel2.Controls.Add(_grid);
        workspace.Panel1.Controls.Add(dataSplit);
        workspace.Panel2.Controls.Add(CreateOptionsPanel());
        Controls.Add(workspace);
        Controls.Add(toolbar);
        _msfs2020Path.Text = _settings.Msfs2020CommunityPath ?? string.Empty;
        _msfs2024Path.Text = _settings.Msfs2024CommunityPath ?? string.Empty;
        _statusTimer.Tick += (_, _) => UpdateConnectionStatus();
        _statusTimer.Start();
        UpdateConnectionStatus();
        RefreshDetectedPaths();
        if (!string.IsNullOrWhiteSpace(initialPath)) LoadDump(initialPath);
    }

    private Control CreateOptionsPanel()
    {
        var panel = new Panel { Dock = DockStyle.Fill, Padding = new Padding(10), BackColor = Color.FromArgb(22, 27, 34) };
        panel.Controls.Add(_detectedPaths);
        var detect = new Button { Text = "Find local folders", Dock = DockStyle.Top, Height = 32 };
        detect.Click += (_, _) => RefreshDetectedPaths();
        panel.Controls.Add(detect);
        panel.Controls.Add(new Label { Text = "Detected candidates (double-click to use)", ForeColor = Color.Gainsboro, Dock = DockStyle.Top, Height = 24 });
        _detectedPaths.DoubleClick += (_, _) => UseSelectedCandidate();
        AddPathControls(panel, "MSFS 2024 Community folder", _msfs2024Path, SimulatorVersion.Msfs2024);
        AddPathControls(panel, "MSFS 2020 Community folder", _msfs2020Path, SimulatorVersion.Msfs2020);
        panel.Controls.Add(new Label { Text = "OPTIONS", Font = new Font(Font, FontStyle.Bold), ForeColor = Color.White, Dock = DockStyle.Top, Height = 30 });
        return panel;
    }

    private void AddPathControls(Panel panel, string label, TextBox pathBox, SimulatorVersion simulator)
    {
        var row = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 34, FlowDirection = FlowDirection.LeftToRight, WrapContents = false };
        var browse = new Button { Text = "Browse…", AutoSize = true };
        browse.Click += (_, _) => BrowseForPath(pathBox, simulator);
        var open = new Button { Text = "Open", AutoSize = true };
        open.Click += (_, _) => OpenFolder(pathBox.Text);
        row.Controls.Add(browse);
        row.Controls.Add(open);
        panel.Controls.Add(row);
        panel.Controls.Add(pathBox);
        panel.Controls.Add(new Label { Text = label, ForeColor = Color.Gainsboro, Dock = DockStyle.Top, Height = 23 });
    }

    private void BrowseForPath(TextBox pathBox, SimulatorVersion simulator)
    {
        using var dialog = new FolderBrowserDialog { Description = $"Select the {simulator} Community folder" };
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        pathBox.Text = dialog.SelectedPath;
        SavePaths();
    }

    private void OpenFolder(string path)
    {
        if (!Directory.Exists(path)) { MessageBox.Show(this, "This folder does not exist yet.", "Cannot open folder", MessageBoxButtons.OK, MessageBoxIcon.Information); return; }
        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = path, UseShellExecute = true });
    }

    private void RefreshDetectedPaths()
    {
        _detectedPaths.DataSource = CommunityFolderDiscovery.FindCandidates().ToList();
        _detectedPaths.DisplayMember = nameof(CommunityFolderCandidate.Display);
    }

    private void UseSelectedCandidate()
    {
        if (_detectedPaths.SelectedItem is not CommunityFolderCandidate candidate) return;
        var destination = candidate.Simulator == SimulatorVersion.Msfs2024 ? _msfs2024Path : _msfs2020Path;
        destination.Text = candidate.Path;
        SavePaths();
    }

    private void SavePaths()
    {
        _settings.Msfs2020CommunityPath = _msfs2020Path.Text.Trim();
        _settings.Msfs2024CommunityPath = _msfs2024Path.Text.Trim();
        try { SettingsStore.Save(_settings); }
        catch (IOException ex) { MessageBox.Show(this, ex.Message, "Cannot save options", MessageBoxButtons.OK, MessageBoxIcon.Warning); }
    }

    private void UpdateConnectionStatus()
    {
        var status = MonitorStatusReader.Read();
        var (prefix, color) = status.State.ToLowerInvariant() switch
        {
            "connected" => ("● MSFS CONNECTED", Color.FromArgb(63, 185, 80)),
            "disconnected" => ("● MSFS DISCONNECTED", Color.FromArgb(248, 81, 73)),
            "connecting" => ("● MSFS CONNECTING", Color.FromArgb(210, 153, 34)),
            _ => ("● RECORDER NOT RUNNING", Color.FromArgb(148, 163, 184))
        };
        _connectionStatus.ForeColor = color;
        _connectionStatus.Text = $"{prefix} — {status.Detail}";
    }

    private static DataGridView CreateGrid()
    {
        var grid = new DataGridView
        {
            Dock = DockStyle.Fill, ReadOnly = true, AllowUserToAddRows = false, AllowUserToDeleteRows = false,
            AutoGenerateColumns = false, BackgroundColor = Color.FromArgb(13, 17, 23), GridColor = Color.FromArgb(48, 54, 61),
            EnableHeadersVisualStyles = false, ColumnHeadersDefaultCellStyle = { BackColor = Color.FromArgb(22, 27, 34), ForeColor = Color.White },
            DefaultCellStyle = { BackColor = Color.FromArgb(13, 17, 23), ForeColor = Color.FromArgb(201, 209, 217), SelectionBackColor = Color.FromArgb(31, 111, 235) }
        };
        foreach (var (header, property) in new[] { ("Time (ms)", "Timestamp"), ("Yoke X", "YokeX"), ("Yoke Y", "YokeY"), ("Rudder", "Rudder"), ("Throttle", "Throttle"), ("Switches 0", "Switches0"), ("Switches 1", "Switches1") })
            grid.Columns.Add(new DataGridViewTextBoxColumn { HeaderText = header, DataPropertyName = property, AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
        return grid;
    }

    private void ChooseAndLoad()
    {
        using var dialog = new OpenFileDialog { Filter = "Crash telemetry dump (*.json)|*.json|All files (*.*)|*.*", Title = "Open MSFS Input Blackbox dump" };
        if (dialog.ShowDialog(this) == DialogResult.OK) LoadDump(dialog.FileName);
    }

    private void LoadDump(string path)
    {
        try
        {
            var dump = TelemetryDumpReader.Read(path);
            _grid.DataSource = dump.Frames.ToArray();
            _timeline.SetFrames(dump.Frames);
            var duration = dump.Frames[^1].Timestamp - dump.Frames[0].Timestamp;
            _summary.Text = $"{dump.Frames.Count:N0} frames  •  {duration / 1000.0:F1}s  •  {dump.Reason}  •  {dump.CapturedAtUtc:O}";
        }
        catch (Exception ex) when (ex is IOException or JsonException or InvalidDataException or KeyNotFoundException)
        {
            MessageBox.Show(this, ex.Message, "Cannot decode telemetry dump", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        _statusTimer.Dispose();
        base.OnFormClosed(e);
    }
}

