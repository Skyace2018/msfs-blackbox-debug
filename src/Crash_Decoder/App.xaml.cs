// MIT License. Copyright (c) 2026 MSFS-Input-Blackbox contributors.
using System.Windows;

namespace MsfsInputBlackbox.Decoder;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        if (e.Args.Contains("--self-test", StringComparer.OrdinalIgnoreCase))
        {
            Shutdown(SelfTest.Run() ? 0 : 1);
            return;
        }
        new MainWindow(e.Args.FirstOrDefault(File.Exists)).Show();
    }
}

