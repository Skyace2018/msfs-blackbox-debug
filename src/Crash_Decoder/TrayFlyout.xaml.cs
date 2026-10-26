// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Windows;

namespace MsfsInputBlackbox.Decoder;

public partial class TrayFlyout : Window
{
    private readonly Action _restore;
    private readonly Action _exit;

    public TrayFlyout(Action restore, Action exit)
    {
        InitializeComponent();
        _restore = restore;
        _exit = exit;
        Deactivated += (_, _) => Hide();
    }

    public void ShowNearTaskbar(string status)
    {
        StatusText.Text = status;
        var workArea = SystemParameters.WorkArea;
        Left = workArea.Right - Width - 14;
        Top = workArea.Bottom - Height - 14;
        Show();
        Activate();
    }

    private void Restore_Click(object sender, RoutedEventArgs e) { Hide(); _restore(); }
    private void Minimize_Click(object sender, RoutedEventArgs e) => Hide();
    private void Exit_Click(object sender, RoutedEventArgs e) { Hide(); _exit(); }
}
