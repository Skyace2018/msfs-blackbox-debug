// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Text.Json;

namespace MsfsInputBlackbox.Decoder;

internal sealed class DecoderForm : Form
{
    private readonly Label _summary = new() { AutoSize = true, ForeColor = Color.White, Padding = new Padding(10, 8, 10, 8) };
    private readonly DataGridView _grid = CreateGrid();
    private readonly TimelinePanel _timeline = new();

    public DecoderForm(string? initialPath)
    {
        Text = "MSFS Input Blackbox — Crash Telemetry Decoder";
        MinimumSize = new Size(960, 640);
        BackColor = Color.FromArgb(13, 17, 23);
        var toolbar = new FlowLayoutPanel { Dock = DockStyle.Top, Height = 45, Padding = new Padding(8), BackColor = Color.FromArgb(22, 27, 34) };
        var open = new Button { Text = "Open telemetry dump…", AutoSize = true };
        open.Click += (_, _) => ChooseAndLoad();
        toolbar.Controls.Add(open);
        toolbar.Controls.Add(_summary);
        var split = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterDistance = 260, BackColor = BackColor };
        split.Panel1.Controls.Add(_timeline);
        split.Panel2.Controls.Add(_grid);
        Controls.Add(split);
        Controls.Add(toolbar);
        if (!string.IsNullOrWhiteSpace(initialPath)) LoadDump(initialPath);
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
}

